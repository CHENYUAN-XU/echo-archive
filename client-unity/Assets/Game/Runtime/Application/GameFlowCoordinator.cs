using EchoForum.Domain;

namespace EchoForum.Application
{
    public interface IGameNavigator
    {
        void LoadScene(string sceneName);
        void Quit();
    }

    public interface IGameFlowCoordinator
    {
        void OpenMainMenu();
        bool CreateProfileAndEnterForum(string displayName, out string error);
        bool ContinueToForum();
        void EnterCaseFromThread(string threadId);
        void ReturnToForum();
        void ReturnToMainMenu();
        void RememberForumThread(string threadId);
        void QuitGame();
    }

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

        public bool CreateProfileAndEnterForum(string displayName, out string error)
        {
            if (!session.TryCreateProfile(displayName, out error)) return false;
            OpenForum(null);
            return true;
        }

        public bool ContinueToForum()
        {
            if (!session.HasProfile) return false;
            OpenForum(session.ForumState.LastOpenedThreadId);
            return true;
        }

        public void EnterCaseFromThread(string threadId)
        {
            var entry = forumCases.RequestEntry(threadId);
            if (!entry.CanEnter || string.IsNullOrEmpty(entry.SceneName)) return;
            session.UpdateNavigation(entry.SceneName, entry.ThreadId, entry.CaseId);
            navigator.LoadScene(entry.SceneName);
        }

        public void ReturnToForum()
        {
            if (!session.HasProfile) { OpenMainMenu(); return; }
            OpenForum(session.Navigation == null ? null : session.Navigation.ReturnThreadId);
        }

        public void ReturnToMainMenu()
        {
            session.SaveNow();
            navigator.LoadScene(MainMenuScene);
        }

        public void RememberForumThread(string threadId) => session.RememberForumThread(threadId);
        public void QuitGame() { session.SaveNow(); navigator.Quit(); }

        private void OpenForum(string threadId)
        {
            session.UpdateNavigation(ForumScene, threadId, null);
            navigator.LoadScene(ForumScene);
        }
    }
}
