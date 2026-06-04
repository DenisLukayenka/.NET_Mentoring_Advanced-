using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobDefinitionRepository
{
    Task<JobDefinition> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobDefinition>> ListByNextExecutionAsync(DateTime asOf, CancellationToken cancellationToken = default);
    Task CreateAsync(JobDefinition definition, JobDetail detail, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobDefinition definition, CancellationToken cancellationToken = default);
    Task UpdateNextExecutionAsync(Guid id, DateTime nextExecutionDate, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
