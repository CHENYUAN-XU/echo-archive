using System;
using System.Collections.Generic;

namespace EchoForum.Domain
{
    [Serializable]
    public sealed class PlayerProfile
    {
        public string PlayerId;
        public string DisplayName;
        public string CreatedAtUtc;
        public string LastPlayedAtUtc;
    }

    [Serializable]
    public sealed class ForumState
    {
        public string LastOpenedThreadId;
        public string PendingNotice;
    }

    [Serializable]
    public sealed class PlayerCreatedThread
    {
        public string ThreadId;
        public string BoardId;
        public string AuthorPlayerId;
        public string AuthorDisplayName;
        public string Title;
        public string Body;
        public string CreatedAtUtc;
    }

    [Serializable]
    public sealed class PlayerCreatedReply
    {
        public string ReplyId;
        public string ThreadId;
        public string AuthorPlayerId;
        public string AuthorDisplayName;
        public string Body;
        public string CreatedAtUtc;
    }

    [Serializable]
    public sealed class SystemCaseReply
    {
        public string ReplyId;
        public string ThreadId;
        public string CaseId;
        public string Body;
        public string CreatedAtUtc;
    }

    [Serializable]
    public sealed class ForumDynamicState
    {
        public List<PlayerCreatedThread> PlayerThreads = new List<PlayerCreatedThread>();
        public List<PlayerCreatedReply> PlayerReplies = new List<PlayerCreatedReply>();
        public List<SystemCaseReply> SystemCaseReplies = new List<SystemCaseReply>();
        public List<string> SystemClosureCaseIds = new List<string>();
    }

    public sealed class ForumThreadDraft
    {
        public ForumThreadDraft(string boardId, string title, string body)
        {
            BoardId = boardId;
            Title = title;
            Body = body;
        }

        public string BoardId { get; }
        public string Title { get; }
        public string Body { get; }
    }

    public sealed class ForumReplyDraft
    {
        public ForumReplyDraft(string threadId, string body)
        {
            ThreadId = threadId;
            Body = body;
        }

        public string ThreadId { get; }
        public string Body { get; }
    }

    public sealed class ForumProfileSummary
    {
        public ForumProfileSummary(string displayName, string createdAtUtc, int threadCount, int replyCount, int inProgressCaseCount, int closedCaseCount)
        {
            DisplayName = displayName;
            CreatedAtUtc = createdAtUtc;
            ThreadCount = threadCount;
            ReplyCount = replyCount;
            InProgressCaseCount = inProgressCaseCount;
            ClosedCaseCount = closedCaseCount;
        }

        public string DisplayName { get; }
        public string CreatedAtUtc { get; }
        public int ThreadCount { get; }
        public int ReplyCount { get; }
        public int InProgressCaseCount { get; }
        public int ClosedCaseCount { get; }
        public bool HasActiveInvestigation => InProgressCaseCount > 0;
    }

    [Serializable]
    public sealed class NavigationContext
    {
        public string LastSceneName;
        public string ReturnThreadId;
        public string ActiveCaseId;
        public string CheckpointId;
    }

    [Serializable]
    public sealed class SaveFlag
    {
        public string Key;
        public string Value;
    }

    /// <summary>Versioned, Unity-independent MVP save payload. Static ScriptableObject content is intentionally excluded.</summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public int SaveVersion = 2;
        public PlayerProfile PlayerProfile;
        public ForumState ForumState = new ForumState();
        public ForumDynamicState ForumDynamicState = new ForumDynamicState();
        public List<CaseSnapshot> CaseSnapshots = new List<CaseSnapshot>();
        public List<SaveFlag> GlobalFlags = new List<SaveFlag>();
        public List<SaveFlag> SettingsData = new List<SaveFlag>();
        public NavigationContext Navigation = new NavigationContext();
        public string LastSavedAtUtc;
    }

    public enum GameSaveLoadStatus { Missing, Valid, Corrupt }

    public sealed class GameSaveLoadResult
    {
        public GameSaveLoadResult(GameSaveLoadStatus status, GameSaveData save, string message)
        {
            Status = status;
            Save = save;
            Message = message;
        }

        public GameSaveLoadStatus Status { get; }
        public GameSaveData Save { get; }
        public string Message { get; }
    }
}