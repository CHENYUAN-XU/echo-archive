using EchoForum.Application;
using EchoForum.Infrastructure;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    public sealed class GameSessionHost : MonoBehaviour
    {
        public static GameSessionHost Instance { get; private set; }
        public ForumQueries Forum { get; private set; }
        public CaseUseCases Cases { get; private set; }
        public string ReturnThreadId { get; set; }
        public static GameSessionHost Require() => Instance ?? new GameObject("GameSessionHost").AddComponent<GameSessionHost>();
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Cases = new CaseUseCases(new LocalCaseSessionRepository(new JsonCaseSaveStore()));
            Forum = new LocalForumQueriesFactory().Create();
        }
    }
}
