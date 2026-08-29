using System.Collections.Generic;
using EchoForum.Domain;

namespace EchoForum.Application
{
    /// <summary>
    /// 论坛读取边界。当前由本地快照实现，后续可替换为 API 或缓存适配器。
    /// </summary>
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

    public sealed class ForumQueries
    {
        private readonly IForumCatalog catalog;

        public ForumQueries(IForumCatalog catalog)
        {
            this.catalog = catalog;
        }

        public ForumHomeSnapshot LoadHome()
        {
            return catalog.LoadHome();
        }

        public ForumThread FindThread(string threadId)
        {
            return catalog.FindThread(threadId);
        }
    }
}
