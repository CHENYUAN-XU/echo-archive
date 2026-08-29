using System.Collections.Generic;

namespace EchoForum.Domain
{
    /// <summary>
    /// 可序列化的事件会话快照。具体字段将在玩法原型稳定后再增加。
    /// </summary>
    public sealed class CaseSnapshot
    {
        public string CaseId { get; set; }
        public long LastSequence { get; set; }
        public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();
    }
}
