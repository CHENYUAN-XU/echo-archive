namespace EchoForum.Domain
{
    /// <summary>
    /// 对一次事件会话的意图。输入、UI 和未来网络层都只提交命令，
    /// 不直接篡改事件状态。
    /// </summary>
    public abstract class CaseCommand
    {
        protected CaseCommand(string caseId)
        {
            CaseId = caseId;
        }

        public string CaseId { get; }
    }
}
