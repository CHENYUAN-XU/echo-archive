using System;

namespace EchoForum.Application
{
    public interface IGameNavigator { void LoadScene(string sceneName); void Quit(); }

    public interface IGameFlowCoordinator
    {
        void OpenMainMenu();
        bool CreateProfileAndEnterForum(string displayName, out string error);
        bool ContinueGame();
        bool ContinueToForum();
        bool ResumeInvestigation();
        void EnterCaseFromThread(string threadId);
        void ReturnToForum();
        void ReturnToMainMenu();
        void RememberForumThread(string threadId);
        void SaveInvestigationCheckpoint(string checkpointId);
        void QuitGame();
    }

    /// <summary>Single navigation owner. Case/thread routing always comes from saved context plus content bindings.</summary>
    public sealed class GameFlowCoordinator : IGameFlowCoordinator
    {
        private const string MainMenuScene = "MainMenu";
        private const string ForumScene = "ForumPrototype";
        private readonly IGameSession session;
        private readonly ForumCaseUseCases forumCases;
        private readonly IGameNavigator navigator;

        public GameFlowCoordinator(IGameSession session, ForumCaseUseCases forumCases, IGameNavigator navigator)
        {
            this.session = session;
            this.forumCases = forumCases;
            this.navigator = navigator;
        }

        public void OpenMainMenu() => navigator.LoadScene(MainMenuScene);
        public bool CreateProfileAndEnterForum(string displayName, out string error) { if (!session.TryCreateProfile(displayName, out error)) return false; OpenForum(null); return true; }
        public bool ContinueGame()
        {
            if (!session.HasProfile) return false;
            var hadSavedInvestigation = session.Navigation != null && !string.IsNullOrWhiteSpace(session.Navigation.ActiveCaseId) && !string.IsNullOrWhiteSpace(session.Navigation.LastSceneName);
            if (ResumeInvestigation()) return true;
            if (hadSavedInvestigation) return true;
            OpenForum(session.ForumState.LastOpenedThreadId);
            return true;
        }
        public bool ContinueToForum() { if (!session.HasProfile) return false; OpenForum(session.ForumState.LastOpenedThreadId); return true; }

        public bool ResumeInvestigation()
        {
            if (!session.HasProfile || session.Navigation == null) return false;
            var navigation = session.Navigation;
            if (string.IsNullOrWhiteSpace(navigation.ActiveCaseId) || string.IsNullOrWhiteSpace(navigation.ReturnThreadId) || string.IsNullOrWhiteSpace(navigation.LastSceneName)) return false;
            var entry = forumCases.GetEntry(navigation.ReturnThreadId);
            if (entry.State != ForumCaseEntryState.InProgress || entry.CaseId != navigation.ActiveCaseId || string.IsNullOrWhiteSpace(entry.SceneName) || entry.SceneName != navigation.LastSceneName)
            {
                session.SetForumNotice("未能恢复上次调查，已安全返回论坛首页。");
                OpenForum(null);
                return false;
            }
            session.UpdateNavigation(entry.SceneName, entry.ThreadId, entry.CaseId, navigation.CheckpointId);
            navigator.LoadScene(entry.SceneName);
            return true;
        }

        public void EnterCaseFromThread(string threadId)
        {
            var entry = forumCases.RequestEntry(threadId);
            if (!entry.CanEnter || string.IsNullOrEmpty(entry.SceneName)) return;
            session.UpdateNavigation(entry.SceneName, entry.ThreadId, entry.CaseId, "entry");
            navigator.LoadScene(entry.SceneName);
        }

        public void ReturnToForum()
        {
            if (!session.HasProfile) { OpenMainMenu(); return; }
            var threadId = session.Navigation == null ? null : session.Navigation.ReturnThreadId;
            if (!string.IsNullOrWhiteSpace(threadId))
            {
                var entry = forumCases.GetEntry(threadId);
                if (entry.State == ForumCaseEntryState.Closed && !string.IsNullOrWhiteSpace(entry.CompletionReplyText)) session.EnsureSystemCaseReply(entry.ThreadId, entry.CaseId, entry.CompletionReplyText, entry.CompletedAtUtc);
            }
            OpenForum(threadId);
        }

        public void ReturnToMainMenu() { session.SaveNow(); navigator.LoadScene(MainMenuScene); }
        public void RememberForumThread(string threadId) => session.RememberForumThread(threadId);
        public void SaveInvestigationCheckpoint(string checkpointId)
        {
            if (!session.HasProfile || session.Navigation == null || string.IsNullOrWhiteSpace(session.Navigation.ActiveCaseId)) return;
            session.UpdateNavigation(session.Navigation.LastSceneName, session.Navigation.ReturnThreadId, session.Navigation.ActiveCaseId, checkpointId);
        }
        public void QuitGame() { session.SaveNow(); navigator.Quit(); }
        private void OpenForum(string threadId) { session.UpdateNavigation(ForumScene, threadId, null, null); navigator.LoadScene(ForumScene); }
    }
}