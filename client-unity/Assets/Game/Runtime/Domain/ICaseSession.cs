using System.Collections.Generic;

namespace EchoForum.Domain
{
    /// <summary>
    /// 事件会话边界。初期由本地实现持有，联机时可以由权威主机或服务端实现。
    /// </summary>
    public interface ICaseSession
    {
        CaseSnapshot Snapshot { get; }
        IReadOnlyList<CaseEvent> Execute(CaseCommand command);
        void Restore(CaseSnapshot snapshot);
    }
}
