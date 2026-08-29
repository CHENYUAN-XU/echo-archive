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
}
