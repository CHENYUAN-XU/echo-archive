using System.Collections.Generic;
using System.IO;
using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;

namespace EchoForum.Infrastructure
{
    public interface ILocalCaseSaveStore
    {
        CaseSnapshot Load(string caseId);
        void Save(CaseSnapshot snapshot);
    }

    public interface IPrototypeProgressReset
    {
        void ResetPrototypeProgress();
    }

    [System.Serializable]
    public sealed class LocalCaseSaveFile
    {
        public int SaveVersion = 1;
        public List<CaseSnapshot> Cases = new List<CaseSnapshot>();
    }

    /// <summary>Local progress only. Published case and forum content remains in ScriptableObject assets.</summary>
    public sealed class JsonCaseSaveStore : ILocalCaseSaveStore, IPrototypeProgressReset
    {
        private const string SaveName = "echo-archive-case.json";
        private readonly string path;

        public JsonCaseSaveStore() : this(UnityEngine.Application.persistentDataPath) { }
        public JsonCaseSaveStore(string directory) { path = Path.Combine(directory, SaveName); }

        public CaseSnapshot Load(string caseId)
        {
            foreach (var snapshot in LoadAll())
            {
                if (snapshot != null && snapshot.CaseId == caseId) return snapshot.Clone();
            }
            return null;
        }

        public void Save(CaseSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrEmpty(snapshot.CaseId)) return;
            var all = LoadAll();
            var index = all.FindIndex(item => item != null && item.CaseId == snapshot.CaseId);
            if (index >= 0) all[index] = snapshot.Clone(); else all.Add(snapshot.Clone());
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new LocalCaseSaveFile { Cases = all }, true));
        }

        public void ResetPrototypeProgress()
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private List<CaseSnapshot> LoadAll()
        {
            if (!File.Exists(path)) return new List<CaseSnapshot>();
            var json = File.ReadAllText(path);
            var current = JsonUtility.FromJson<LocalCaseSaveFile>(json);
            if (current != null && current.Cases != null && current.Cases.Count > 0) return current.Cases;

            // Compatibility with the previous single-CaseSnapshot save format.
            var legacy = JsonUtility.FromJson<CaseSnapshot>(json);
            if (legacy != null && !string.IsNullOrEmpty(legacy.CaseId)) return new List<CaseSnapshot> { legacy };
            return current != null && current.Cases != null ? current.Cases : new List<CaseSnapshot>();
        }
    }

    public sealed class LocalCaseSessionRepository : ICaseSessionRepository
    {
        private readonly ILocalCaseSaveStore store;
        private readonly Dictionary<string, CaseSession> sessions = new Dictionary<string, CaseSession>();
        public LocalCaseSessionRepository(ILocalCaseSaveStore store) { this.store = store; }
        public CaseSnapshot GetSnapshot(string id) => GetSession(id).Snapshot;
        public IReadOnlyList<CaseEvent> Dispatch(CaseCommand command)
        {
            var session = GetSession(command.CaseId);
            var events = session.Execute(command);
            if (events.Count > 0) store.Save(session.Snapshot);
            return events;
        }
        private CaseSession GetSession(string id)
        {
            if (!sessions.TryGetValue(id, out var session)) { session = new CaseSession(id, store.Load(id)); sessions.Add(id, session); }
            return session;
        }
    }
}