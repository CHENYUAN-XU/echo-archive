using UnityEngine;

namespace EchoForum.Bootstrap
{
    /// <summary>Boot scene entry point. It creates the single composition root and hands off to the menu.</summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake() => GameSessionHost.Require();
        private void Start() => GameSessionHost.Require().Flow.OpenMainMenu();
    }
}
