using System;
using System.Linq;
using System.Threading.Tasks;
using EchoForum.Application;
using EchoForum.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace EchoForum.Editor
{
    /// <summary>Opt-in localhost smoke test. Credentials come only from process environment variables.</summary>
    public static class NodeBbIntegrationVerifier
    {
        [MenuItem("Echo Archive/Verify Local NodeBB Gateway")]
        public static async void Verify()
        {
            var username = Environment.GetEnvironmentVariable("ECHO_NODEBB_TEST_USERNAME");
            var password = Environment.GetEnvironmentVariable("ECHO_NODEBB_TEST_PASSWORD");
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                Debug.LogError("NodeBB verification requires ECHO_NODEBB_TEST_USERNAME and ECHO_NODEBB_TEST_PASSWORD environment variables.");
                Finish(1);
                return;
            }

            try
            {
                if (Environment.GetEnvironmentVariable("ECHO_NODEBB_VERIFY_REGISTER") == "1")
                {
                    using (var registrationGateway = new NodeBbForumGateway("http://127.0.0.1:4567"))
                    {
                        var registration = new CommunityForumUseCases(registrationGateway);
                        var newUser = "ut" + DateTimeOffset.UtcNow.ToString("MMddHHmmss");
                        var createdUser = await registration.RegisterAsync(newUser, newUser + "@localhost.test", password, true);
                        Require(createdUser.Succeeded && registration.CurrentUser != null, "Unity registration failed: " + createdUser.Message);
                        Require((await registration.LogoutAsync()).Succeeded, "New community account could not log out.");
                        Debug.Log("NodeBB registration verification passed.");
                    }
                }

                using (var gateway = new NodeBbForumGateway("http://127.0.0.1:4567"))
                {
                    var forum = new CommunityForumUseCases(gateway);
                    var boards = await forum.GetBoardsAsync();
                    Require(boards.Succeeded, "Board query failed: " + boards.Message);
                    var board = boards.Value.FirstOrDefault(item => item.Name == "Test Discussion A");
                    Require(board != null, "The local NodeBB test board is missing.");

                    var topics = await forum.GetTopicsAsync(board.Id);
                    Require(topics.Succeeded && topics.Value.Count > 0, "Topic list was unavailable: " + topics.Message);
                    var existing = await forum.GetThreadAsync(topics.Value[0].Id);
                    Require(existing.Succeeded && existing.Value.OriginalPost != null, "Topic detail was unavailable: " + existing.Message);

                    var login = await forum.LoginAsync(username, password);
                    Require(login.Succeeded && forum.CurrentUser != null, "NodeBB login failed: " + login.Message);
                    var title = "Unity Gateway Test " + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
                    var created = await forum.CreateThreadAsync(new EchoForum.Domain.ForumThreadDraft(board.Id, title, "Neutral Unity integration test content."));
                    Require(created.Succeeded, "NodeBB did not publish the Unity topic: " + created.Message);
                    var published = await forum.GetThreadAsync(created.Value);
                    Require(published.Succeeded && published.Value.Title == title, "Published topic could not be read back from NodeBB.");

                    var reply = await forum.CreateReplyAsync(new EchoForum.Domain.ForumReplyDraft(created.Value, "Neutral Unity integration test reply."));
                    Require(reply.Succeeded, "NodeBB did not publish the Unity reply: " + reply.Message);
                    var updated = await forum.GetThreadAsync(created.Value);
                    Require(updated.Succeeded && updated.Value.ReplyCount >= 1 && updated.Value.Replies.Count >= 1, "Published reply could not be read back from NodeBB.");
                    Require((await forum.LogoutAsync()).Succeeded && forum.CurrentUser == null, "NodeBB logout did not clear the runtime session.");
                    Debug.Log("NodeBB gateway verification passed: boards, topics, detail, login, topic publication, reply publication, readback, and logout. Published topic ID: " + created.Value);
                }

                using (var unavailable = new NodeBbForumGateway("http://127.0.0.1:4568"))
                {
                    var result = await unavailable.GetBoardsAsync();
                    Require(!result.Succeeded && result.Error == ForumGatewayError.Unavailable, "An unavailable local NodeBB service was not reported cleanly.");
                    Debug.Log("NodeBB unavailable-service verification passed.");
                }
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        public static async void VerifyCrossClientRead()
        {
            var topicId = Environment.GetEnvironmentVariable("ECHO_NODEBB_VERIFY_TOPIC_ID");
            var expectedReply = Environment.GetEnvironmentVariable("ECHO_NODEBB_EXPECT_REPLY_TEXT");
            try
            {
                Require(!string.IsNullOrWhiteSpace(topicId) && !string.IsNullOrWhiteSpace(expectedReply), "Missing cross-client test topic or expected reply.");
                using (var gateway = new NodeBbForumGateway("http://127.0.0.1:4567"))
                {
                    var thread = await new CommunityForumUseCases(gateway).GetThreadAsync(topicId);
                    Require(thread.Succeeded, "Unity could not refresh the NodeBB topic: " + thread.Message);
                    Require(thread.Value.Replies.Any(reply => reply.Body.Contains(expectedReply)), "An external client's reply did not appear in Unity's refreshed topic detail.");
                    Debug.Log("NodeBB cross-client refresh verification passed for topic ID: " + topicId);
                }
                Finish(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Finish(1);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish(int exitCode)
        {
            if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(exitCode);
        }
    }
}
