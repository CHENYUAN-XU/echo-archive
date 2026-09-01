using System;
using System.Collections.Generic;

namespace EchoForum.Domain
{
    /// <summary>
    /// 离线论坛快照使用的纯数据模型。它不包含 Unity、存档或网络实现。
    /// </summary>
    public sealed class ForumBoard
    {
        public ForumBoard(string id, string slug, string name, string description)
        {
            Id = id;
            Slug = slug;
            Name = name;
            Description = description;
        }

        public string Id { get; }
        public string Slug { get; }
        public string Name { get; }
        public string Description { get; }
    }

    public sealed class ForumUser
    {
        public ForumUser(string id, string displayName, string presenceLabel, bool isOfficial = false)
        {
            Id = id;
            DisplayName = displayName;
            PresenceLabel = presenceLabel;
            IsOfficial = isOfficial;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public string PresenceLabel { get; }
        public bool IsOfficial { get; }
    }

    public sealed class ForumPost
    {
        public ForumPost(string id, ForumUser author, string body, DateTimeOffset publishedAtUtc)
        {
            Id = id;
            Author = author;
            Body = body;
            PublishedAtUtc = publishedAtUtc;
        }

        public string Id { get; }
        public ForumUser Author { get; }
        public string Body { get; }
        public DateTimeOffset PublishedAtUtc { get; }
    }

    public sealed class ForumReply
    {
        public ForumReply(string id, ForumUser author, string body, DateTimeOffset publishedAtUtc, int floor)
        {
            Id = id;
            Author = author;
            Body = body;
            PublishedAtUtc = publishedAtUtc;
            Floor = floor;
        }

        public string Id { get; }
        public ForumUser Author { get; }
        public string Body { get; }
        public DateTimeOffset PublishedAtUtc { get; }
        public int Floor { get; }
    }

    public sealed class ForumThread
    {
        public ForumThread(
            string id,
            ForumBoard board,
            string title,
            ForumPost originalPost,
            IReadOnlyList<ForumReply> replies,
            IReadOnlyList<string> tags,
            bool isPinned,
            bool isOfficial,
            string summary = null)
        {
            Id = id;
            Board = board;
            Title = title;
            OriginalPost = originalPost;
            Replies = replies;
            Tags = tags;
            IsPinned = isPinned;
            IsOfficial = isOfficial;
            Summary = summary;
        }

        public string Id { get; }
        public ForumBoard Board { get; }
        public string Title { get; }
        public ForumPost OriginalPost { get; }
        public IReadOnlyList<ForumReply> Replies { get; }
        public IReadOnlyList<string> Tags { get; }
        public bool IsPinned { get; }
        public bool IsOfficial { get; }
        public string Summary { get; }
        public int ReplyCount => Replies.Count;
        public DateTimeOffset LastActivityUtc => ReplyCount > 0 ? Replies[ReplyCount - 1].PublishedAtUtc : OriginalPost.PublishedAtUtc;
    }
}
