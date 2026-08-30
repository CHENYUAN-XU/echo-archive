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
    public sealed class StartCaseCommand : CaseCommand { public StartCaseCommand(string caseId) : base(caseId) { } }
    public sealed class InvestigateCaseCommand : CaseCommand { public InvestigateCaseCommand(string caseId, string clueId) : base(caseId) { ClueId = clueId; } public string ClueId { get; } }
    public sealed class AcquireCaseItemCommand : CaseCommand { public AcquireCaseItemCommand(string caseId, string itemId) : base(caseId) { ItemId = itemId; } public string ItemId { get; } }
    public sealed class ResolveCaseObjectCommand : CaseCommand { public ResolveCaseObjectCommand(string caseId, string objectId) : base(caseId) { ObjectId = objectId; } public string ObjectId { get; } }
    public sealed class SettleCaseCommand : CaseCommand { public SettleCaseCommand(string caseId) : base(caseId) { } }
}
