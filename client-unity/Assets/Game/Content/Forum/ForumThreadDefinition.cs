using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoForum.Content
{
    [Serializable]
    public sealed class ForumReplyDefinition
    {
        public string ReplyId;
        public string AuthorId;
        [TextArea] public string Body;
        public string PublishedAtUtc;
        public int Floor;
    }

    [CreateAssetMenu(menuName = "Echo Archive/Forum/Thread Definition", fileName = "ForumThread")]
    public sealed class ForumThreadDefinition : ScriptableObject
    {
        public string ThreadId;
        public string BoardId;
        public string Title;
        public string AuthorId;
        public string PublishedAtUtc;
        [TextArea] public string Body;
        [TextArea] public string Summary;
        public List<string> Tags = new List<string>();
        public bool IsPinned;
        public bool IsOfficial;
        public List<ForumReplyDefinition> InitialReplies = new List<ForumReplyDefinition>();
        public string CaseId;
    }
}
