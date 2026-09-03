using System;
using System.Collections.Generic;
using EchoForum.Domain;

namespace EchoForum.Application
{
    public interface IGameSaveRepository
    {
        GameSaveLoadResult Load();
        void Save(GameSaveData save);
        void BackupUnreadableSave();
    }

    public interface IGameSession : ICaseSessionRepository
    {
        GameSaveLoadStatus SaveStatus { get; }
        string SaveMessage { get; }
        bool HasProfile { get; }
        PlayerProfile PlayerProfile { get; }
        NavigationContext Navigation { get; }
        ForumState ForumState { get; }
        bool TryCreateProfile(string displayName, out string error);
        void SaveNow();
        void UpdateNavigation(string sceneName, string returnThreadId, string activeCaseId);
        void RememberForumThread(string threadId);
    }

    public sealed class GameSession : IGameSession
    {
        private readonly IGameSaveRepository repository;
        private readonly Dictionary<string, CaseSession> sessions = new Dictionary<string, CaseSession>();
        private GameSaveData save;

        public GameSession(IGameSaveRepository repository)
        {
            this.repository = repository;
            var loaded = repository.Load();
            SaveStatus = loaded.Status;
            SaveMessage = loaded.Message;
            save = loaded.Status == GameSaveLoadStatus.Valid ? loaded.Save : null;
        }

        public GameSaveLoadStatus SaveStatus { get; private set; }
        public string SaveMessage { get; private set; }
        public bool HasProfile => save != null && save.PlayerProfile != null && !string.IsNullOrWhiteSpace(save.PlayerProfile.DisplayName);
        public PlayerProfile PlayerProfile => save == null ? null : save.PlayerProfile;
        public NavigationContext Navigation => save == null ? null : save.Navigation;
        public ForumState ForumState => save == null ? null : save.ForumState;

        public bool TryCreateProfile(string displayName, out string error)
        {
            displayName = displayName == null ? string.Empty : displayName.Trim();
            if (string.IsNullOrEmpty(displayName)) { error = "请输入档案显示名。"; return false; }
            if (SaveStatus == GameSaveLoadStatus.Corrupt) repository.BackupUnreadableSave();
            var migratedCases = save == null ? new List<CaseSnapshot>() : save.CaseSnapshots;
            var now = DateTimeOffset.UtcNow.ToString("O");
            save = new GameSaveData
            {
                PlayerProfile = new PlayerProfile { PlayerId = Guid.NewGuid().ToString("N"), DisplayName = displayName, CreatedAtUtc = now, LastPlayedAtUtc = now },
                ForumState = new ForumState(),
                Navigation = new NavigationContext { LastSceneName = "MainMenu" },
                CaseSnapshots = migratedCases
            };
            sessions.Clear();
            SaveStatus = GameSaveLoadStatus.Valid;
            SaveMessage = null;
            SaveNow();
            error = null;
            return true;
        }

        public CaseSnapshot GetSnapshot(string caseId) => GetSession(caseId).Snapshot;

        public IReadOnlyList<CaseEvent> Dispatch(CaseCommand command)
        {
            if (!HasProfile || command == null) return new List<CaseEvent>();
            var events = GetSession(command.CaseId).Execute(command);
            if (events.Count > 0) SaveNow();
            return events;
        }

        public void UpdateNavigation(string sceneName, string returnThreadId, string activeCaseId)
        {
            if (!HasProfile) return;
            save.Navigation.LastSceneName = sceneName;
            save.Navigation.ReturnThreadId = returnThreadId;
            save.Navigation.ActiveCaseId = activeCaseId;
            SaveNow();
        }

        public void RememberForumThread(string threadId)
        {
            if (!HasProfile) return;
            save.ForumState.LastOpenedThreadId = threadId;
            SaveNow();
        }

        public void SaveNow()
        {
            if (!HasProfile) return;
            save.PlayerProfile.LastPlayedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            save.LastSavedAtUtc = save.PlayerProfile.LastPlayedAtUtc;
            var snapshotsByCaseId = new Dictionary<string, CaseSnapshot>();
            foreach (var snapshot in save.CaseSnapshots)
            {
                if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.CaseId))
                {
                    snapshotsByCaseId[snapshot.CaseId] = snapshot.Clone();
                }
            }

            foreach (var session in sessions.Values)
            {
                var snapshot = session.Snapshot;
                snapshotsByCaseId[snapshot.CaseId] = snapshot;
            }

            save.CaseSnapshots = new List<CaseSnapshot>(snapshotsByCaseId.Values);
            repository.Save(save);
        }

        private CaseSession GetSession(string caseId)
        {
            if (string.IsNullOrEmpty(caseId)) throw new ArgumentException("CaseId is required.", nameof(caseId));
            if (sessions.TryGetValue(caseId, out var session)) return session;
            CaseSnapshot restored = null;
            if (save != null)
            {
                foreach (var snapshot in save.CaseSnapshots)
                {
                    if (snapshot != null && snapshot.CaseId == caseId) { restored = snapshot; break; }
                }
            }
            session = new CaseSession(caseId, restored);
            sessions.Add(caseId, session);
            return session;
        }
    }
}
