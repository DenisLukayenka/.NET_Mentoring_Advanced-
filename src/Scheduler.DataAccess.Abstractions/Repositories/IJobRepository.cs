using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobRepository
{
    Task CreateAsync(Job job, CancellationToken cancellationToken = default);
    Task<Job> GetByIdAsync(Guid id, Guid jobDefinitionId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Job>> GetByJobDefinitionIdAsync(Guid jobDefinitionId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(Guid jobId, Guid jobDefinitionId, JobStatus status, string errorMessage = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, Guid jobDefinitionId, CancellationToken cancellationToken = default);
}
