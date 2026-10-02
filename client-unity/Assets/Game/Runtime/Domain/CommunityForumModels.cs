using System;

namespace EchoForum.Domain
{
    public enum ForumConnectionMode { LocalOffline, NodeBbLocal }

    public sealed class CommunityTopicSummary
    {
        public CommunityTopicSummary(string id, ForumBoard board, string title, ForumUser author, string excerpt,
            DateTimeOffset publishedAtUtc, DateTimeOffset lastActivityUtc, int replyCount, bool isPinned)
        {
            Id = id;
            Board = board;
            Title = title;
            Author = author;
            Excerpt = excerpt;
            PublishedAtUtc = publishedAtUtc;
            LastActivityUtc = lastActivityUtc;
            ReplyCount = replyCount;
            IsPinned = isPinned;
        }

        public string Id { get; }
        public ForumBoard Board { get; }
        public string Title { get; }
        public ForumUser Author { get; }
        public string Excerpt { get; }
        public DateTimeOffset PublishedAtUtc { get; }
        public DateTimeOffset LastActivityUtc { get; }
        public int ReplyCount { get; }
        public bool IsPinned { get; }
    }
}
