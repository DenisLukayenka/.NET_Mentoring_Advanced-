using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobDetailRepository
{
    Task<JobDetail> GetByIdAsync(Guid id, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default);
    Task UpdateAsync(JobDetail detail, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
