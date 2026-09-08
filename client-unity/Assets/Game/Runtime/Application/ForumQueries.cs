using System;
using System.Collections.Generic;
using System.Globalization;
using EchoForum.Domain;

namespace EchoForum.Application
{
    /// <summary>Read-only static content boundary. Implementations may be local content or a future remote cache.</summary>
    public interface IForumCatalog
    {
        ForumHomeSnapshot LoadHome();
        ForumThread FindThread(string threadId);
    }

    public sealed class ForumHomeSnapshot
    {
        public ForumHomeSnapshot(IReadOnlyList<ForumBoard> boards, IReadOnlyList<ForumThread> threads, IReadOnlyList<ForumUser> onlineUsers)
        {
            Boards = boards;
            Threads = threads;
            OnlineUsers = onlineUsers;
        }

        public IReadOnlyList<ForumBoard> Boards { get; }
        public IReadOnlyList<ForumThread> Threads { get; }
        public IReadOnlyList<ForumUser> OnlineUsers { get; }
    }

    /// <summary>Application forum use cases. It merges immutable content with the current profile's saved local contributions.</summary>
    public sealed class ForumQueries
    {
        private readonly IForumCatalog catalog;
        private readonly IGameSession session;

        public ForumQueries(IForumCatalog catalog, IGameSession session = null)
        {
            this.catalog = catalog;
            this.session = session;
        }

        public ForumHomeSnapshot LoadHome()
        {
            var contentHome = catalog.LoadHome();
            var threads = new List<ForumThread>();
            foreach (var thread in contentHome.Threads) threads.Add(MergeStaticThread(thread));
            if (session != null && session.HasProfile)
            {
                foreach (var thread in session.ForumDynamicState.PlayerThreads)
                {
                    var created = BuildPlayerThread(thread, contentHome.Boards);
                    if (created != null) threads.Add(created);
                }
            }
            threads.Sort((left, right) => right.LastActivityUtc.CompareTo(left.LastActivityUtc));
            return new ForumHomeSnapshot(contentHome.Boards, threads, contentHome.OnlineUsers);
        }

        public ForumThread FindThread(string threadId)
        {
            if (string.IsNullOrWhiteSpace(threadId)) return null;
            var staticThread = catalog.FindThread(threadId);
            if (staticThread != null) return MergeStaticThread(staticThread);
            if (session == null || !session.HasProfile) return null;
            foreach (var thread in session.ForumDynamicState.PlayerThreads)
            {
                if (thread != null && thread.ThreadId == threadId) return BuildPlayerThread(thread, catalog.LoadHome().Boards);
            }
            return null;
        }

        public bool TryCreateThread(ForumThreadDraft draft, out ForumThread thread, out string error)
        {
            thread = null;
            if (session == null || !session.HasProfile) { error = "请先创建本地档案。"; return false; }
            if (!BoardExists(draft == null ? null : draft.BoardId)) { error = "请选择有效板块。"; return false; }
            if (!session.TryCreateForumThread(draft, out var created, out error)) return false;
            thread = BuildPlayerThread(created, catalog.LoadHome().Boards);
            return thread != null;
        }

        public bool TryCreateReply(ForumReplyDraft draft, out ForumReply reply, out string error)
        {
            reply = null;
            if (FindThread(draft == null ? null : draft.ThreadId) == null) { error = "未找到回复主题。"; return false; }
            if (session == null) { error = "请先创建本地档案。"; return false; }
            if (!session.TryCreateForumReply(draft, out var created, out error)) return false;
            reply = BuildPlayerReply(created, NextFloor(draft.ThreadId));
            return true;
        }

        public ForumProfileSummary GetProfileSummary()
        {
            if (session == null || !session.HasProfile) return new ForumProfileSummary("访客", null, 0, 0, 0, 0);
            var active = 0;
            var closed = 0;
            foreach (var snapshot in session.CaseSnapshots)
            {
                if (snapshot == null) continue;
                if (snapshot.Status == CaseStatus.InProgress || snapshot.Status == CaseStatus.ReadyToSettle) active++;
                if (snapshot.Status == CaseStatus.Closed) closed++;
            }
            return new ForumProfileSummary(session.PlayerProfile.DisplayName, session.PlayerProfile.CreatedAtUtc, session.ForumDynamicState.PlayerThreads.Count, session.ForumDynamicState.PlayerReplies.Count, active, closed);
        }

        public string ConsumeForumNotice()
        {
            if (session == null || !session.HasProfile || string.IsNullOrWhiteSpace(session.ForumState.PendingNotice)) return null;
            var notice = session.ForumState.PendingNotice;
            session.SetForumNotice(null);
            return notice;
        }

        private ForumThread MergeStaticThread(ForumThread thread)
        {
            if (session == null || !session.HasProfile) return thread;
            var replies = new List<ForumReply>(thread.Replies);
            AddDynamicReplies(thread.Id, replies);
            return new ForumThread(thread.Id, thread.Board, thread.Title, thread.OriginalPost, replies, thread.Tags, thread.IsPinned, thread.IsOfficial, thread.Summary);
        }

        private ForumThread BuildPlayerThread(PlayerCreatedThread created, IReadOnlyList<ForumBoard> boards)
        {
            if (created == null) return null;
            ForumBoard board = null;
            foreach (var candidate in boards) if (candidate.Id == created.BoardId) { board = candidate; break; }
            if (board == null) return null;
            var author = new ForumUser(created.AuthorPlayerId, created.AuthorDisplayName, "本地发表");
            var replies = new List<ForumReply>();
            AddDynamicReplies(created.ThreadId, replies);
            return new ForumThread(created.ThreadId, board, created.Title, new ForumPost("post-" + created.ThreadId, author, created.Body, ParseUtc(created.CreatedAtUtc)), replies, new List<string> { "本地记录" }, false, false, created.Body);
        }

        private void AddDynamicReplies(string threadId, List<ForumReply> replies)
        {
            if (session == null || !session.HasProfile) return;
            var dynamicReplies = new List<ForumReply>();
            foreach (var system in session.ForumDynamicState.SystemCaseReplies)
            {
                if (system != null && system.ThreadId == threadId)
                {
                    dynamicReplies.Add(new ForumReply(system.ReplyId, new ForumUser("local-system", "本地系统", "结案记录", true), system.Body, ParseUtc(system.CreatedAtUtc), 0));
                }
            }
            foreach (var player in session.ForumDynamicState.PlayerReplies)
            {
                if (player != null && player.ThreadId == threadId) dynamicReplies.Add(BuildPlayerReply(player, 0));
            }
            dynamicReplies.Sort((left, right) => left.PublishedAtUtc.CompareTo(right.PublishedAtUtc));
            var nextFloor = replies.Count + 2;
            foreach (var reply in dynamicReplies)
            {
                replies.Add(new ForumReply(reply.Id, reply.Author, reply.Body, reply.PublishedAtUtc, nextFloor++));
            }
        }

        private int NextFloor(string threadId)
        {
            var thread = FindThread(threadId);
            return thread == null ? 2 : thread.ReplyCount + 2;
        }

        private bool BoardExists(string boardId)
        {
            if (string.IsNullOrWhiteSpace(boardId)) return false;
            foreach (var board in catalog.LoadHome().Boards) if (board.Id == boardId) return true;
            return false;
        }

        private static ForumReply BuildPlayerReply(PlayerCreatedReply reply, int floor)
        {
            return new ForumReply(reply.ReplyId, new ForumUser(reply.AuthorPlayerId, reply.AuthorDisplayName, "本地发表"), reply.Body, ParseUtc(reply.CreatedAtUtc), floor);
        }

        private static DateTimeOffset ParseUtc(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var time) ? time : DateTimeOffset.UtcNow;
        }
    }
}