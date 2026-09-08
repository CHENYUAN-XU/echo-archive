using System;
using System.IO;
using EchoForum.Application;
using EchoForum.Domain;
using EchoForum.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace EchoForum.Editor
{
    public static class MvpGameFlowVerifier
    {
        [MenuItem("Echo Archive/Verify MVP Game Flow")]
        public static void Verify()
        {
            VerifyBuildOrder();
            VerifyProfileForumResumeAndPersistence();
            VerifyLegacyMigration();
            VerifyCorruptSave();
            Debug.Log("MVP game-flow verification passed: profile, local forum persistence, configured investigation resume, settlement writeback, legacy migration, and corrupt-save recovery.");
        }

        private static void VerifyProfileForumResumeAndPersistence()
        {
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-mvp-flow-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new JsonGameSaveRepository(directory);
                var session = new GameSession(repository);
                var content = new ContentDrivenForumCatalog();
                var cases = new CaseUseCases(session);
                var forum = new ForumQueries(content, session);
                var navigator = new RecordingNavigator();
                var flow = new GameFlowCoordinator(session, new ForumCaseUseCases(content, cases), navigator);
                Require(!session.HasProfile && !flow.ContinueGame(), "A first launch unexpectedly continued a profile.");
                Require(!flow.CreateProfileAndEnterForum("  ", out _), "An empty display name created a profile.");
                Require(flow.CreateProfileAndEnterForum("测试档案", out _), "A valid local profile could not be created.");
                Require(navigator.LastScene == "ForumPrototype" && session.PlayerProfile.DisplayName == "测试档案", "Profile creation did not enter the forum.");
                Require(forum.TryCreateThread(new ForumThreadDraft("board-archive", "本地测试主题", "本地主题正文"), out var createdThread, out _), "A local thread could not be created.");
                Require(forum.TryCreateReply(new ForumReplyDraft("thread-tapes", "本地测试回复"), out _, out _), "A local reply could not be created.");
                Require(forum.FindThread(createdThread.Id) != null && forum.FindThread("thread-tapes").ReplyCount > content.FindThread("thread-tapes").ReplyCount, "Dynamic forum content was not merged into the view.");

                flow.EnterCaseFromThread("thread-guidelines");
                Require(navigator.LastScene == "InvestigationPrototype" && session.Navigation.ReturnThreadId == "thread-guidelines" && session.Navigation.ActiveCaseId == "case-observation-01", "Configured thread entry did not preserve navigation context.");
                cases.Investigate("case-observation-01", "observation-a");
                flow.SaveInvestigationCheckpoint("observation-a");
                var resumed = new GameSession(repository);
                var resumeCases = new CaseUseCases(resumed);
                var resumeContent = new ContentDrivenForumCatalog();
                var resumeNavigator = new RecordingNavigator();
                var resumeFlow = new GameFlowCoordinator(resumed, new ForumCaseUseCases(resumeContent, resumeCases), resumeNavigator);
                Require(resumeFlow.ContinueGame() && resumeNavigator.LastScene == "InvestigationPrototype", "Continue Game did not resume the saved investigation scene.");
                Require(resumeCases.GetSnapshot("case-observation-01").DiscoveredClues.Contains("observation-a"), "Saved investigation clue was not restored.");
                resumeCases.Investigate("case-observation-01", "record-fragment"); resumeCases.Acquire("case-observation-01", "test-tool"); resumeCases.Resolve("case-observation-01", "disposal-object"); resumeCases.Settle("case-observation-01");
                resumeFlow.ReturnToForum();
                var restoredForum = new ForumQueries(resumeContent, resumed);
                Require(resumeNavigator.LastScene == "ForumPrototype" && resumeCases.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Settlement did not return to the original forum context.");
                var closedThread = restoredForum.FindThread("thread-guidelines");
                Require(closedThread.ReplyCount > resumeContent.FindThread("thread-guidelines").ReplyCount, "Settlement did not add its idempotent local system reply.");
                resumeFlow.ReturnToForum();
                Require(restoredForum.FindThread("thread-guidelines").ReplyCount == closedThread.ReplyCount, "System closure reply was duplicated.");

                var reloaded = new GameSession(repository);
                var reloadedForum = new ForumQueries(new ContentDrivenForumCatalog(), reloaded);
                Require(reloaded.HasProfile && reloaded.PlayerProfile.DisplayName == "测试档案" && reloadedForum.FindThread(createdThread.Id) != null, "Reload lost profile or player-created thread.");
                Require(reloadedForum.FindThread("thread-tapes").ReplyCount > content.FindThread("thread-tapes").ReplyCount, "Reload lost player reply.");
                Require(reloaded.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Reload lost closed case state.");

                reloaded.UpdateNavigation("MainMenu", "missing-thread", "missing-case", "entry");
                var invalidNavigator = new RecordingNavigator();
                var invalidFlow = new GameFlowCoordinator(reloaded, new ForumCaseUseCases(new ContentDrivenForumCatalog(), new CaseUseCases(reloaded)), invalidNavigator);
                Require(invalidFlow.ContinueGame() && invalidNavigator.LastScene == "ForumPrototype", "Invalid navigation did not safely fall back to forum.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void VerifyBuildOrder()
        {
            Require(EditorBuildSettings.scenes.Length >= 4 && EditorBuildSettings.scenes[0].path == "Assets/Game/Scenes/Boot.unity", "Boot is not the first Build Settings scene.");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Boot.unity"); Require(UnityEngine.Object.FindFirstObjectByType<EchoForum.Bootstrap.GameBootstrap>() != null, "Boot scene has no GameBootstrap.");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity"); Require(UnityEngine.Object.FindFirstObjectByType<EchoForum.Presentation.MainMenuController>() != null, "MainMenu scene has no menu controller.");
        }

        private static void VerifyLegacyMigration()
        {
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-legacy-migration-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            try
            {
                var legacy = new CaseSnapshot { CaseId = "case-observation-01", Status = CaseStatus.Closed, CompletedAtUtc = "2026-08-30T00:00:00.0000000+00:00" };
                File.WriteAllText(Path.Combine(directory, "echo-archive-case.json"), JsonUtility.ToJson(legacy));
                var session = new GameSession(new JsonGameSaveRepository(directory));
                Require(session.SaveStatus == GameSaveLoadStatus.Valid && session.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Legacy case save did not migrate into the unified session.");
                Require(session.TryCreateProfile("迁移档案", out _), "A profile could not be created over migrated progress.");
                Require(new GameSession(new JsonGameSaveRepository(directory)).GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Migrated closed case was lost after creating the total save.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void VerifyCorruptSave()
        {
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-corrupt-save-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "echo-archive-save.json"), "not-json");
                var session = new GameSession(new JsonGameSaveRepository(directory));
                Require(session.SaveStatus == GameSaveLoadStatus.Corrupt && !session.HasProfile, "Corrupt save was not reported safely.");
                Require(session.TryCreateProfile("恢复档案", out _), "Explicit profile creation failed after a corrupt save.");
                Require(Directory.GetFiles(directory, "echo-archive-save.json.corrupt-*.json").Length == 1, "Corrupt save was not backed up before replacement.");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private sealed class RecordingNavigator : IGameNavigator { public string LastScene { get; private set; } public void LoadScene(string sceneName) { LastScene = sceneName; } public void Quit() { } }
    }
}