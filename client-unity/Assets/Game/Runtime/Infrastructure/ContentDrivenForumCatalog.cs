using System;
using System.Collections.Generic;
using System.Globalization;
using EchoForum.Application;
using EchoForum.Content;
using EchoForum.Domain;
using UnityEngine;

namespace EchoForum.Infrastructure
{
    /// <summary>Reads published ScriptableObject content and exposes only pure Domain/Application data.</summary>
    public sealed class ContentDrivenForumCatalog : IForumCatalog, ICaseContentCatalog
    {
        private readonly ForumHomeSnapshot home;
        private readonly Dictionary<string, ForumThread> threads = new Dictionary<string, ForumThread>();
        private readonly Dictionary<string, CaseDefinition> cases = new Dictionary<string, CaseDefinition>();
        private readonly Dictionary<string, ThreadCaseBinding> bindings = new Dictionary<string, ThreadCaseBinding>();

        public ContentDrivenForumCatalog()
        {
            var boards = BuildBoards(Resources.LoadAll<ForumBoardDefinition>("Forum"));
            var users = BuildUsers(Resources.LoadAll<ForumUserDefinition>("Forum"));
            var forumThreads = BuildThreads(Resources.LoadAll<ForumThreadDefinition>("Forum"), boards, users);
            BuildCases(Resources.LoadAll<CaseDefinitionAsset>("Cases"));
            BuildBindings(Resources.LoadAll<ThreadCaseBindingAsset>("Bindings"));
            home = new ForumHomeSnapshot(new List<ForumBoard>(boards.Values), forumThreads, new List<ForumUser>(users.Values));
        }

        public ForumHomeSnapshot LoadHome() => home;
        public ForumThread FindThread(string threadId) => threadId != null && threads.TryGetValue(threadId, out var thread) ? thread : null;
        public CaseDefinition FindCase(string caseId) => caseId != null && cases.TryGetValue(caseId, out var definition) ? definition : null;
        public ThreadCaseBinding FindBinding(string threadId) => threadId != null && bindings.TryGetValue(threadId, out var binding) ? binding : null;

        private static Dictionary<string, ForumBoard> BuildBoards(ForumBoardDefinition[] definitions)
        {
            var result = new Dictionary<string, ForumBoard>();
            foreach (var definition in definitions)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.BoardId)) result[definition.BoardId] = new ForumBoard(definition.BoardId, definition.Slug, definition.DisplayName, definition.Description);
            }
            return result;
        }

        private static Dictionary<string, ForumUser> BuildUsers(ForumUserDefinition[] definitions)
        {
            var result = new Dictionary<string, ForumUser>();
            foreach (var definition in definitions)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.UserId)) result[definition.UserId] = new ForumUser(definition.UserId, definition.DisplayName, definition.PresenceLabel, definition.IsOfficial);
            }
            return result;
        }

        private List<ForumThread> BuildThreads(ForumThreadDefinition[] definitions, Dictionary<string, ForumBoard> boards, Dictionary<string, ForumUser> users)
        {
            var result = new List<ForumThread>();
            foreach (var definition in definitions)
            {
                if (definition == null || string.IsNullOrEmpty(definition.ThreadId) || !boards.TryGetValue(definition.BoardId, out var board) || !users.TryGetValue(definition.AuthorId, out var author)) continue;
                var replies = new List<ForumReply>();
                foreach (var reply in definition.InitialReplies)
                {
                    if (reply != null && users.TryGetValue(reply.AuthorId, out var replyAuthor)) replies.Add(new ForumReply(reply.ReplyId, replyAuthor, reply.Body, ParseUtc(reply.PublishedAtUtc), reply.Floor));
                }
                var thread = new ForumThread(definition.ThreadId, board, definition.Title, new ForumPost("post-" + definition.ThreadId, author, definition.Body, ParseUtc(definition.PublishedAtUtc)), replies, definition.Tags, definition.IsPinned, definition.IsOfficial, definition.Summary);
                threads[thread.Id] = thread;
                result.Add(thread);
            }
            result.Sort((left, right) => right.LastActivityUtc.CompareTo(left.LastActivityUtc));
            return result;
        }

        private void BuildCases(CaseDefinitionAsset[] definitions)
        {
            foreach (var definition in definitions)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.CaseId)) cases[definition.CaseId] = new CaseDefinition(definition.CaseId, definition.DisplayName, definition.InvestigationSceneName, definition.IsAvailableInCurrentBuild, definition.RuleSummary, definition.StartConditionId, definition.CompletionStatusText, definition.CompletionReplyText);
            }
        }

        private void BuildBindings(ThreadCaseBindingAsset[] definitions)
        {
            foreach (var definition in definitions)
            {
                if (definition != null && !string.IsNullOrEmpty(definition.ThreadId)) bindings[definition.ThreadId] = new ThreadCaseBinding(definition.ThreadId, definition.CaseId, definition.AllowsEntry, definition.NotStartedEntryLabel, definition.InProgressEntryLabel, definition.ClosedEntryLabel, definition.UnavailableEntryLabel, definition.UnlockConditionId);
            }
        }

        private static DateTimeOffset ParseUtc(string value)
        {
            return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var timestamp) ? timestamp : DateTimeOffset.UnixEpoch;
        }
    }
}
