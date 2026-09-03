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
    }

    [Serializable]
    public sealed class NavigationContext
    {
        public string LastSceneName;
        public string ReturnThreadId;
        public string ActiveCaseId;
    }

    [Serializable]
    public sealed class SaveFlag
    {
        public string Key;
        public string Value;
    }

    /// <summary>Versioned, Unity-independent MVP save payload. Static content is intentionally excluded.</summary>
    [Serializable]
    public sealed class GameSaveData
    {
        public int SaveVersion = 1;
        public PlayerProfile PlayerProfile;
        public ForumState ForumState = new ForumState();
        public List<CaseSnapshot> CaseSnapshots = new List<CaseSnapshot>();
        public List<SaveFlag> GlobalFlags = new List<SaveFlag>();
        public List<SaveFlag> SettingsData = new List<SaveFlag>();
        public NavigationContext Navigation = new NavigationContext();
        public string LastSavedAtUtc;
    }

    public enum GameSaveLoadStatus
    {
        Missing,
        Valid,
        Corrupt
    }

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
