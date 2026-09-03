using EchoForum.Presentation;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    [RequireComponent(typeof(InvestigationPrototypeController))]
    public sealed class InvestigationPrototypeBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            var host = GameSessionHost.Require();
            var activeCase = host.Session.Navigation == null ? null : host.Session.Navigation.ActiveCaseId;
            GetComponent<InvestigationPrototypeController>().Initialize(host.Cases, host.Flow, activeCase);
        }
    }
}