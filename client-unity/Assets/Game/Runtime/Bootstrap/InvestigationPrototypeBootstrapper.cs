using EchoForum.Bootstrap;
using EchoForum.Presentation;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    [RequireComponent(typeof(InvestigationPrototypeController))]
    public sealed class InvestigationPrototypeBootstrapper : MonoBehaviour
    {
        private void Awake() => GetComponent<InvestigationPrototypeController>().Initialize(GameSessionHost.Require().Cases);
    }
}
