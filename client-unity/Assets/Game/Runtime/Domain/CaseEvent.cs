namespace EchoForum.Domain
{
    /// <summary>
    /// 已发生且不可变的事件记录。单机存档与未来多人同步都围绕它工作。
    /// </summary>
    public abstract class CaseEvent
    {
        protected CaseEvent(string caseId, long sequence)
        {
            CaseId = caseId;
            Sequence = sequence;
        }

        public string CaseId { get; }
        public long Sequence { get; }
    }
    public sealed class CaseStartedEvent : CaseEvent { public CaseStartedEvent(string id, long sequence) : base(id, sequence) { } }
    public sealed class ClueDiscoveredEvent : CaseEvent { public ClueDiscoveredEvent(string id, long sequence, string clueId) : base(id, sequence) { ClueId = clueId; } public string ClueId { get; } }
    public sealed class CaseItemAcquiredEvent : CaseEvent { public CaseItemAcquiredEvent(string id, long sequence, string itemId) : base(id, sequence) { ItemId = itemId; } public string ItemId { get; } }
    public sealed class CaseObjectResolvedEvent : CaseEvent { public CaseObjectResolvedEvent(string id, long sequence, string objectId) : base(id, sequence) { ObjectId = objectId; } public string ObjectId { get; } }
    public sealed class CaseClosedEvent : CaseEvent { public CaseClosedEvent(string id, long sequence, string completedAtUtc) : base(id, sequence) { CompletedAtUtc = completedAtUtc; } public string CompletedAtUtc { get; } }
}
