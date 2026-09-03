using EchoForum.Application;
using EchoForum.Infrastructure;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    /// <summary>The sole Unity composition root. All scenes share this session instead of constructing local repositories.</summary>
    public sealed class GameSessionHost : MonoBehaviour
    {
        public static GameSessionHost Instance { get; private set; }
        public GameSession Session { get; private set; }
        public ForumQueries Forum { get; private set; }
        public CaseUseCases Cases { get; private set; }
        public ForumCaseUseCases ForumCases { get; private set; }
        public GameFlowCoordinator Flow { get; private set; }
        public static GameSessionHost Require() => Instance ?? new GameObject("GameSessionHost").AddComponent<GameSessionHost>();
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Session = new GameSession(new JsonGameSaveRepository());
            Cases = new CaseUseCases(Session);
            var content = new ContentDrivenForumCatalog();
            Forum = new ForumQueries(content);
            ForumCases = new ForumCaseUseCases(content, Cases);
            Flow = new GameFlowCoordinator(Session, ForumCases, new UnityGameNavigator());
            UnityEngine.Application.quitting += SaveOnQuit;
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            UnityEngine.Application.quitting -= SaveOnQuit;
            Session?.SaveNow();
            Instance = null;
        }

        private void SaveOnQuit() => Session?.SaveNow();
    }
}