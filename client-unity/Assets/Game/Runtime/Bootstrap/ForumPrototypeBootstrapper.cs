using EchoForum.Application;
using EchoForum.Infrastructure;
using EchoForum.Presentation;
using UnityEngine;

namespace EchoForum.Bootstrap
{
    /// <summary>
    /// 最外层 Unity 组合根。它负责选择本地适配器，避免 Infrastructure 反向依赖 UI。
    /// </summary>
    [RequireComponent(typeof(ForumPrototypeController))]
    public sealed class ForumPrototypeBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            var controller = GetComponent<ForumPrototypeController>();
            controller.Initialize(new LocalForumQueriesFactory().Create());
        }
    }
}
