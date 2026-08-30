using System;
using System.Collections.Generic;

namespace EchoForum.Domain
{
    public sealed class CaseSession : ICaseSession
    {
        private CaseSnapshot snapshot;

        public CaseSession(string caseId, CaseSnapshot restored = null)
        {
            snapshot = restored ?? new CaseSnapshot { CaseId = caseId, Status = CaseStatus.NotStarted };
        }

        public CaseSnapshot Snapshot => snapshot.Clone();

        public IReadOnlyList<CaseEvent> Execute(CaseCommand command)
        {
            var results = new List<CaseEvent>();
            if (command == null || command.CaseId != snapshot.CaseId || snapshot.Status == CaseStatus.Closed) return results;
            if (command is StartCaseCommand && snapshot.Status == CaseStatus.NotStarted) { snapshot.Status = CaseStatus.InProgress; results.Add(new CaseStartedEvent(snapshot.CaseId, Next())); }
            else if (command is InvestigateCaseCommand clue && CanInspect(clue.ClueId)) { snapshot.DiscoveredClues.Add(clue.ClueId); results.Add(new ClueDiscoveredEvent(snapshot.CaseId, Next(), clue.ClueId)); }
            else if (command is AcquireCaseItemCommand item && item.ItemId == "test-tool" && HasClue("record-fragment") && !HasItem(item.ItemId)) { snapshot.HeldItems.Add(item.ItemId); results.Add(new CaseItemAcquiredEvent(snapshot.CaseId, Next(), item.ItemId)); }
            else if (command is ResolveCaseObjectCommand target && target.ObjectId == "disposal-object" && HasItem("test-tool") && snapshot.Status == CaseStatus.InProgress) { snapshot.Status = CaseStatus.ReadyToSettle; results.Add(new CaseObjectResolvedEvent(snapshot.CaseId, Next(), target.ObjectId)); }
            else if (command is SettleCaseCommand && snapshot.Status == CaseStatus.ReadyToSettle) { snapshot.Status = CaseStatus.Closed; snapshot.CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O"); results.Add(new CaseClosedEvent(snapshot.CaseId, Next(), snapshot.CompletedAtUtc)); }
            return results;
        }

        public void Restore(CaseSnapshot restored) { if (restored != null && restored.CaseId == snapshot.CaseId) snapshot = restored.Clone(); }
        private bool CanInspect(string id) => snapshot.Status == CaseStatus.InProgress && !HasClue(id) && (id == "observation-a" || id == "record-fragment" && HasClue("observation-a"));
        private bool HasClue(string id) => snapshot.DiscoveredClues.Contains(id);
        private bool HasItem(string id) => snapshot.HeldItems.Contains(id);
        private long Next() { snapshot.LastSequence++; return snapshot.LastSequence; }
    }
}
