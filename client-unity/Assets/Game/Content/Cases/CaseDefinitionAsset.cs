using UnityEngine;

namespace EchoForum.Content
{
    [CreateAssetMenu(menuName = "Echo Archive/Cases/Case Definition", fileName = "CaseDefinition")]
    public sealed class CaseDefinitionAsset : ScriptableObject
    {
        public string CaseId;
        public string DisplayName;
        public string InvestigationSceneName;
        public bool IsAvailableInCurrentBuild;
        [TextArea] public string RuleSummary;
        public string StartConditionId;
        [TextArea] public string CompletionStatusText;
        [TextArea] public string CompletionReplyText;
    }
}
