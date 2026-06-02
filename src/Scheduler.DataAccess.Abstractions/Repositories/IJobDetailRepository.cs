using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobDetailRepository
{
    Task<JobDetail> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
