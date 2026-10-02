using System;
using System.Collections.Generic;
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
        private CommunityForumUseCases communityUseCases;
        private CommunityForumPresenter community;
        private ForumCaseUseCases forumCases;
        private IGameFlowCoordinator flow;
        private IGameSession session;
        private string initialThreadId;
        private string currentThreadId;
        private ForumConnectionMode connectionMode = ForumConnectionMode.LocalOffline;
        public ForumConnectionMode ConnectionMode => connectionMode;
        private ForumHomeSnapshot home;
        private VisualElement root;
        private VisualElement boardNavigation;
        private VisualElement threadList;
        private VisualElement onlineUsers;
        private VisualElement hotThreads;
        private VisualElement homeView;
        private VisualElement composeView;
        private VisualElement detailView;
        private VisualElement detailContent;
        private VisualElement communityView;
        private VisualElement leftRail;
        private VisualElement rightRail;
        private Label connectionStatus;
        private Label forumNotice;
        private DropdownField threadBoard;
        private TextField threadTitle;
        private TextField threadBody;
        private Label threadFormMessage;
        private TextField replyBody;
        private Label replyFormMessage;
        private readonly Dictionary<string, string> boardIdsByName = new Dictionary<string, string>();

        public void Initialize(ForumQueries forumQueries, CommunityForumUseCases communityForum, ForumCaseUseCases forumCaseUseCases, IGameFlowCoordinator gameFlow, IGameSession gameSession, string returnThreadId)
        {
            queries = forumQueries; communityUseCases = communityForum; forumCases = forumCaseUseCases; flow = gameFlow; session = gameSession; initialThreadId = returnThreadId;
        }

        private void Start()
        {
            if (queries == null || communityUseCases == null || forumCases == null || flow == null || session == null) { Debug.LogError("ForumPrototypeController requires ForumPrototypeBootstrapper."); return; }
            root = GetComponent<UIDocument>().rootVisualElement;
            root.RegisterCallback<GeometryChangedEvent>(evt => root.EnableInClassList("compact-topbar", evt.newRect.width < 1450));
            boardNavigation = root.Q<VisualElement>("board-navigation"); threadList = root.Q<VisualElement>("thread-list"); onlineUsers = root.Q<VisualElement>("online-users"); hotThreads = root.Q<VisualElement>("hot-threads"); homeView = root.Q<VisualElement>("home-view"); composeView = root.Q<VisualElement>("compose-view"); detailView = root.Q<VisualElement>("detail-view"); detailContent = root.Q<VisualElement>("detail-content"); forumNotice = root.Q<Label>("forum-notice");
            threadBoard = root.Q<DropdownField>("thread-board"); threadTitle = root.Q<TextField>("thread-title"); threadBody = root.Q<TextField>("thread-body"); threadFormMessage = root.Q<Label>("thread-form-message"); replyBody = root.Q<TextField>("reply-body"); replyFormMessage = root.Q<Label>("reply-form-message");
            communityView = root.Q<VisualElement>("community-view"); leftRail = root.Q<VisualElement>("left-rail"); rightRail = root.Q<VisualElement>("right-rail"); connectionStatus = root.Q<Label>("connection-status");
            community = new CommunityForumPresenter(root, communityUseCases, ShowArchive);
            root.Q<Button>("archive-tab-button").clicked += ShowArchive; root.Q<Button>("community-tab-button").clicked += ShowCommunity;
            root.Q<Button>("home-button").clicked += ShowArchive; root.Q<Button>("back-button").clicked += ShowHome; root.Q<Button>("compose-back-button").clicked += ShowHome; root.Q<Button>("new-thread-button").clicked += ShowCompose; root.Q<Button>("main-menu-button").clicked += flow.ReturnToMainMenu; root.Q<Button>("cancel-thread-button").clicked += ShowHome; root.Q<Button>("publish-thread-button").clicked += PublishThread; root.Q<Button>("publish-reply-button").clicked += PublishReply; root.Q<Button>("continue-investigation-button").clicked += ResumeInvestigation;
            RefreshProfile(); BuildHome();
            communityView.style.display = DisplayStyle.None;
            if (string.IsNullOrEmpty(initialThreadId)) ShowHome(); else ShowThread(initialThreadId);
        }

        private void BuildHome()
        {
            home = queries.LoadHome();
            boardNavigation.Clear(); boardIdsByName.Clear();
            var choices = new List<string>();
            foreach (var board in home.Boards)
            {
                var boardButton = new Button { text = board.Name }; boardButton.AddToClassList("board-link"); boardButton.tooltip = board.Description; boardNavigation.Add(boardButton);
                choices.Add(board.Name); boardIdsByName[board.Name] = board.Id;
            }
            threadBoard.choices = choices;
            if (choices.Count > 0 && !choices.Contains(threadBoard.value)) threadBoard.value = choices[0];
            threadList.Clear(); foreach (var thread in home.Threads) threadList.Add(CreateThreadCard(thread));
            onlineUsers.Clear(); foreach (var user in home.OnlineUsers) { var row = new VisualElement(); row.AddToClassList("user-row"); var dot = new VisualElement(); dot.AddToClassList("presence-dot"); row.Add(dot); row.Add(CreateLabel(user.DisplayName, "user-name")); row.Add(CreateLabel(user.PresenceLabel, "user-presence")); onlineUsers.Add(row); }
            hotThreads.Clear(); for (var index = 0; index < Math.Min(3, home.Threads.Count); index++) { var thread = home.Threads[index]; var hot = new Button(() => ShowThread(thread.Id)) { text = thread.Title }; hot.AddToClassList("hot-thread-link"); hotThreads.Add(hot); }
            forumNotice.text = queries.ConsumeForumNotice() ?? string.Empty;
        }

        private void RefreshProfile()
        {
            var summary = queries.GetProfileSummary();
            var playerStatus = root.Q<Label>("player-status"); if (playerStatus != null) playerStatus.text = session.HasProfile ? "档案：" + summary.DisplayName : "档案：访客";
            root.Q<Label>("profile-name").text = summary.DisplayName;
            root.Q<Label>("profile-joined").text = string.IsNullOrEmpty(summary.CreatedAtUtc) ? "加入日期：—" : "加入日期：" + FormatTime(ParseTime(summary.CreatedAtUtc));
            root.Q<Label>("profile-thread-count").text = "发表主题：" + summary.ThreadCount;
            root.Q<Label>("profile-reply-count").text = "发表回复：" + summary.ReplyCount;
            root.Q<Label>("profile-case-count").text = "进行中：" + summary.InProgressCaseCount + " · 已结案：" + summary.ClosedCaseCount;
            var resume = root.Q<Button>("continue-investigation-button"); resume.SetEnabled(summary.HasActiveInvestigation); resume.style.display = summary.HasActiveInvestigation ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement CreateThreadCard(ForumThread thread)
        {
            var card = new Button(() => ShowThread(thread.Id)); card.AddToClassList("thread-card"); if (thread.IsPinned) card.AddToClassList("thread-card-pinned");
            var header = new VisualElement(); header.AddToClassList("thread-card-header"); if (thread.IsOfficial) header.Add(CreateLabel("官方", "official-badge")); if (thread.IsPinned) header.Add(CreateLabel("置顶", "pinned-badge")); header.Add(CreateLabel(thread.Board.Name, "board-badge")); card.Add(header);
            card.Add(CreateLabel(thread.Title, "thread-title")); card.Add(CreateLabel(Excerpt(string.IsNullOrEmpty(thread.Summary) ? thread.OriginalPost.Body : thread.Summary, 62), "thread-excerpt"));
            var footer = new VisualElement(); footer.AddToClassList("thread-card-footer"); footer.Add(CreateLabel(thread.OriginalPost.Author.DisplayName, "thread-author")); footer.Add(CreateLabel(FormatTime(thread.LastActivityUtc), "thread-time")); footer.Add(CreateLabel($"{thread.ReplyCount} 回复", "thread-replies")); card.Add(footer);
            var tagRow = new VisualElement(); tagRow.AddToClassList("tag-row"); foreach (var tag in thread.Tags) tagRow.Add(CreateLabel(tag, "tag")); card.Add(tagRow); return card;
        }

        private void ShowThread(string threadId)
        {
            var thread = queries.FindThread(threadId); if (thread == null) { ShowHome(); return; }
            currentThreadId = threadId; flow.RememberForumThread(threadId);
            var entry = forumCases.GetEntry(thread.Id);
            if (entry.State == ForumCaseEntryState.Closed && !string.IsNullOrEmpty(entry.CompletionReplyText))
            {
                session.EnsureSystemCaseReply(entry.ThreadId, entry.CaseId, entry.CompletionReplyText, entry.CompletedAtUtc);
                thread = queries.FindThread(threadId);
            }
            detailContent.Clear(); detailContent.Add(CreateLabel(thread.Board.Name + " / 主题阅读", "detail-kicker")); detailContent.Add(CreateLabel(thread.Title, "detail-title")); detailContent.Add(CreatePostBlock(thread.OriginalPost.Author, thread.OriginalPost.Body, thread.OriginalPost.PublishedAtUtc, "主楼")); detailContent.Add(CreateLabel($"回复 · {thread.ReplyCount}", "reply-heading"));
            foreach (var reply in thread.Replies) detailContent.Add(CreatePostBlock(reply.Author, reply.Body, reply.PublishedAtUtc, reply.Author.Id == "local-system" ? "系统" : $"#{reply.Floor}"));
            if (entry.HasBinding)
            {
                var action = new VisualElement(); action.AddToClassList("case-action"); action.Add(CreateLabel(entry.StatusText, "case-status")); var enter = new Button(() => flow.EnterCaseFromThread(thread.Id)) { text = entry.Label }; enter.SetEnabled(entry.CanEnter); enter.AddToClassList("case-enter-button"); action.Add(enter); detailContent.Add(action);
            }
            replyBody.value = string.Empty; replyFormMessage.text = string.Empty; ShowOnly(detailView);
        }

        private void ShowCompose()
        {
            threadTitle.value = string.Empty; threadBody.value = string.Empty; threadFormMessage.text = string.Empty; ShowOnly(composeView); threadTitle.Focus();
        }

        private void PublishThread()
        {
            boardIdsByName.TryGetValue(threadBoard.value, out var boardId);
            if (!queries.TryCreateThread(new ForumThreadDraft(boardId, threadTitle.value, threadBody.value), out var thread, out var error)) { threadFormMessage.text = error; return; }
            RefreshProfile(); BuildHome(); ShowThread(thread.Id);
        }

        private void PublishReply()
        {
            if (string.IsNullOrEmpty(currentThreadId)) return;
            if (!queries.TryCreateReply(new ForumReplyDraft(currentThreadId, replyBody.value), out _, out var error)) { replyFormMessage.text = error; return; }
            RefreshProfile(); BuildHome(); ShowThread(currentThreadId);
        }

        private void ResumeInvestigation()
        {
            if (!flow.ResumeInvestigation()) { BuildHome(); ShowHome(); }
        }

        private void ShowHome() { currentThreadId = null; BuildHome(); ShowOnly(homeView); }
        private void ShowArchive()
        {
            connectionMode = ForumConnectionMode.LocalOffline;
            communityView.style.display = DisplayStyle.None;
            leftRail.style.display = DisplayStyle.Flex; rightRail.style.display = DisplayStyle.Flex;
            root.Q<Button>("new-thread-button").style.display = DisplayStyle.Flex;
            root.Q<Button>("archive-tab-button").AddToClassList("mode-selected"); root.Q<Button>("community-tab-button").RemoveFromClassList("mode-selected");
            connectionStatus.text = "本地档案 · 离线";
            ShowHome();
        }
        private void ShowCommunity()
        {
            connectionMode = ForumConnectionMode.NodeBbLocal;
            homeView.style.display = DisplayStyle.None; composeView.style.display = DisplayStyle.None; detailView.style.display = DisplayStyle.None;
            communityView.style.display = DisplayStyle.Flex;
            leftRail.style.display = DisplayStyle.None; rightRail.style.display = DisplayStyle.None;
            root.Q<Button>("new-thread-button").style.display = DisplayStyle.None;
            root.Q<Button>("archive-tab-button").RemoveFromClassList("mode-selected"); root.Q<Button>("community-tab-button").AddToClassList("mode-selected");
            connectionStatus.text = "NodeBB · 本机社区";
            community.Open();
        }
        private void ShowOnly(VisualElement visible) { communityView.style.display = DisplayStyle.None; homeView.style.display = visible == homeView ? DisplayStyle.Flex : DisplayStyle.None; composeView.style.display = visible == composeView ? DisplayStyle.Flex : DisplayStyle.None; detailView.style.display = visible == detailView ? DisplayStyle.Flex : DisplayStyle.None; }
        private VisualElement CreatePostBlock(ForumUser author, string body, DateTimeOffset publishedAt, string floor) { var block = new VisualElement(); block.AddToClassList("post-block"); var meta = new VisualElement(); meta.AddToClassList("post-meta"); meta.Add(CreateLabel(author.DisplayName, "post-author")); if (author.IsOfficial) meta.Add(CreateLabel(author.Id == "local-system" ? "本地系统" : "官方帐号", "official-badge")); meta.Add(CreateLabel(FormatTime(publishedAt), "post-time")); meta.Add(CreateLabel(floor, "post-floor")); block.Add(meta); block.Add(CreateLabel(body, "post-body")); return block; }
        private static Label CreateLabel(string text, string className) { var label = new Label(text) { enableRichText = false }; label.AddToClassList(className); return label; }
        private static string Excerpt(string body, int maximumLength) => body.Length <= maximumLength ? body : body.Substring(0, maximumLength) + "…";
        private static string FormatTime(DateTimeOffset timestamp) => timestamp.ToLocalTime().ToString("MM-dd HH:mm");
        private static DateTimeOffset ParseTime(string value) => DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.UtcNow;
    }
}
