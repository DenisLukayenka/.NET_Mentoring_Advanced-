using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobDefinitionRepository
{
    Task<JobDefinition> GetByIdAsync(Guid id, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobDefinition>> ListByNextExecutionAsync(DateTime asOf, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task CreateAsync(JobDefinition definition, JobDetail detail, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobDefinition definition, CancellationToken cancellationToken = default);
    Task<bool> TryClaimNextExecutionAsync(Guid id, DateTime expectedNextExecutionDate, DateTime newNextExecutionDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
