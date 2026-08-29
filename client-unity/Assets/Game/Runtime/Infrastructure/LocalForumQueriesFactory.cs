using EchoForum.Application;

namespace EchoForum.Infrastructure
{
    /// <summary>
    /// Creates the P1 local implementation of forum read use cases.
    /// </summary>
    public sealed class LocalForumQueriesFactory
    {
        public ForumQueries Create()
        {
            return new ForumQueries(new LocalForumCatalog());
        }
    }
}
