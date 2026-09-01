using UnityEngine;

namespace EchoForum.Content
{
    [CreateAssetMenu(menuName = "Echo Archive/Bindings/Thread Case Binding", fileName = "ThreadCaseBinding")]
    public sealed class ThreadCaseBindingAsset : ScriptableObject
    {
        public string ThreadId;
        public string CaseId;
        public bool AllowsEntry;
        public string NotStartedEntryLabel = "前往现场";
        public string InProgressEntryLabel = "继续调查";
        public string ClosedEntryLabel = "已结案";
        public string UnavailableEntryLabel = "调查区域尚未开放";
        public string UnlockConditionId;
    }
}
