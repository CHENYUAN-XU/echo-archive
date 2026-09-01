using UnityEngine;

namespace EchoForum.Content
{
    [CreateAssetMenu(menuName = "Echo Archive/Forum/Board Definition", fileName = "ForumBoard")]
    public sealed class ForumBoardDefinition : ScriptableObject
    {
        public string BoardId;
        public string Slug;
        public string DisplayName;
        [TextArea] public string Description;
    }
}
