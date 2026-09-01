namespace EchoForum.Application
{
    /// <summary>Application-level scene handoff data. Presentation only receives values returned by use cases.</summary>
    public static class ForumNavigationState
    {
        public static string ReturnThreadId { get; set; }
        public static string ActiveCaseId { get; set; }
    }
}