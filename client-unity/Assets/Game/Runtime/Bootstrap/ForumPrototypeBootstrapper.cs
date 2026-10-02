using EchoForum.Presentation;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    [RequireComponent(typeof(ForumPrototypeController))]
    public sealed class ForumPrototypeBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            var host = GameSessionHost.Require();
            var returnThread = host.Session.Navigation == null ? null : host.Session.Navigation.ReturnThreadId;
            GetComponent<ForumPrototypeController>().Initialize(host.Forum, host.Community, host.ForumCases, host.Flow, host.Session, returnThread);
        }
    }
}
