using System.Collections.Generic;

namespace EchoForum.Domain
{
    /// <summary>
    /// 可序列化的事件会话快照。具体字段将在玩法原型稳定后再增加。
    /// </summary>
    [System.Serializable]
    public sealed class CaseSnapshot
    {
        public string CaseId;
        public long LastSequence;
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
        public CaseStatus Status;
        public List<string> DiscoveredClues = new List<string>();
        public List<string> HeldItems = new List<string>();
        public string CompletedAtUtc;

        public CaseSnapshot Clone()
        {
            return new CaseSnapshot { CaseId = CaseId, Status = Status, LastSequence = LastSequence, DiscoveredClues = new List<string>(DiscoveredClues ?? new List<string>()), HeldItems = new List<string>(HeldItems ?? new List<string>()), CompletedAtUtc = CompletedAtUtc };
        }

    }
}
