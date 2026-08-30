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

    public sealed class JsonCaseSaveStore : ILocalCaseSaveStore
    {
        private const string SaveName = "echo-archive-case.json";
        public CaseSnapshot Load(string caseId)
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, SaveName);
            if (!File.Exists(path)) return null;
            var data = JsonUtility.FromJson<CaseSnapshot>(File.ReadAllText(path));
            return data != null && data.CaseId == caseId ? data : null;
        }
        public void Save(CaseSnapshot snapshot)
        {
            var path = Path.Combine(UnityEngine.Application.persistentDataPath, SaveName);
            File.WriteAllText(path, JsonUtility.ToJson(snapshot, true));
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
