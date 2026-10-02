using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EchoForum.Domain;

namespace EchoForum.Application
{
    public enum ForumGatewayError { None, Unavailable, Unauthorized, Forbidden, Rejected, InvalidResponse }

    public sealed class ForumGatewayResult<T>
    {
        private ForumGatewayResult(T value, ForumGatewayError error, string message)
        {
            Value = value;
            Error = error;
            Message = message;
        }

        public T Value { get; }
        public ForumGatewayError Error { get; }
        public string Message { get; }
        public bool Succeeded => Error == ForumGatewayError.None;
        public static ForumGatewayResult<T> Ok(T value) => new ForumGatewayResult<T>(value, ForumGatewayError.None, null);
        public static ForumGatewayResult<T> Fail(ForumGatewayError error, string message) => new ForumGatewayResult<T>(default, error, message);
    }

    /// <summary>Community data is owned by the remote forum. No player save data enters this boundary.</summary>
    public interface ICommunityForumGateway
    {
        ForumUser CurrentUser { get; }
        Task<ForumGatewayResult<IReadOnlyList<ForumBoard>>> GetBoardsAsync();
        Task<ForumGatewayResult<IReadOnlyList<CommunityTopicSummary>>> GetTopicsAsync(string boardId);
        Task<ForumGatewayResult<ForumThread>> GetThreadAsync(string threadId);
        Task<ForumGatewayResult<ForumUser>> RegisterAsync(string username, string email, string password, bool consent);
        Task<ForumGatewayResult<ForumUser>> LoginAsync(string username, string password);
        Task<ForumGatewayResult<bool>> LogoutAsync();
        Task<ForumGatewayResult<string>> CreateThreadAsync(ForumThreadDraft draft);
        Task<ForumGatewayResult<bool>> CreateReplyAsync(ForumReplyDraft draft);
    }

    public sealed class CommunityForumUseCases
    {
        private readonly ICommunityForumGateway gateway;
        public CommunityForumUseCases(ICommunityForumGateway gateway) { this.gateway = gateway; }
        public ForumUser CurrentUser => gateway.CurrentUser;
        public Task<ForumGatewayResult<IReadOnlyList<ForumBoard>>> GetBoardsAsync() => gateway.GetBoardsAsync();
        public Task<ForumGatewayResult<IReadOnlyList<CommunityTopicSummary>>> GetTopicsAsync(string boardId) => gateway.GetTopicsAsync(boardId);
        public Task<ForumGatewayResult<ForumThread>> GetThreadAsync(string threadId) => gateway.GetThreadAsync(threadId);

        public Task<ForumGatewayResult<ForumUser>> RegisterAsync(string username, string email, string password, bool consent)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Rejected, "注册需要用户名、邮箱和密码。"));
            if (!consent) return Task.FromResult(ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Rejected, "请先同意本机测试论坛处理注册信息。"));
            return gateway.RegisterAsync(username.Trim(), email.Trim(), password, consent);
        }

        public Task<ForumGatewayResult<ForumUser>> LoginAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return InvalidUser();
            return gateway.LoginAsync(username.Trim(), password);
        }

        public Task<ForumGatewayResult<bool>> LogoutAsync() => gateway.LogoutAsync();

        public Task<ForumGatewayResult<string>> CreateThreadAsync(ForumThreadDraft draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.BoardId) || string.IsNullOrWhiteSpace(draft.Title) || string.IsNullOrWhiteSpace(draft.Body))
                return Task.FromResult(ForumGatewayResult<string>.Fail(ForumGatewayError.Rejected, "请选择板块并填写标题和正文。"));
            if (CurrentUser == null) return Task.FromResult(ForumGatewayResult<string>.Fail(ForumGatewayError.Unauthorized, "请先登录 NodeBB 社区账号。"));
            return gateway.CreateThreadAsync(draft);
        }

        public Task<ForumGatewayResult<bool>> CreateReplyAsync(ForumReplyDraft draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.ThreadId) || string.IsNullOrWhiteSpace(draft.Body))
                return Task.FromResult(ForumGatewayResult<bool>.Fail(ForumGatewayError.Rejected, "回复内容不能为空。"));
            if (CurrentUser == null) return Task.FromResult(ForumGatewayResult<bool>.Fail(ForumGatewayError.Unauthorized, "请先登录 NodeBB 社区账号。"));
            return gateway.CreateReplyAsync(draft);
        }

        private static Task<ForumGatewayResult<ForumUser>> InvalidUser() => Task.FromResult(ForumGatewayResult<ForumUser>.Fail(ForumGatewayError.Rejected, "请输入用户名和密码。"));
    }
}
