using System;
using System.Collections.Generic;
using EchoForum.Application;
using EchoForum.Domain;

namespace EchoForum.Infrastructure
{
    /// <summary>
    /// P1 阶段的离线快照。替换为本地存档或 HTTP 读取时，Presentation 无需改变。
    /// </summary>
    public sealed class LocalForumCatalog : IForumCatalog
    {
        private readonly ForumHomeSnapshot home;
        private readonly Dictionary<string, ForumThread> threadsById;

        public LocalForumCatalog()
        {
            var archive = new ForumBoard("board-archive", "archive", "留声档案", "整理、检索与保存异常记录的公共板块。");
            var exchange = new ForumBoard("board-exchange", "exchange", "夜间交流", "关于设备、地点与记录方法的日常讨论。");
            var notice = new ForumBoard("board-notice", "notice", "站务公告", "论坛运行说明与离线快照提示。");

            var caretaker = new ForumUser("user-caretaker", "archive_caretaker", "正在巡检", true);
            var lantern = new ForumUser("user-lantern", "灯下记录员", "在线");
            var reed = new ForumUser("user-reed", "芦苇", "在线");
            var north = new ForumUser("user-north", "北窗", "离开 8 分钟前");
            var player = new ForumUser("user-player", "访客", "离线快照");

            var threads = new List<ForumThread>
            {
                new ForumThread(
                    "thread-guidelines",
                    notice,
                    "新访客须知：记录、引用与归档规范",
                    new ForumPost("post-guidelines", caretaker, "欢迎进入留声论坛。本地演示仅提供预置内容；登录、发帖与事件入口会在后续版本开放。请先阅读板块说明，再开始整理自己的记录。", Utc(8, 12)),
                    new List<ForumReply>
                    {
                        new ForumReply("reply-guidelines-1", lantern, "收到。离线状态也能把旧帖读完，这点很实用。", Utc(8, 31), 2),
                        new ForumReply("reply-guidelines-2", caretaker, "已更新索引说明。", Utc(9, 4), 3)
                    },
                    new List<string> { "置顶", "站务", "离线快照" },
                    true,
                    true),
                new ForumThread(
                    "thread-tapes",
                    archive,
                    "一批旧磁带的编号记录方式",
                    new ForumPost("post-tapes", lantern, "整理旧介质时，先按来源、日期和载体编号。缺失信息用空位标记，不要为了看起来完整而补写。", Utc(21, 16)),
                    new List<ForumReply>
                    {
                        new ForumReply("reply-tapes-1", reed, "我会额外记下播放设备，之后复查时比较方便。", Utc(21, 42), 2),
                        new ForumReply("reply-tapes-2", north, "同意，环境噪声也值得写一笔。", Utc(22, 10), 3),
                        new ForumReply("reply-tapes-3", lantern, "已把模板放在首楼末尾，欢迎补充。", Utc(22, 28), 4)
                    },
                    new List<string> { "介质", "整理", "方法" },
                    false,
                    false),
                new ForumThread(
                    "thread-corridor",
                    exchange,
                    "请教：楼道回声应该怎样标注？",
                    new ForumPost("post-corridor", reed, "同一段录音在不同时间听起来不太一致。我目前只记地点和时长，是否还应该记录门窗、人数和天气？", Utc(23, 2)),
                    new List<ForumReply>
                    {
                        new ForumReply("reply-corridor-1", lantern, "建议写下录音方向和设备距离，其他条件按实际情况补充。", Utc(23, 18), 2),
                        new ForumReply("reply-corridor-2", north, "先做连续几天的对照，别急着给现象命名。", Utc(23, 44), 3)
                    },
                    new List<string> { "求助", "记录", "声音" },
                    false,
                    false),
                new ForumThread(
                    "thread-index",
                    archive,
                    "[索引] 本周新增的公开条目",
                    new ForumPost("post-index", caretaker, "本周索引已同步到本地快照。这里列出的是分类入口，不代表任何结论或处理状态。", Utc(18, 40)),
                    new List<ForumReply>
                    {
                        new ForumReply("reply-index-1", player, "索引页加载正常。", Utc(19, 7), 2)
                    },
                    new List<string> { "索引", "官方" },
                    false,
                    true)
            };

            home = new ForumHomeSnapshot(
                new List<ForumBoard> { archive, exchange, notice },
                threads,
                new List<ForumUser> { caretaker, lantern, reed, north });
            threadsById = new Dictionary<string, ForumThread>();
            foreach (var thread in threads)
            {
                threadsById.Add(thread.Id, thread);
            }
        }

        public ForumHomeSnapshot LoadHome()
        {
            return home;
        }

        public ForumThread FindThread(string threadId)
        {
            ForumThread thread;
            return threadId != null && threadsById.TryGetValue(threadId, out thread) ? thread : null;
        }

        private static DateTimeOffset Utc(int hour, int minute)
        {
            return new DateTimeOffset(2026, 8, 29, hour, minute, 0, TimeSpan.Zero);
        }
    }
}
