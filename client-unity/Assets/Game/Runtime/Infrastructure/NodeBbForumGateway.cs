using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EchoForum.Application;
using EchoForum.Domain;
using UnityEngine;

namespace EchoForum.Infrastructure
{
    /// <summary>Session-cookie adapter for the official NodeBB v4 API. Credentials and cookies remain in memory.</summary>
    public sealed class NodeBbForumGateway : ICommunityForumGateway, IDisposable
    {
        private readonly Uri baseUri;
        private readonly Dictionary<string, ForumBoard> boards = new Dictionary<string, ForumBoard>();
        private HttpClient client;
        private string csrfToken;

        public NodeBbForumGateway(string baseUrl)
        {
            baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
            if (!baseUri.IsLoopback || baseUri.Scheme != Uri.UriSchemeHttp)
                throw new ArgumentException("NodeBB local development must use an HTTP loopback address.", nameof(baseUrl));
            ResetClient();
        }

        public ForumUser CurrentUser { get; private set; }

        public async Task<ForumGatewayResult<IReadOnlyList<ForumBoard>>> GetBoardsAsync()
        {
            var payload = await SendAsync(HttpMethod.Get, "api/v3/categories");
            if (!payload.Ok) return Fail<IReadOnlyList<ForumBoard>>(payload);
            var envelope = Parse<CategoryEnvelope>(payload.Body);
            if (envelope?.response?.categories == null) return Invalid<IReadOnlyList<ForumBoard>>();
            var result = new List<ForumBoard>();
            boards.Clear();
            foreach (var item in envelope.response.categories)
            {
                if (item == null || item.cid <= 0) continue;
                var board = Board(item);
                result.Add(board);
                boards[board.Id] = board;
            }
            return ForumGatewayResult<IReadOnlyList<ForumBoard>>.Ok(result);
        }

        public async Task<ForumGatewayResult<IReadOnlyList<CommunityTopicSummary>>> GetTopicsAsync(string boardId)
        {
            if (!ValidId(boardId)) return ForumGatewayResult<IReadOnlyList<CommunityTopicSummary>>.Fail(ForumGatewayError.Rejected, "板块 ID 无效。");
            var payload = await SendAsync(HttpMethod.Get, "api/v3/categories/" + boardId + "/topics");
            if (!payload.Ok) return Fail<IReadOnlyList<CommunityTopicSummary>>(payload);
            var envelope = Parse<TopicListEnvelope>(payload.Body);
            if (envelope?.response?.topics == null) return Invalid<IReadOnlyList<CommunityTopicSummary>>();
            var result = new List<CommunityTopicSummary>();
            foreach (var item in envelope.response.topics)
            {
                if (item == null || item.tid <= 0) continue;
                var board = FindBoard(item.cid, item.category);
                var author = User(item.user, item.uid);
                result.Add(new CommunityTopicSummary(item.tid.ToString(), board, Clean(item.title), author,
                    Clean(item.teaser == null ? string.Empty : item.teaser.content), Time(item.timestamp),
                    Time(item.lastposttime), Math.Max(0, item.postcount - 1), item.pinned != 0));
            }
            return ForumGatewayResult<IReadOnlyList<CommunityTopicSummary>>.Ok(result);
        }

        public async Task<ForumGatewayResult<ForumThread>> GetThreadAsync(string threadId)
        {
            if (!ValidId(threadId)) return ForumGatewayResult<ForumThread>.Fail(ForumGatewayError.Rejected, "主题 ID 无效。");
            var payload = await SendAsync(HttpMethod.Get, "api/topic/" + threadId);
            if (!payload.Ok) return Fail<ForumThread>(payload);
            var detail = Parse<TopicDetail>(payload.Body);
            if (detail == null || detail.tid <= 0 || detail.posts == null || detail.posts.Length == 0) return Invalid<ForumThread>();
            var allPosts = new List<PostDto>(detail.posts);
            for (var page = 2; allPosts.Count < detail.postcount && page <= 20; page++)
            {
                var next = await SendAsync(HttpMethod.Get, "api/topic/" + threadId + "?page=" + page);
                if (!next.Ok) break;
                var nextDetail = Parse<TopicDetail>(next.Body);
                if (nextDetail?.posts == null || nextDetail.posts.Length == 0) break;
                var added = 0;
                foreach (var post in nextDetail.posts)
                {
                    if (post == null) continue;
                    var duplicate = false;
                    foreach (var existing in allPosts) if (existing.pid == post.pid) { duplicate = true; break; }
                    if (!duplicate) { allPosts.Add(post); added++; }
                }
                if (added == 0) break;
            }
            allPosts.Sort((a, b) => a.index.CompareTo(b.index));
            var first = allPosts[0];
            var replies = new List<ForumReply>();
            for (var index = 1; index < allPosts.Count; index++)
            {
                var post = allPosts[index];
                replies.Add(new ForumReply(post.pid.ToString(), User(post.user, post.uid), Clean(post.content), Time(post.timestamp), post.index + 1));
            }
            var original = new ForumPost(first.pid.ToString(), User(first.user, first.uid), Clean(first.content), Time(first.timestamp));
            var board = FindBoard(detail.cid, detail.category);
            var tags = new List<string>();
            if (detail.tags != null) foreach (var tag in detail.tags) if (tag != null && !string.IsNullOrWhiteSpace(tag.value)) tags.Add(Clean(tag.value));
            return ForumGatewayResult<ForumThread>.Ok(new ForumThread(threadId, board, Clean(detail.title), original,
                replies, tags, detail.pinned != 0, false, Clean(first.content), Math.Max(0, detail.postcount - 1), Time(detail.lastposttime)));
        }

        public async Task<ForumGatewayResult<ForumUser>> RegisterAsync(string username, string email, string password, bool consent)
        {
            if (!consent) return ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Rejected, "请先确认本机论坛的注册信息处理说明。");
            var config = await RefreshConfigAsync();
            if (!config.Succeeded) return ForumGatewayResult<ForumUser>.Fail(config.Error, config.Message);
            var form = Form(new Dictionary<string, string> {
                { "username", username }, { "email", email }, { "password", password }, { "password-confirm", password }
            });
            var response = await SendAsync(HttpMethod.Post, "register", form, true);
            if (!response.Ok) return Fail<ForumUser>(response);
            var registration = Parse<RegistrationReply>(response.Body);
            if (registration != null && registration.next != null && registration.next.Contains("/register/complete"))
            {
                var intermediate = await RefreshConfigAsync();
                if (!intermediate.Succeeded) return ForumGatewayResult<ForumUser>.Fail(intermediate.Error, intermediate.Message);
                var agreement = Form(new Dictionary<string, string> { { "gdpr_agree_data", "on" }, { "gdpr_agree_email", "on" } });
                var completion = await SendAsync(HttpMethod.Post, "register/complete", agreement, true);
                if (!completion.Ok) return Fail<ForumUser>(completion);
            }
            var verified = await RefreshConfigAsync();
            if (!verified.Succeeded) return ForumGatewayResult<ForumUser>.Fail(verified.Error, verified.Message);
            if (!verified.Value.loggedIn || verified.Value.uid <= 0)
                return ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Rejected, "NodeBB 尚未完成注册，请在本机论坛检查账号状态。");
            CurrentUser = new ForumUser(verified.Value.uid.ToString(), username, "社区账号");
            return ForumGatewayResult<ForumUser>.Ok(CurrentUser);
        }

        public async Task<ForumGatewayResult<ForumUser>> LoginAsync(string username, string password)
        {
            var config = await RefreshConfigAsync();
            if (!config.Succeeded) return ForumGatewayResult<ForumUser>.Fail(config.Error, config.Message);
            var response = await SendAsync(HttpMethod.Post, "login", Form(new Dictionary<string, string> {
                { "username", username }, { "password", password }
            }), true);
            if (!response.Ok) return Fail<ForumUser>(response);
            var verified = await RefreshConfigAsync();
            if (!verified.Succeeded) return ForumGatewayResult<ForumUser>.Fail(verified.Error, verified.Message);
            if (!verified.Value.loggedIn || verified.Value.uid <= 0)
                return ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Unauthorized, "NodeBB 登录失败，请检查用户名和密码。");
            CurrentUser = new ForumUser(verified.Value.uid.ToString(), username, "社区账号");
            return ForumGatewayResult<ForumUser>.Ok(CurrentUser);
        }

        public async Task<ForumGatewayResult<bool>> LogoutAsync()
        {
            if (CurrentUser == null) return ForumGatewayResult<bool>.Ok(true);
            var response = await SendAsync(HttpMethod.Post, "logout", Form(new Dictionary<string, string>()), true);
            if (!response.Ok) return Fail<bool>(response);
            ResetClient();
            return ForumGatewayResult<bool>.Ok(true);
        }

        public async Task<ForumGatewayResult<string>> CreateThreadAsync(ForumThreadDraft draft)
        {
            if (!ValidId(draft.BoardId)) return ForumGatewayResult<string>.Fail(ForumGatewayError.Rejected, "板块 ID 无效。");
            var body = JsonUtility.ToJson(new CreateTopicBody { cid = int.Parse(draft.BoardId), title = draft.Title.Trim(), content = draft.Body.Trim() });
            var response = await SendAsync(HttpMethod.Post, "api/v3/topics", Json(body), true);
            if (!response.Ok) return Fail<string>(response);
            var envelope = Parse<CreateTopicEnvelope>(response.Body);
            if (envelope?.response == null || envelope.response.tid <= 0)
                return ForumGatewayResult<string>.Fail(ForumGatewayError.Rejected, "NodeBB 已接收主题，但尚未发布；请检查社区审核状态。");
            return ForumGatewayResult<string>.Ok(envelope.response.tid.ToString());
        }

        public async Task<ForumGatewayResult<bool>> CreateReplyAsync(ForumReplyDraft draft)
        {
            if (!ValidId(draft.ThreadId)) return ForumGatewayResult<bool>.Fail(ForumGatewayError.Rejected, "主题 ID 无效。");
            var body = JsonUtility.ToJson(new CreateReplyBody { content = draft.Body.Trim() });
            var response = await SendAsync(HttpMethod.Post, "api/v3/topics/" + draft.ThreadId, Json(body), true);
            if (!response.Ok) return Fail<bool>(response);
            var envelope = Parse<CreateReplyEnvelope>(response.Body);
            return envelope?.response != null && envelope.response.pid > 0
                ? ForumGatewayResult<bool>.Ok(true)
                : ForumGatewayResult<bool>.Fail(ForumGatewayError.Rejected, "NodeBB 尚未发布这条回复，请检查社区审核状态。");
        }

        private async Task<ForumGatewayResult<ConfigDto>> RefreshConfigAsync()
        {
            var response = await SendAsync(HttpMethod.Get, "api/config");
            if (!response.Ok) return Fail<ConfigDto>(response);
            var config = Parse<ConfigDto>(response.Body);
            if (config == null || string.IsNullOrWhiteSpace(config.csrf_token)) return Invalid<ConfigDto>();
            csrfToken = config.csrf_token;
            return ForumGatewayResult<ConfigDto>.Ok(config);
        }

        private async Task<HttpPayload> SendAsync(HttpMethod method, string path, HttpContent content = null, bool requiresCsrf = false)
        {
            if (requiresCsrf && string.IsNullOrWhiteSpace(csrfToken))
            {
                var config = await RefreshConfigAsync();
                if (!config.Succeeded) return new HttpPayload { Error = config.Error, Message = config.Message };
            }
            try
            {
                using (var request = new HttpRequestMessage(method, new Uri(baseUri, path)))
                {
                    request.Content = content;
                    if (requiresCsrf) request.Headers.TryAddWithoutValidation("x-csrf-token", csrfToken);
                    using (var response = await client.SendAsync(request))
                    {
                        var body = await response.Content.ReadAsStringAsync();
                        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Found)
                            return ErrorResponse((int)response.StatusCode, body);
                        return new HttpPayload { Body = body, Error = ForumGatewayError.None };
                    }
                }
            }
            catch (Exception exception) when (exception is HttpRequestException || exception is TaskCanceledException)
            {
                return new HttpPayload { Error = ForumGatewayError.Unavailable, Message = "本机论坛服务不可用，请检查 Docker Desktop 与 NodeBB。可切回离线档案。" };
            }
        }

        private static HttpPayload ErrorResponse(int status, string body)
        {
            var serverMessage = Parse<ErrorEnvelope>(body)?.status?.message;
            var error = status == 401 ? ForumGatewayError.Unauthorized : status == 403 ? ForumGatewayError.Forbidden : ForumGatewayError.Rejected;
            if (!string.IsNullOrEmpty(body))
            {
                if (body.Contains("username-too-long")) return new HttpPayload { Error = error, Message = "用户名过长，请换一个较短的用户名。" };
                if (body.Contains("username-taken")) return new HttpPayload { Error = error, Message = "用户名已被使用。" };
                if (body.Contains("email-taken")) return new HttpPayload { Error = error, Message = "邮箱已被使用。" };
                if (body.Contains("password-too-short")) return new HttpPayload { Error = error, Message = "密码未达到 NodeBB 的长度要求。" };
            }
            var message = status == 401 ? "请先登录 NodeBB 社区账号。" : status == 403 ? "NodeBB 拒绝了请求，请检查账号权限或登录状态。" :
                status == 429 ? "操作过于频繁，请稍后再试。" : "NodeBB 请求失败（HTTP " + status + "）" + (string.IsNullOrWhiteSpace(serverMessage) ? "。" : "：" + Clean(serverMessage));
            return new HttpPayload { Error = error, Message = message };
        }

        private ForumBoard FindBoard(int cid, CategoryDto category)
        {
            var id = cid.ToString();
            if (boards.TryGetValue(id, out var board)) return board;
            board = category == null ? new ForumBoard(id, id, "板块 " + id, string.Empty) : Board(category);
            boards[id] = board;
            return board;
        }

        private static ForumBoard Board(CategoryDto data) => new ForumBoard(data.cid.ToString(), Clean(data.slug), Clean(data.name), Clean(data.description));
        private static ForumUser User(UserDto data, int fallbackId) => new ForumUser((data == null ? fallbackId : data.uid).ToString(),
            Clean(data == null ? "社区用户" : string.IsNullOrWhiteSpace(data.displayname) ? data.username : data.displayname), "NodeBB 用户");
        private static DateTimeOffset Time(long milliseconds) => milliseconds > 0 && milliseconds < 253402300799000L ? DateTimeOffset.FromUnixTimeMilliseconds(milliseconds) : DateTimeOffset.UnixEpoch;
        private static bool ValidId(string value) => int.TryParse(value, out var number) && number > 0;
        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var withoutMarkup = Regex.Replace(value, "<[^>]*>", " ");
            return WebUtility.HtmlDecode(withoutMarkup).Trim();
        }
        private static T Parse<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try { return JsonUtility.FromJson<T>(json); }
            catch (ArgumentException) { return null; }
        }
        private static HttpContent Json(string body) => new StringContent(body, Encoding.UTF8, "application/json");
        private static HttpContent Form(Dictionary<string, string> fields) => new FormUrlEncodedContent(fields);
        private static ForumGatewayResult<T> Fail<T>(HttpPayload payload) => ForumGatewayResult<T>.Fail(payload.Error, payload.Message);
        private static ForumGatewayResult<T> Invalid<T>() => ForumGatewayResult<T>.Fail(ForumGatewayError.InvalidResponse, "NodeBB 返回了无法识别的数据。请刷新或切回离线档案。");

        private void ResetClient()
        {
            client?.Dispose();
            var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new CookieContainer(), UseProxy = false, AllowAutoRedirect = false };
            client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(12) };
            csrfToken = null;
            CurrentUser = null;
        }

        public void Dispose() => client?.Dispose();

        private sealed class HttpPayload { public string Body; public ForumGatewayError Error; public string Message; public bool Ok => Error == ForumGatewayError.None; }
        [Serializable] private sealed class ApiStatus { public string code; public string message; }
        [Serializable] private sealed class ErrorEnvelope { public ApiStatus status; }
        [Serializable] private sealed class ConfigDto { public bool loggedIn; public int uid; public string csrf_token; }
        [Serializable] private sealed class RegistrationReply { public string next; }
        [Serializable] private sealed class CategoryDto { public int cid; public string slug; public string name; public string description; }
        [Serializable] private sealed class UserDto { public int uid; public string username; public string displayname; }
        [Serializable] private sealed class TeaserDto { public string content; }
        [Serializable] private sealed class TopicDto { public int tid; public int uid; public int cid; public string title; public long timestamp; public long lastposttime; public int postcount; public int pinned; public UserDto user; public CategoryDto category; public TeaserDto teaser; }
        [Serializable] private sealed class CategoryListData { public CategoryDto[] categories; }
        [Serializable] private sealed class CategoryEnvelope { public ApiStatus status; public CategoryListData response; }
        [Serializable] private sealed class TopicListData { public TopicDto[] topics; }
        [Serializable] private sealed class TopicListEnvelope { public ApiStatus status; public TopicListData response; }
        [Serializable] private sealed class PostDto { public int pid; public int uid; public int index; public long timestamp; public string content; public UserDto user; }
        [Serializable] private sealed class TagDto { public string value; }
        [Serializable] private sealed class TopicDetail { public int tid; public int cid; public string title; public int postcount; public int pinned; public long lastposttime; public PostDto[] posts; public TagDto[] tags; public CategoryDto category; }
        [Serializable] private sealed class CreateTopicBody { public int cid; public string title; public string content; }
        [Serializable] private sealed class CreateReplyBody { public string content; }
        [Serializable] private sealed class CreatedTopic { public int tid; }
        [Serializable] private sealed class CreateTopicEnvelope { public ApiStatus status; public CreatedTopic response; }
        [Serializable] private sealed class CreatedReply { public int pid; }
        [Serializable] private sealed class CreateReplyEnvelope { public ApiStatus status; public CreatedReply response; }
    }
}
