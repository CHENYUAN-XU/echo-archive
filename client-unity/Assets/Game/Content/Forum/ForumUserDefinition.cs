using UnityEngine;

namespace EchoForum.Content
{
    [CreateAssetMenu(menuName = "Echo Archive/Forum/User Definition", fileName = "ForumUser")]
    public sealed class ForumUserDefinition : ScriptableObject
    {
        public string UserId;
        public string DisplayName;
        public string PresenceLabel;
        public bool IsOfficial;
    }
}
