using System;
using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoForum.Presentation
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ForumPrototypeController : MonoBehaviour
    {
        private ForumQueries queries;
        private ForumCaseUseCases forumCases;
        private IGameFlowCoordinator flow;
        private IGameSession session;
        private string initialThreadId;
        private ForumHomeSnapshot home;
        private VisualElement root;
        private VisualElement boardNavigation;
        private VisualElement threadList;
        private VisualElement onlineUsers;
        private VisualElement hotThreads;
        private VisualElement homeView;
        private VisualElement detailView;
        private VisualElement detailContent;

        public void Initialize(ForumQueries forumQueries, ForumCaseUseCases forumCaseUseCases, IGameFlowCoordinator gameFlow, IGameSession gameSession, string returnThreadId)
        {
            queries = forumQueries;
            forumCases = forumCaseUseCases;
            flow = gameFlow;
            session = gameSession;
            initialThreadId = returnThreadId;
        }

        private void Start()
        {
            if (queries == null || forumCases == null || flow == null || session == null)
            {
                Debug.LogError("ForumPrototypeController requires ForumPrototypeBootstrapper.");
                return;
            }

            root = GetComponent<UIDocument>().rootVisualElement;
            boardNavigation = root.Q<VisualElement>("board-navigation");
            threadList = root.Q<VisualElement>("thread-list");
            onlineUsers = root.Q<VisualElement>("online-users");
            hotThreads = root.Q<VisualElement>("hot-threads");
            homeView = root.Q<VisualElement>("home-view");
            detailView = root.Q<VisualElement>("detail-view");
            detailContent = root.Q<VisualElement>("detail-content");
            var playerStatus = root.Q<Label>("player-status");
            if (playerStatus != null) playerStatus.text = session.HasProfile ? "档案：" + session.PlayerProfile.DisplayName : "档案：访客";

            root.Q<Button>("home-button").clicked += ShowHome;
            root.Q<Button>("back-button").clicked += ShowHome;
            var mainMenu = root.Q<Button>("main-menu-button");
            if (mainMenu != null) mainMenu.clicked += flow.ReturnToMainMenu;
            home = queries.LoadHome();
            BuildHome();
            if (string.IsNullOrEmpty(initialThreadId)) ShowHome(); else ShowThread(initialThreadId);
        }

        private void BuildHome()
        {
            boardNavigation.Clear();
            foreach (var board in home.Boards)
            {
                var boardButton = new Button { text = board.Name };
                boardButton.AddToClassList("board-link");
                boardButton.tooltip = board.Description;
                boardNavigation.Add(boardButton);
            }
            threadList.Clear();
            foreach (var thread in home.Threads) threadList.Add(CreateThreadCard(thread));

            onlineUsers.Clear();
            foreach (var user in home.OnlineUsers)
            {
                var row = new VisualElement(); row.AddToClassList("user-row");
                var dot = new VisualElement(); dot.AddToClassList("presence-dot");
                row.Add(dot); row.Add(CreateLabel(user.DisplayName, "user-name")); row.Add(CreateLabel(user.PresenceLabel, "user-presence"));
                onlineUsers.Add(row);
            }
            hotThreads.Clear();
            for (var index = 0; index < Math.Min(3, home.Threads.Count); index++)
            {
                var thread = home.Threads[index];
                var hot = new Button(() => ShowThread(thread.Id)) { text = thread.Title };
                hot.AddToClassList("hot-thread-link"); hotThreads.Add(hot);
            }
        }

        private VisualElement CreateThreadCard(ForumThread thread)
        {
            var card = new Button(() => ShowThread(thread.Id)); card.AddToClassList("thread-card");
            if (thread.IsPinned) card.AddToClassList("thread-card-pinned");
            var header = new VisualElement(); header.AddToClassList("thread-card-header");
            if (thread.IsOfficial) header.Add(CreateLabel("官方", "official-badge"));
            if (thread.IsPinned) header.Add(CreateLabel("置顶", "pinned-badge"));
            header.Add(CreateLabel(thread.Board.Name, "board-badge")); card.Add(header);
            card.Add(CreateLabel(thread.Title, "thread-title"));
            card.Add(CreateLabel(Excerpt(string.IsNullOrEmpty(thread.Summary) ? thread.OriginalPost.Body : thread.Summary, 62), "thread-excerpt"));
            var footer = new VisualElement(); footer.AddToClassList("thread-card-footer");
            footer.Add(CreateLabel(thread.OriginalPost.Author.DisplayName, "thread-author")); footer.Add(CreateLabel(FormatTime(thread.LastActivityUtc), "thread-time")); footer.Add(CreateLabel($"{thread.ReplyCount} 回复", "thread-replies")); card.Add(footer);
            var tagRow = new VisualElement(); tagRow.AddToClassList("tag-row");
            foreach (var tag in thread.Tags) tagRow.Add(CreateLabel(tag, "tag"));
            card.Add(tagRow);
            return card;
        }

        private void ShowThread(string threadId)
        {
            var thread = queries.FindThread(threadId);
            if (thread == null) return;
            flow.RememberForumThread(threadId);
            detailContent.Clear();
            detailContent.Add(CreateLabel(thread.Board.Name + " / 主题阅读", "detail-kicker"));
            detailContent.Add(CreateLabel(thread.Title, "detail-title"));
            detailContent.Add(CreatePostBlock(thread.OriginalPost.Author, thread.OriginalPost.Body, thread.OriginalPost.PublishedAtUtc, "主楼"));
            detailContent.Add(CreateLabel($"回复 · {thread.ReplyCount}", "reply-heading"));
            foreach (var reply in thread.Replies) detailContent.Add(CreatePostBlock(reply.Author, reply.Body, reply.PublishedAtUtc, $"#{reply.Floor}"));

            var entry = forumCases.GetEntry(thread.Id);
            if (entry.HasBinding)
            {
                if (entry.State == ForumCaseEntryState.Closed && !string.IsNullOrEmpty(entry.CompletionReplyText))
                {
                    var completedAt = DateTimeOffset.TryParse(entry.CompletedAtUtc, out var parsed) ? parsed : DateTimeOffset.UtcNow;
                    detailContent.Add(CreatePostBlock(thread.OriginalPost.Author, entry.CompletionReplyText, completedAt, "系统"));
                }
                var action = new VisualElement(); action.AddToClassList("case-action");
                action.Add(CreateLabel(entry.StatusText, "case-status"));
                var enter = new Button(() => flow.EnterCaseFromThread(thread.Id)) { text = entry.Label };
                enter.SetEnabled(entry.CanEnter); enter.AddToClassList("case-enter-button"); action.Add(enter); detailContent.Add(action);
            }
            homeView.style.display = DisplayStyle.None;
            detailView.style.display = DisplayStyle.Flex;
        }

        private VisualElement CreatePostBlock(ForumUser author, string body, DateTimeOffset publishedAt, string floor)
        {
            var block = new VisualElement(); block.AddToClassList("post-block");
            var meta = new VisualElement(); meta.AddToClassList("post-meta");
            meta.Add(CreateLabel(author.DisplayName, "post-author")); if (author.IsOfficial) meta.Add(CreateLabel("官方帐号", "official-badge"));
            meta.Add(CreateLabel(FormatTime(publishedAt), "post-time")); meta.Add(CreateLabel(floor, "post-floor"));
            block.Add(meta); block.Add(CreateLabel(body, "post-body")); return block;
        }

        private void ShowHome() { homeView.style.display = DisplayStyle.Flex; detailView.style.display = DisplayStyle.None; }
        private static Label CreateLabel(string text, string className) { var label = new Label(text); label.AddToClassList(className); return label; }
        private static string Excerpt(string body, int maximumLength) => body.Length <= maximumLength ? body : body.Substring(0, maximumLength) + "…";
        private static string FormatTime(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("MM-dd HH:mm");
    }
}