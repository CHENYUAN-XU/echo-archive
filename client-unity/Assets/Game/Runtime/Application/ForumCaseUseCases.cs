using EchoForum.Domain;

namespace EchoForum.Application
{
    public interface ICaseContentCatalog
    {
        CaseDefinition FindCase(string caseId);
        ThreadCaseBinding FindBinding(string threadId);
    }

    public enum ForumCaseEntryState
    {
        None,
        Available,
        InProgress,
        Unavailable,
        Closed
    }

    public sealed class ForumCaseEntry
    {
        public ForumCaseEntry(ForumCaseEntryState state, string threadId, string caseId, string sceneName, string label, string statusText, string completionReplyText, string completedAtUtc)
        {
            State = state;
            ThreadId = threadId;
            CaseId = caseId;
            SceneName = sceneName;
            Label = label;
            StatusText = statusText;
            CompletionReplyText = completionReplyText;
            CompletedAtUtc = completedAtUtc;
        }

        public ForumCaseEntryState State { get; }
        public string ThreadId { get; }
        public string CaseId { get; }
        public string SceneName { get; }
        public string Label { get; }
        public string StatusText { get; }
        public string CompletionReplyText { get; }
        public string CompletedAtUtc { get; }
        public bool HasBinding => State != ForumCaseEntryState.None;
        public bool CanEnter => State == ForumCaseEntryState.Available || State == ForumCaseEntryState.InProgress;
    }

    public sealed class ForumCaseUseCases
    {
        private readonly ICaseContentCatalog content;
        private readonly CaseUseCases cases;

        public ForumCaseUseCases(ICaseContentCatalog content, CaseUseCases cases)
        {
            this.content = content;
            this.cases = cases;
        }

        public ForumCaseEntry GetEntry(string threadId)
        {
            var binding = content.FindBinding(threadId);
            if (binding == null) return new ForumCaseEntry(ForumCaseEntryState.None, threadId, null, null, null, null, null, null);

            var definition = content.FindCase(binding.CaseId);
            if (definition == null || !binding.AllowsEntry || !definition.IsAvailable || string.IsNullOrEmpty(definition.SceneName))
            {
                return new ForumCaseEntry(ForumCaseEntryState.Unavailable, binding.ThreadId, binding.CaseId, null, binding.UnavailableEntryLabel, "事件状态：暂不可用", null, null);
            }

            var snapshot = cases.GetSnapshot(binding.CaseId);
            if (snapshot.Status == CaseStatus.Closed)
            {
                return new ForumCaseEntry(ForumCaseEntryState.Closed, binding.ThreadId, binding.CaseId, definition.SceneName, binding.ClosedEntryLabel, definition.CompletionStatusText, definition.CompletionReplyText, snapshot.CompletedAtUtc);
            }

            if (snapshot.Status == CaseStatus.InProgress || snapshot.Status == CaseStatus.ReadyToSettle)
            {
                return new ForumCaseEntry(ForumCaseEntryState.InProgress, binding.ThreadId, binding.CaseId, definition.SceneName, binding.InProgressEntryLabel, "事件状态：进行中", null, null);
            }

            return new ForumCaseEntry(ForumCaseEntryState.Available, binding.ThreadId, binding.CaseId, definition.SceneName, binding.NotStartedEntryLabel, "事件状态：可调查", null, null);
        }

        public ForumCaseEntry RequestEntry(string threadId)
        {
            var entry = GetEntry(threadId);
            if (!entry.CanEnter) return entry;
            if (cases.GetSnapshot(entry.CaseId).Status == CaseStatus.NotStarted) cases.Start(entry.CaseId);
            return GetEntry(threadId);
        }
    }
}
