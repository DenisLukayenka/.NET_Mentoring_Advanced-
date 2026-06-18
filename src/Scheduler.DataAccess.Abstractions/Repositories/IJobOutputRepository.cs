using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobOutputRepository
{
    Task CreateAsync(JobOutput output, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobOutput>> GetByJobIdAsync(Guid jobId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobOutput>> GetByJobIdAndDateRangeAsync(Guid jobId, DateTime from, DateTime to, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task DeleteByJobIdAsync(Guid jobId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
}
