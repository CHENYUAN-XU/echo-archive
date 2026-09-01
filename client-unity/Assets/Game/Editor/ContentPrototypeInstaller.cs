using System;
using System.Collections.Generic;
using EchoForum.Content;
using EchoForum.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace EchoForum.Editor
{
    public static class ContentPrototypeInstaller
    {
        [MenuItem("Echo Archive/Content/Create Or Update Prototype Content")]
        public static void CreateOrUpdate()
        {
            EnsureFolders();
            CreateBoards();
            CreateUsers();
            CreateThreads();
            CreateCases();
            CreateBindings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ContentBindingValidator.Validate();
        }

        [MenuItem("Echo Archive/Development/Reset Prototype Progress")]
        public static void ResetPrototypeProgress()
        {
            new JsonCaseSaveStore().ResetPrototypeProgress();
            Debug.Log("Prototype case progress was reset. This menu action is development-only and never runs at startup.");
        }

        private static void CreateBoards()
        {
            Set(Board("Forum/BoardArchive"), "board-archive", "archive", "留声档案", "整理、检索与保存异常记录的公共板块。");
            Set(Board("Forum/BoardExchange"), "board-exchange", "exchange", "夜间交流", "关于设备、地点与记录方法的日常讨论。");
            Set(Board("Forum/BoardNotice"), "board-notice", "notice", "站务公告", "论坛运行说明与离线快照提示。");
        }

        private static void CreateUsers()
        {
            Set(User("Forum/UserCaretaker"), "user-caretaker", "archive_caretaker", "正在巡检", true);
            Set(User("Forum/UserLantern"), "user-lantern", "灯下记录员", "在线", false);
            Set(User("Forum/UserReed"), "user-reed", "芦苇", "在线", false);
            Set(User("Forum/UserNorth"), "user-north", "北窗", "离开 8 分钟前", false);
            Set(User("Forum/UserPlayer"), "user-player", "访客", "离线快照", false);
        }

        private static void CreateThreads()
        {
            Set(Thread("Forum/ThreadGuidelines"), "thread-guidelines", "board-notice", "新访客须知：记录、引用与归档规范", "user-caretaker", "2026-08-29T08:12:00Z", "欢迎进入留声论坛。本地演示仅提供预置内容；登录、发帖与事件入口会在后续版本开放。请先阅读板块说明，再开始整理自己的记录。", "离线快照中的测试事件 A 可由绑定配置提供入口。", new List<string> { "置顶", "站务", "离线快照" }, true, true, "case-observation-01", Replies(("reply-guidelines-1", "user-lantern", "收到。离线状态也能把旧帖读完，这点很实用。", "2026-08-29T08:31:00Z", 2), ("reply-guidelines-2", "user-caretaker", "已更新索引说明。", "2026-08-29T09:04:00Z", 3)));
            Set(Thread("Forum/ThreadTapes"), "thread-tapes", "board-archive", "一批旧磁带的编号记录方式", "user-lantern", "2026-08-29T21:16:00Z", "整理旧介质时，先按来源、日期和载体编号。缺失信息用空位标记，不要为了看起来完整而补写。", "关于介质编号的中性测试讨论。", new List<string> { "介质", "整理", "方法" }, false, false, null, Replies(("reply-tapes-1", "user-reed", "我会额外记下播放设备，之后复查时比较方便。", "2026-08-29T21:42:00Z", 2), ("reply-tapes-2", "user-north", "同意，环境噪声也值得写一笔。", "2026-08-29T22:10:00Z", 3), ("reply-tapes-3", "user-lantern", "已把模板放在首楼末尾，欢迎补充。", "2026-08-29T22:28:00Z", 4)));
            Set(Thread("Forum/ThreadCorridor"), "thread-corridor", "board-exchange", "请教：楼道回声应该怎样标注？", "user-reed", "2026-08-29T23:02:00Z", "同一段录音在不同时间听起来不太一致。我目前只记地点和时长，是否还应该记录门窗、人数和天气？", "关于记录方法的中性问题。", new List<string> { "求助", "记录", "声音" }, false, false, null, Replies(("reply-corridor-1", "user-lantern", "建议写下录音方向和设备距离，其他条件按实际情况补充。", "2026-08-29T23:18:00Z", 2), ("reply-corridor-2", "user-north", "先做连续几天的对照，别急着给现象命名。", "2026-08-29T23:44:00Z", 3)));
            Set(Thread("Forum/ThreadIndex"), "thread-index", "board-archive", "[索引] 本周新增的公开条目", "user-caretaker", "2026-08-29T18:40:00Z", "本周索引已同步到本地快照。这里列出的是分类入口，不代表任何结论或处理状态。", "官方索引测试帖。", new List<string> { "索引", "官方" }, false, true, null, Replies(("reply-index-1", "user-player", "索引页加载正常。", "2026-08-29T19:07:00Z", 2)));
            Set(Thread("Forum/ThreadFuture"), "thread-future-observation", "board-archive", "测试事件 B：占位调查入口", "user-caretaker", "2026-08-30T00:10:00Z", "此帖用于验证尚未开放的内容绑定。它不包含正式地点、人物或事件叙事。", "用于验证不可用调查入口的内容配置。", new List<string> { "测试事件", "占位" }, false, true, "case-observation-02", Replies());
        }

        private static void CreateCases()
        {
            Set(Case("Cases/CaseObservationA"), "case-observation-01", "测试事件 A", "InvestigationPrototype", true, "观测规则：按既有原型完成三段中性测试交互。", "prototype-ready", "事件状态：已结案", "系统结案标记：测试事件 A 已完成。");
            Set(Case("Cases/CaseObservationB"), "case-observation-02", "测试事件 B", string.Empty, false, "观测规则：占位调查入口。", "future-content", "事件状态：暂不可用", "系统结案标记：测试事件 B 已完成。");
        }

        private static void CreateBindings()
        {
            Set(Binding("Bindings/BindingObservationA"), "thread-guidelines", "case-observation-01", true, "前往现场", "继续调查", "已结案", "调查区域尚未开放", "prototype-ready");
            Set(Binding("Bindings/BindingObservationB"), "thread-future-observation", "case-observation-02", false, "前往现场", "继续调查", "已结案", "调查区域尚未开放", "future-content");
        }

        private static List<ForumReplyDefinition> Replies(params (string id, string author, string body, string time, int floor)[] values)
        {
            var replies = new List<ForumReplyDefinition>();
            foreach (var value in values) replies.Add(new ForumReplyDefinition { ReplyId = value.id, AuthorId = value.author, Body = value.body, PublishedAtUtc = value.time, Floor = value.floor });
            return replies;
        }

        private static ForumBoardDefinition Board(string key) => GetOrCreate<ForumBoardDefinition>("Assets/Game/Content/Resources/" + key + ".asset");
        private static ForumUserDefinition User(string key) => GetOrCreate<ForumUserDefinition>("Assets/Game/Content/Resources/" + key + ".asset");
        private static ForumThreadDefinition Thread(string key) => GetOrCreate<ForumThreadDefinition>("Assets/Game/Content/Resources/" + key + ".asset");
        private static CaseDefinitionAsset Case(string key) => GetOrCreate<CaseDefinitionAsset>("Assets/Game/Content/Resources/" + key + ".asset");
        private static ThreadCaseBindingAsset Binding(string key) => GetOrCreate<ThreadCaseBindingAsset>("Assets/Game/Content/Resources/" + key + ".asset");

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void Set(ForumBoardDefinition asset, string id, string slug, string name, string description) { asset.BoardId = id; asset.Slug = slug; asset.DisplayName = name; asset.Description = description; EditorUtility.SetDirty(asset); }
        private static void Set(ForumUserDefinition asset, string id, string name, string presence, bool official) { asset.UserId = id; asset.DisplayName = name; asset.PresenceLabel = presence; asset.IsOfficial = official; EditorUtility.SetDirty(asset); }
        private static void Set(ForumThreadDefinition asset, string id, string boardId, string title, string authorId, string publishedAt, string body, string summary, List<string> tags, bool pinned, bool official, string caseId, List<ForumReplyDefinition> replies) { asset.ThreadId = id; asset.BoardId = boardId; asset.Title = title; asset.AuthorId = authorId; asset.PublishedAtUtc = publishedAt; asset.Body = body; asset.Summary = summary; asset.Tags = tags; asset.IsPinned = pinned; asset.IsOfficial = official; asset.CaseId = caseId; asset.InitialReplies = replies; EditorUtility.SetDirty(asset); }
        private static void Set(CaseDefinitionAsset asset, string id, string name, string scene, bool available, string rules, string startCondition, string completionStatus, string completionReply) { asset.CaseId = id; asset.DisplayName = name; asset.InvestigationSceneName = scene; asset.IsAvailableInCurrentBuild = available; asset.RuleSummary = rules; asset.StartConditionId = startCondition; asset.CompletionStatusText = completionStatus; asset.CompletionReplyText = completionReply; EditorUtility.SetDirty(asset); }
        private static void Set(ThreadCaseBindingAsset asset, string threadId, string caseId, bool allowed, string notStarted, string inProgress, string closed, string unavailable, string unlock) { asset.ThreadId = threadId; asset.CaseId = caseId; asset.AllowsEntry = allowed; asset.NotStartedEntryLabel = notStarted; asset.InProgressEntryLabel = inProgress; asset.ClosedEntryLabel = closed; asset.UnavailableEntryLabel = unavailable; asset.UnlockConditionId = unlock; EditorUtility.SetDirty(asset); }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Game/Content/Resources");
            EnsureFolder("Assets/Game/Content/Resources/Forum");
            EnsureFolder("Assets/Game/Content/Resources/Cases");
            EnsureFolder("Assets/Game/Content/Resources/Bindings");
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
