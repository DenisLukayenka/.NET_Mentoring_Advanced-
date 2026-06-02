using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Abstractions.Repositories;

public interface IJobOutputRepository
{
    Task CreateAsync(JobOutput output, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JobOutput>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
}
