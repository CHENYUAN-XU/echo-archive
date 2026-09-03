using EchoForum.Presentation;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    [RequireComponent(typeof(MainMenuController))]
    public sealed class MainMenuBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            var host = GameSessionHost.Require();
            GetComponent<MainMenuController>().Initialize(host.Session, host.Flow);
        }
    }
}
