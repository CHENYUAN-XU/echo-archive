using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine.UIElements;

namespace EchoForum.Presentation
{
    /// <summary>UI Toolkit presentation for the remote community. It never reads saves or HTTP payloads.</summary>
    internal sealed class CommunityForumPresenter
    {
        private readonly CommunityForumUseCases forum;
        private readonly VisualElement home;
        private readonly VisualElement compose;
        private readonly VisualElement detail;
        private readonly VisualElement boardList;
        private readonly VisualElement topicList;
        private readonly VisualElement detailContent;
        private readonly VisualElement authCard;
        private readonly VisualElement authForm;
        private readonly Label status;
        private readonly Label accountName;
        private readonly Label boardTitle;
        private readonly TextField username;
        private readonly TextField email;
        private readonly TextField password;
        private readonly Toggle consent;
        private readonly DropdownField composeBoard;
        private readonly TextField composeTitle;
        private readonly TextField composeBody;
        private readonly TextField replyBody;
        private readonly Button logoutButton;
        private readonly Button authToggleButton;
        private readonly Button newTopicButton;
        private readonly Button publishReplyButton;
        private readonly Dictionary<string, string> boardIdsByName = new Dictionary<string, string>();
        private IReadOnlyList<ForumBoard> boards = new List<ForumBoard>();
        private string selectedBoardId;
        private string selectedThreadId;
        private bool hasOpened;
        private bool authExpanded;

        public CommunityForumPresenter(VisualElement root, CommunityForumUseCases forum)
        {
            this.forum = forum;
            home = root.Q<VisualElement>("community-home");
            compose = root.Q<VisualElement>("community-compose");
            detail = root.Q<VisualElement>("community-detail");
            boardList = root.Q<VisualElement>("community-board-list");
            topicList = root.Q<VisualElement>("community-topic-list");
            detailContent = root.Q<VisualElement>("community-detail-content");
            authCard = root.Q<VisualElement>("community-auth-card");
            authForm = root.Q<VisualElement>("community-auth-form");
            status = root.Q<Label>("community-status");
            accountName = root.Q<Label>("community-account-name");
            boardTitle = root.Q<Label>("community-board-title");
            username = root.Q<TextField>("community-username");
            email = root.Q<TextField>("community-email");
            password = root.Q<TextField>("community-password");
            password.isPasswordField = true;
            consent = root.Q<Toggle>("community-consent");
            composeBoard = root.Q<DropdownField>("community-compose-board");
            composeTitle = root.Q<TextField>("community-compose-title");
            composeBody = root.Q<TextField>("community-compose-body");
            replyBody = root.Q<TextField>("community-reply-body");
            logoutButton = root.Q<Button>("community-logout-button");
            authToggleButton = root.Q<Button>("community-auth-toggle");
            newTopicButton = root.Q<Button>("community-new-topic-button");
            publishReplyButton = root.Q<Button>("community-publish-reply");

            root.Q<Button>("community-refresh-button").clicked += () => Run(RefreshAsync);
            root.Q<Button>("community-login-button").clicked += () => Run(LoginAsync);
            root.Q<Button>("community-register-button").clicked += () => Run(RegisterAsync);
            logoutButton.clicked += () => Run(LogoutAsync);
            authToggleButton.clicked += () => { authExpanded = !authExpanded; UpdateAuth(); };
            newTopicButton.clicked += ShowCompose;
            root.Q<Button>("community-compose-back").clicked += ShowHome;
            root.Q<Button>("community-cancel-topic").clicked += ShowHome;
            root.Q<Button>("community-publish-topic").clicked += () => Run(PublishTopicAsync);
            root.Q<Button>("community-detail-back").clicked += ShowHome;
            publishReplyButton.clicked += () => Run(PublishReplyAsync);
            UpdateAuth();
            ShowHome();
        }

        public void Open()
        {
            if (hasOpened) { Run(RefreshAsync); return; }
            hasOpened = true;
            Run(LoadBoardsAsync);
        }

        private async Task RefreshAsync()
        {
            if (detail.style.display.value == DisplayStyle.Flex && !string.IsNullOrEmpty(selectedThreadId))
                await OpenThreadAsync(selectedThreadId);
            else await LoadBoardsAsync();
        }

        private async Task LoadBoardsAsync()
        {
            SetStatus("正在连接 NodeBB，读取公开板块…");
            var result = await forum.GetBoardsAsync();
            if (!result.Succeeded) { ShowError(result.Message); return; }
            boards = result.Value;
            boardList.Clear();
            boardIdsByName.Clear();
            var names = new List<string>();
            foreach (var board in boards)
            {
                names.Add(board.Name);
                boardIdsByName[board.Name] = board.Id;
                var button = new Button(() => Run(() => LoadTopicsAsync(board))) { text = board.Name, tooltip = board.Description, enableRichText = false };
                button.AddToClassList("board-link");
                boardList.Add(button);
            }
            composeBoard.choices = names;
            if (boards.Count == 0)
            {
                selectedBoardId = null;
                boardTitle.text = "暂无公开板块";
                topicList.Clear();
                SetStatus("NodeBB 已连接，当前没有可浏览的公开板块。");
                ShowHome();
                return;
            }
            ForumBoard selected = null;
            foreach (var board in boards) if (board.Id == selectedBoardId) { selected = board; break; }
            if (selected == null) selected = boards[0];
            await LoadTopicsAsync(selected);
        }

        private async Task LoadTopicsAsync(ForumBoard board)
        {
            selectedBoardId = board.Id;
            boardTitle.text = board.Name;
            composeBoard.value = board.Name;
            SetStatus("正在读取“" + board.Name + "”的真实主题…");
            var result = await forum.GetTopicsAsync(board.Id);
            if (!result.Succeeded) { ShowError(result.Message); return; }
            topicList.Clear();
            foreach (var topic in result.Value) topicList.Add(TopicCard(topic));
            if (result.Value.Count == 0) topicList.Add(Label("这个板块还没有主题。", "empty-state"));
            SetStatus("已从 NodeBB 读取 " + result.Value.Count + " 条主题 · 点击刷新可获取浏览器端的新内容。");
            ShowHome();
        }

        private VisualElement TopicCard(CommunityTopicSummary topic)
        {
            var card = new Button(() => Run(() => OpenThreadAsync(topic.Id)));
            card.AddToClassList("thread-card");
            if (topic.IsPinned) card.AddToClassList("thread-card-pinned");
            var header = new VisualElement(); header.AddToClassList("thread-card-header");
            header.Add(Label(topic.Board.Name, "board-badge"));
            if (topic.IsPinned) header.Add(Label("置顶", "pinned-badge"));
            card.Add(header);
            card.Add(Label(topic.Title, "thread-title"));
            card.Add(Label(Excerpt(topic.Excerpt, 130), "thread-excerpt"));
            var footer = new VisualElement(); footer.AddToClassList("thread-card-footer");
            footer.Add(Label(topic.Author.DisplayName, "thread-author"));
            footer.Add(Label(Time(topic.LastActivityUtc), "thread-time"));
            footer.Add(Label(topic.ReplyCount + " 回复", "thread-replies"));
            card.Add(footer);
            return card;
        }

        private async Task OpenThreadAsync(string threadId)
        {
            SetStatus("正在从 NodeBB 读取主题与回复…");
            var result = await forum.GetThreadAsync(threadId);
            if (!result.Succeeded) { ShowError(result.Message); return; }
            var thread = result.Value;
            selectedThreadId = thread.Id;
            detailContent.Clear();
            detailContent.Add(Label(thread.Board.Name + " / 社区主题", "detail-kicker"));
            detailContent.Add(Label(thread.Title, "detail-title"));
            detailContent.Add(PostBlock(thread.OriginalPost.Author, thread.OriginalPost.Body, thread.OriginalPost.PublishedAtUtc, "主楼"));
            detailContent.Add(Label("回复 · " + thread.ReplyCount, "reply-heading"));
            foreach (var reply in thread.Replies) detailContent.Add(PostBlock(reply.Author, reply.Body, reply.PublishedAtUtc, "#" + reply.Floor));
            replyBody.value = string.Empty;
            SetStatus("主题与回复来自 NodeBB · 刷新可看到浏览器端的新回复。");
            ShowOnly(detail);
        }

        private async Task LoginAsync()
        {
            SetStatus("正在登录 NodeBB 社区账号…");
            var secret = password.value;
            password.value = string.Empty;
            var result = await forum.LoginAsync(username.value, secret);
            if (!result.Succeeded) { ShowError(result.Message); return; }
            authExpanded = false;
            UpdateAuth();
            SetStatus("已登录社区账号：" + result.Value.DisplayName);
            await LoadBoardsAsync();
        }

        private async Task RegisterAsync()
        {
            SetStatus("正在注册 NodeBB 社区账号…");
            var secret = password.value;
            password.value = string.Empty;
            var result = await forum.RegisterAsync(username.value, email.value, secret, consent.value);
            if (!result.Succeeded) { ShowError(result.Message); return; }
            authExpanded = false;
            UpdateAuth();
            SetStatus("社区账号注册成功：" + result.Value.DisplayName);
            await LoadBoardsAsync();
        }

        private async Task LogoutAsync()
        {
            var result = await forum.LogoutAsync();
            if (!result.Succeeded) { ShowError(result.Message); return; }
            UpdateAuth();
            SetStatus("已登出 NodeBB 社区账号；本地玩家档案不受影响。");
            await LoadBoardsAsync();
        }

        private void ShowCompose()
        {
            if (forum.CurrentUser == null) { ShowError("发布社区主题前请先登录 NodeBB 账号。"); return; }
            if (boards.Count == 0) { ShowError("当前没有可发布的社区板块。"); return; }
            composeTitle.value = string.Empty; composeBody.value = string.Empty;
            ShowOnly(compose); composeTitle.Focus();
        }

        private async Task PublishTopicAsync()
        {
            boardIdsByName.TryGetValue(composeBoard.value, out var boardId);
            SetStatus("正在将主题发布到 NodeBB…");
            var result = await forum.CreateThreadAsync(new ForumThreadDraft(boardId, composeTitle.value, composeBody.value));
            if (!result.Succeeded) { ShowError(result.Message); return; }
            SetStatus("社区主题已发布到 NodeBB。");
            ForumBoard board = null;
            foreach (var item in boards) if (item.Id == boardId) { board = item; break; }
            if (board != null) await LoadTopicsAsync(board);
            await OpenThreadAsync(result.Value);
            SetStatus("社区主题已发布到 NodeBB，浏览器端也可看到。");
        }

        private async Task PublishReplyAsync()
        {
            if (string.IsNullOrEmpty(selectedThreadId)) return;
            SetStatus("正在将回复发布到 NodeBB…");
            var result = await forum.CreateReplyAsync(new ForumReplyDraft(selectedThreadId, replyBody.value));
            if (!result.Succeeded) { ShowError(result.Message); return; }
            await OpenThreadAsync(selectedThreadId);
            SetStatus("社区回复已发布到 NodeBB，浏览器端也可看到。");
        }

        private void UpdateAuth()
        {
            var user = forum.CurrentUser;
            accountName.text = user == null ? "访客" : user.DisplayName;
            authCard.style.display = user == null && authExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            authForm.style.display = user == null && authExpanded ? DisplayStyle.Flex : DisplayStyle.None;
            authToggleButton.style.display = user == null ? DisplayStyle.Flex : DisplayStyle.None;
            authToggleButton.text = authExpanded ? "收起表单" : "登录 / 注册";
            logoutButton.style.display = user == null ? DisplayStyle.None : DisplayStyle.Flex;
            newTopicButton.SetEnabled(user != null);
            publishReplyButton.SetEnabled(user != null);
        }

        private void ShowHome() => ShowOnly(home);
        private void ShowOnly(VisualElement visible)
        {
            home.style.display = visible == home ? DisplayStyle.Flex : DisplayStyle.None;
            compose.style.display = visible == compose ? DisplayStyle.Flex : DisplayStyle.None;
            detail.style.display = visible == detail ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetStatus(string message) { status.text = message; status.RemoveFromClassList("community-error"); }
        private void ShowError(string message) { status.text = message; status.AddToClassList("community-error"); }
        private async void Run(Func<Task> action)
        {
            try { await action(); }
            catch (Exception) { ShowError("论坛请求未能完成，请刷新或返回离线档案。"); }
        }

        private static VisualElement PostBlock(ForumUser author, string body, DateTimeOffset timestamp, string floor)
        {
            var block = new VisualElement(); block.AddToClassList("post-block");
            var meta = new VisualElement(); meta.AddToClassList("post-meta");
            meta.Add(Label(author.DisplayName, "post-author"));
            meta.Add(Label(Time(timestamp), "post-time"));
            meta.Add(Label(floor, "post-floor"));
            block.Add(meta); block.Add(Label(body, "post-body"));
            return block;
        }

        private static Label Label(string text, string className)
        {
            var label = new Label(text ?? string.Empty) { enableRichText = false };
            label.AddToClassList(className);
            return label;
        }
        private static string Excerpt(string body, int maximum) => string.IsNullOrEmpty(body) ? "（无摘要）" : body.Length <= maximum ? body : body.Substring(0, maximum) + "…";
        private static string Time(DateTimeOffset value) => value.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}
