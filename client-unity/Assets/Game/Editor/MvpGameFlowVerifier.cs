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
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-mvp-flow-" + Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new JsonGameSaveRepository(directory);
                var session = new GameSession(repository);
                var content = new ContentDrivenForumCatalog();
                var cases = new CaseUseCases(session);
                var navigator = new RecordingNavigator();
                var flow = new GameFlowCoordinator(session, new ForumCaseUseCases(content, cases), navigator);

                Require(!session.HasProfile && !flow.ContinueToForum(), "A first launch unexpectedly continued a profile.");
                Require(!flow.CreateProfileAndEnterForum("  ", out _), "An empty display name created a profile.");
                Require(flow.CreateProfileAndEnterForum("测试档案", out _), "A valid local profile could not be created.");
                Require(navigator.LastScene == "ForumPrototype" && session.PlayerProfile.DisplayName == "测试档案", "Profile creation did not enter the forum.");

                flow.EnterCaseFromThread("thread-guidelines");
                Require(navigator.LastScene == "InvestigationPrototype" && session.Navigation.ReturnThreadId == "thread-guidelines" && session.Navigation.ActiveCaseId == "case-observation-01", "Configured thread entry did not preserve navigation context.");
                cases.Investigate("case-observation-01", "observation-a");
                cases.Investigate("case-observation-01", "record-fragment");
                cases.Acquire("case-observation-01", "test-tool");
                cases.Resolve("case-observation-01", "disposal-object");
                cases.Settle("case-observation-01");
                flow.ReturnToForum();
                Require(navigator.LastScene == "ForumPrototype" && cases.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Settlement did not return to the original forum context.");

                var restored = new GameSession(repository);
                Require(restored.HasProfile && restored.PlayerProfile.DisplayName == "测试档案" && restored.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Continue did not restore the profile and closed case state.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }

            VerifyLegacyMigration();
            VerifyCorruptSave();
            Debug.Log("MVP game-flow verification passed: boot order, first profile, continue, configured navigation, settlement persistence, legacy migration, and corrupt-save recovery.");
        }

        private static void VerifyBuildOrder()
        {
            Require(EditorBuildSettings.scenes.Length >= 4 && EditorBuildSettings.scenes[0].path == "Assets/Game/Scenes/Boot.unity", "Boot is not the first Build Settings scene.");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Boot.unity");
            Require(UnityEngine.Object.FindFirstObjectByType<EchoForum.Bootstrap.GameBootstrap>() != null, "Boot scene has no GameBootstrap.");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/MainMenu.unity");
            Require(UnityEngine.Object.FindFirstObjectByType<EchoForum.Presentation.MainMenuController>() != null, "MainMenu scene has no menu controller.");
        }

        private static void VerifyLegacyMigration()
        {
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-legacy-migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var legacy = new CaseSnapshot { CaseId = "case-observation-01", Status = CaseStatus.Closed, CompletedAtUtc = "2026-08-30T00:00:00.0000000+00:00" };
                File.WriteAllText(Path.Combine(directory, "echo-archive-case.json"), JsonUtility.ToJson(legacy));
                var session = new GameSession(new JsonGameSaveRepository(directory));
                Require(session.SaveStatus == GameSaveLoadStatus.Valid && session.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Legacy case save did not migrate into the unified session.");
                Require(session.TryCreateProfile("迁移档案", out _), "A profile could not be created over migrated progress.");
                var restored = new GameSession(new JsonGameSaveRepository(directory));
                Require(restored.GetSnapshot("case-observation-01").Status == CaseStatus.Closed, "Migrated closed case was lost after creating the total save.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void VerifyCorruptSave()
        {
            var directory = Path.Combine(Path.GetTempPath(), "echo-archive-corrupt-save-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(Path.Combine(directory, "echo-archive-save.json"), "not-json");
                var session = new GameSession(new JsonGameSaveRepository(directory));
                Require(session.SaveStatus == GameSaveLoadStatus.Corrupt && !session.HasProfile, "Corrupt save was not reported safely.");
                Require(session.TryCreateProfile("恢复档案", out _), "Explicit profile creation failed after a corrupt save.");
                Require(Directory.GetFiles(directory, "echo-archive-save.json.corrupt-*.json").Length == 1, "Corrupt save was not backed up before replacement.");
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class RecordingNavigator : IGameNavigator
        {
            public string LastScene { get; private set; }
            public void LoadScene(string sceneName) { LastScene = sceneName; }
            public void Quit() { }
        }
    }
}
