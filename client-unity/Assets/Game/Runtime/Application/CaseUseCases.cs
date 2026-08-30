using System.Collections.Generic;
using EchoForum.Domain;

namespace EchoForum.Application
{
    public interface ICaseSessionRepository
    {
        CaseSnapshot GetSnapshot(string caseId);
        IReadOnlyList<CaseEvent> Dispatch(CaseCommand command);
    }

    public interface ICaseProgressReader
    {
        CaseSnapshot GetSnapshot(string caseId);
    }

    public sealed class CaseUseCases : ICaseProgressReader
    {
        private readonly ICaseSessionRepository repository;
        public CaseUseCases(ICaseSessionRepository repository) { this.repository = repository; }
        public CaseSnapshot GetSnapshot(string caseId) => repository.GetSnapshot(caseId);
        public IReadOnlyList<CaseEvent> Start(string id) => repository.Dispatch(new StartCaseCommand(id));
        public IReadOnlyList<CaseEvent> Investigate(string id, string clue) => repository.Dispatch(new InvestigateCaseCommand(id, clue));
        public IReadOnlyList<CaseEvent> Acquire(string id, string item) => repository.Dispatch(new AcquireCaseItemCommand(id, item));
        public IReadOnlyList<CaseEvent> Resolve(string id, string target) => repository.Dispatch(new ResolveCaseObjectCommand(id, target));
        public IReadOnlyList<CaseEvent> Settle(string id) => repository.Dispatch(new SettleCaseCommand(id));
    }
}
