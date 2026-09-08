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
        ForumDynamicState ForumDynamicState { get; }
        IReadOnlyList<CaseSnapshot> CaseSnapshots { get; }
        bool TryCreateProfile(string displayName, out string error);
        bool TryCreateForumThread(ForumThreadDraft draft, out PlayerCreatedThread thread, out string error);
        bool TryCreateForumReply(ForumReplyDraft draft, out PlayerCreatedReply reply, out string error);
        void EnsureSystemCaseReply(string threadId, string caseId, string body, string completedAtUtc);
        void SaveNow();
        void UpdateNavigation(string sceneName, string returnThreadId, string activeCaseId, string checkpointId = null);
        void RememberForumThread(string threadId);
        void SetForumNotice(string message);
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
        public ForumDynamicState ForumDynamicState => save == null ? null : save.ForumDynamicState;
        public IReadOnlyList<CaseSnapshot> CaseSnapshots => CollectSnapshots();

        public bool TryCreateProfile(string displayName, out string error)
        {
            displayName = NormalizeText(displayName);
            if (string.IsNullOrEmpty(displayName)) { error = "请输入档案显示名。"; return false; }
            if (SaveStatus == GameSaveLoadStatus.Corrupt) repository.BackupUnreadableSave();
            var migratedCases = save == null ? new List<CaseSnapshot>() : save.CaseSnapshots;
            var now = DateTimeOffset.UtcNow.ToString("O");
            save = new GameSaveData
            {
                PlayerProfile = new PlayerProfile { PlayerId = Guid.NewGuid().ToString("N"), DisplayName = displayName, CreatedAtUtc = now, LastPlayedAtUtc = now },
                ForumState = new ForumState(),
                ForumDynamicState = new ForumDynamicState(),
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

        public bool TryCreateForumThread(ForumThreadDraft draft, out PlayerCreatedThread thread, out string error)
        {
            thread = null;
            if (!HasProfile) { error = "请先创建本地档案。"; return false; }
            var title = NormalizeText(draft == null ? null : draft.Title);
            var body = NormalizeText(draft == null ? null : draft.Body);
            if (draft == null || string.IsNullOrEmpty(draft.BoardId)) { error = "请选择板块。"; return false; }
            if (string.IsNullOrEmpty(title)) { error = "主题标题不能为空。"; return false; }
            if (string.IsNullOrEmpty(body)) { error = "主题正文不能为空。"; return false; }
            thread = new PlayerCreatedThread
            {
                ThreadId = "player-thread-" + Guid.NewGuid().ToString("N"),
                BoardId = draft.BoardId,
                AuthorPlayerId = PlayerProfile.PlayerId,
                AuthorDisplayName = PlayerProfile.DisplayName,
                Title = title,
                Body = body,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
            };
            save.ForumDynamicState.PlayerThreads.Add(thread);
            SaveNow();
            error = null;
            return true;
        }

        public bool TryCreateForumReply(ForumReplyDraft draft, out PlayerCreatedReply reply, out string error)
        {
            reply = null;
            if (!HasProfile) { error = "请先创建本地档案。"; return false; }
            var body = NormalizeText(draft == null ? null : draft.Body);
            if (draft == null || string.IsNullOrEmpty(draft.ThreadId)) { error = "未找到回复主题。"; return false; }
            if (string.IsNullOrEmpty(body)) { error = "回复内容不能为空。"; return false; }
            reply = new PlayerCreatedReply
            {
                ReplyId = "player-reply-" + Guid.NewGuid().ToString("N"),
                ThreadId = draft.ThreadId,
                AuthorPlayerId = PlayerProfile.PlayerId,
                AuthorDisplayName = PlayerProfile.DisplayName,
                Body = body,
                CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O")
            };
            save.ForumDynamicState.PlayerReplies.Add(reply);
            SaveNow();
            error = null;
            return true;
        }

        public void EnsureSystemCaseReply(string threadId, string caseId, string body, string completedAtUtc)
        {
            if (!HasProfile || string.IsNullOrWhiteSpace(threadId) || string.IsNullOrWhiteSpace(caseId) || string.IsNullOrWhiteSpace(body)) return;
            if (save.ForumDynamicState.SystemClosureCaseIds.Contains(caseId)) return;
            save.ForumDynamicState.SystemClosureCaseIds.Add(caseId);
            save.ForumDynamicState.SystemCaseReplies.Add(new SystemCaseReply
            {
                ReplyId = "system-case-" + caseId,
                ThreadId = threadId,
                CaseId = caseId,
                Body = body,
                CreatedAtUtc = string.IsNullOrWhiteSpace(completedAtUtc) ? DateTimeOffset.UtcNow.ToString("O") : completedAtUtc
            });
            SaveNow();
        }

        public CaseSnapshot GetSnapshot(string caseId) => GetSession(caseId).Snapshot;

        public IReadOnlyList<CaseEvent> Dispatch(CaseCommand command)
        {
            if (!HasProfile || command == null) return new List<CaseEvent>();
            var events = GetSession(command.CaseId).Execute(command);
            if (events.Count > 0) SaveNow();
            return events;
        }

        public void UpdateNavigation(string sceneName, string returnThreadId, string activeCaseId, string checkpointId = null)
        {
            if (!HasProfile) return;
            save.Navigation.LastSceneName = sceneName;
            save.Navigation.ReturnThreadId = returnThreadId;
            save.Navigation.ActiveCaseId = activeCaseId;
            save.Navigation.CheckpointId = checkpointId;
            SaveNow();
        }

        public void RememberForumThread(string threadId)
        {
            if (!HasProfile) return;
            save.ForumState.LastOpenedThreadId = threadId;
            SaveNow();
        }

        public void SetForumNotice(string message)
        {
            if (!HasProfile) return;
            save.ForumState.PendingNotice = message;
            SaveNow();
        }

        public void SaveNow()
        {
            if (!HasProfile) return;
            save.PlayerProfile.LastPlayedAtUtc = DateTimeOffset.UtcNow.ToString("O");
            save.LastSavedAtUtc = save.PlayerProfile.LastPlayedAtUtc;
            save.CaseSnapshots = new List<CaseSnapshot>(CollectSnapshots());
            repository.Save(save);
        }

        private IReadOnlyList<CaseSnapshot> CollectSnapshots()
        {
            var snapshotsByCaseId = new Dictionary<string, CaseSnapshot>();
            if (save != null && save.CaseSnapshots != null)
            {
                foreach (var snapshot in save.CaseSnapshots)
                {
                    if (snapshot != null && !string.IsNullOrWhiteSpace(snapshot.CaseId)) snapshotsByCaseId[snapshot.CaseId] = snapshot.Clone();
                }
            }
            foreach (var session in sessions.Values)
            {
                var snapshot = session.Snapshot;
                snapshotsByCaseId[snapshot.CaseId] = snapshot;
            }
            return new List<CaseSnapshot>(snapshotsByCaseId.Values);
        }

        private CaseSession GetSession(string caseId)
        {
            if (string.IsNullOrEmpty(caseId)) throw new ArgumentException("CaseId is required.", nameof(caseId));
            if (sessions.TryGetValue(caseId, out var session)) return session;
            CaseSnapshot restored = null;
            if (save != null && save.CaseSnapshots != null)
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

        private static string NormalizeText(string value) => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}