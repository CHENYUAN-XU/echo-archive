using System;
using System.Collections.Generic;
using EchoForum.Application;
using EchoForum.Domain;
using EchoForum.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoForum.Editor
{
    public static class InvestigationPrototypeVerifier
    {
        [MenuItem("Echo Archive/Verify Investigation Prototype")]
        public static void Verify()
        {
            var store = new MemoryStore();
            var cases = new CaseUseCases(new LocalRepository(store));
            const string caseId = "case-observation-01";

            Require(cases.Start(caseId).Count == 1, "The case did not start.");
            Require(cases.Investigate(caseId, "record-fragment").Count == 0, "The second clue was not gated.");
            Require(cases.Investigate(caseId, "observation-a").Count == 1, "The first clue could not be recorded.");
            Require(cases.Investigate(caseId, "record-fragment").Count == 1, "The second clue did not unlock.");
            Require(cases.Acquire(caseId, "test-tool").Count == 1, "The test item was not granted.");
            Require(cases.Resolve(caseId, "disposal-object").Count == 1, "The disposal point was not resolved.");
            Require(cases.GetSnapshot(caseId).Status == CaseStatus.ReadyToSettle, "The case did not become ready to settle.");
            Require(cases.Settle(caseId).Count == 1, "The case did not settle.");

            var closed = cases.GetSnapshot(caseId);
            var persisted = JsonUtility.FromJson<CaseSnapshot>(JsonUtility.ToJson(closed));
            Require(persisted.Status == CaseStatus.Closed && !string.IsNullOrEmpty(persisted.CompletedAtUtc), "The completed snapshot is not JSON persistent.");

            EditorSceneManager.OpenScene("Assets/Game/Scenes/InvestigationPrototype.unity");
            Require(UnityEngine.Object.FindFirstObjectByType<InvestigationPrototypeController>() != null, "Investigation controller is missing.");
            Require(UnityEngine.Object.FindFirstObjectByType<UIDocument>() != null, "UI Toolkit document is missing.");
            Require(UnityEngine.Object.FindObjectsByType<InvestigationPoint>(FindObjectsSortMode.None).Length == 3, "The scene does not contain three investigation points.");
            Require(UnityEngine.Object.FindFirstObjectByType<InvestigationPlayerController>() != null, "Observer controller is missing.");
            Debug.Log("Investigation prototype verification passed: ordered case flow, JSON persistence, greybox scene, observer, and UI Toolkit overlay.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class MemoryStore
        {
            private readonly Dictionary<string, CaseSnapshot> data = new Dictionary<string, CaseSnapshot>();
            public CaseSnapshot Load(string id) => data.TryGetValue(id, out var snapshot) ? snapshot.Clone() : null;
            public void Save(CaseSnapshot snapshot) => data[snapshot.CaseId] = snapshot.Clone();
        }

        private sealed class LocalRepository : ICaseSessionRepository
        {
            private readonly MemoryStore store;
            private readonly Dictionary<string, CaseSession> sessions = new Dictionary<string, CaseSession>();
            public LocalRepository(MemoryStore store) { this.store = store; }
            public CaseSnapshot GetSnapshot(string id) => Session(id).Snapshot;
            public IReadOnlyList<CaseEvent> Dispatch(CaseCommand command)
            {
                var session = Session(command.CaseId);
                var events = session.Execute(command);
                if (events.Count > 0) store.Save(session.Snapshot);
                return events;
            }
            private CaseSession Session(string id)
            {
                if (!sessions.TryGetValue(id, out var session)) { session = new CaseSession(id, store.Load(id)); sessions[id] = session; }
                return session;
            }
        }
    }
}
