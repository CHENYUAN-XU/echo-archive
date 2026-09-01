using System;
using System.IO;
using EchoForum.Application;
using EchoForum.Domain;
using EchoForum.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace EchoForum.Editor
{
    public static class DataDrivenBindingVerifier
    {
        [MenuItem("Echo Archive/Verify Data-Driven Case Bindings")]
        public static void Verify()
        {
            ContentBindingValidator.Validate();
            var content = new ContentDrivenForumCatalog();
            var primaryDirectory = Path.Combine(Path.GetTempPath(), "echo-archive-binding-test-" + Guid.NewGuid().ToString("N"));
            var legacyDirectory = Path.Combine(Path.GetTempPath(), "echo-archive-legacy-test-" + Guid.NewGuid().ToString("N"));
            var store = new JsonCaseSaveStore(primaryDirectory);
            var cases = new CaseUseCases(new LocalCaseSessionRepository(store));
            var entries = new ForumCaseUseCases(content, cases);

            Require(content.LoadHome().Threads.Count >= 5, "Configured forum threads were not loaded.");
            Require(entries.GetEntry("thread-tapes").State == ForumCaseEntryState.None, "Unbound post unexpectedly has an event entry.");
            var available = entries.GetEntry("thread-guidelines");
            Require(available.State == ForumCaseEntryState.Available && available.CanEnter && available.Label == "前往现场", "Available configured event entry is incorrect.");
            var unavailable = entries.GetEntry("thread-future-observation");
            Require(unavailable.State == ForumCaseEntryState.Unavailable && !unavailable.CanEnter && unavailable.Label == "调查区域尚未开放", "Unavailable configured event entry is incorrect.");

            var requested = entries.RequestEntry("thread-guidelines");
            Require(requested.State == ForumCaseEntryState.InProgress && requested.Label == "继续调查", "Entry request did not start the configured case.");
            cases.Investigate(requested.CaseId, "observation-a");
            cases.Investigate(requested.CaseId, "record-fragment");
            cases.Acquire(requested.CaseId, "test-tool");
            cases.Resolve(requested.CaseId, "disposal-object");
            cases.Settle(requested.CaseId);
            var closed = entries.GetEntry("thread-guidelines");
            Require(closed.State == ForumCaseEntryState.Closed && !closed.CanEnter && !string.IsNullOrEmpty(closed.CompletionReplyText), "Configured completion entry is incorrect.");

            VerifyLegacySaveRecovery(legacyDirectory);
            store.ResetPrototypeProgress();
            Directory.Delete(primaryDirectory, true);
            Debug.Log("Data-driven binding verification passed: unbound, available, unavailable, closed states and legacy save recovery.");
        }

        private static void VerifyLegacySaveRecovery(string directory)
        {
            Directory.CreateDirectory(directory);
            var legacy = new CaseSnapshot { CaseId = "case-observation-01", Status = CaseStatus.Closed, CompletedAtUtc = "2026-08-30T00:00:00.0000000+00:00" };
            File.WriteAllText(Path.Combine(directory, "echo-archive-case.json"), JsonUtility.ToJson(legacy));
            var store = new JsonCaseSaveStore(directory);
            Require(store.Load("case-observation-01").Status == CaseStatus.Closed, "Legacy single-case save was not restored.");
            store.Save(new CaseSnapshot { CaseId = "case-observation-02", Status = CaseStatus.NotStarted });
            Require(store.Load("case-observation-01") != null && store.Load("case-observation-02") != null, "Current multi-case save did not preserve existing progress.");
            store.ResetPrototypeProgress();
            Directory.Delete(directory, true);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
