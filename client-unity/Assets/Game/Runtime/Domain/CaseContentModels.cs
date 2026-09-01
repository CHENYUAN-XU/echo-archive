namespace EchoForum.Domain
{
    /// <summary>Pure published case metadata. Unity assets are converted into this model by Infrastructure.</summary>
    public sealed class CaseDefinition
    {
        public CaseDefinition(string caseId, string displayName, string sceneName, bool isAvailable, string ruleSummary, string startConditionId, string completionStatusText, string completionReplyText)
        {
            CaseId = caseId;
            DisplayName = displayName;
            SceneName = sceneName;
            IsAvailable = isAvailable;
            RuleSummary = ruleSummary;
            StartConditionId = startConditionId;
            CompletionStatusText = completionStatusText;
            CompletionReplyText = completionReplyText;
        }

        public string CaseId { get; }
        public string DisplayName { get; }
        public string SceneName { get; }
        public bool IsAvailable { get; }
        public string RuleSummary { get; }
        public string StartConditionId { get; }
        public string CompletionStatusText { get; }
        public string CompletionReplyText { get; }
    }

    /// <summary>Associates one forum thread with one published case without coupling forum UI to either ID.</summary>
    public sealed class ThreadCaseBinding
    {
        public ThreadCaseBinding(string threadId, string caseId, bool allowsEntry, string notStartedEntryLabel, string inProgressEntryLabel, string closedEntryLabel, string unavailableEntryLabel, string unlockConditionId)
        {
            ThreadId = threadId;
            CaseId = caseId;
            AllowsEntry = allowsEntry;
            NotStartedEntryLabel = notStartedEntryLabel;
            InProgressEntryLabel = inProgressEntryLabel;
            ClosedEntryLabel = closedEntryLabel;
            UnavailableEntryLabel = unavailableEntryLabel;
            UnlockConditionId = unlockConditionId;
        }

        public string ThreadId { get; }
        public string CaseId { get; }
        public bool AllowsEntry { get; }
        public string NotStartedEntryLabel { get; }
        public string InProgressEntryLabel { get; }
        public string ClosedEntryLabel { get; }
        public string UnavailableEntryLabel { get; }
        public string UnlockConditionId { get; }
    }
}
