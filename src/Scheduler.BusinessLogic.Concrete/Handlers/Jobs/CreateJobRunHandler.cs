using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;
using Scheduler.DataAccess.Abstractions.Exceptions;

namespace Scheduler.BusinessLogic.Concrete.Handlers.Jobs;

public class CreateJobRunHandler(
    IJobRepository repository,
    ILogger<CreateJobRunHandler> logger)
    : ICreateJobRunHandler
{
    public async Task<CreateJobRunResponse> HandleAsync(CreateJobRunRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 CreateJobRun (user={UserId}, def={DefId}) -> write-time unique key (consistency level N/A).", request.UserId, request.JobDefinitionId);

        var now = DateTime.UtcNow;
        var job = new Job
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = request.JobDefinitionId,
            CreatedAt = now,
            UpdatedAt = now,
            ScheduledAt = request.ScheduledAt,
            Status = JobStatus.Pending
        };

        try
        {
            await repository
                .CreateAsync(job, cancellationToken)
                .ConfigureAwait(false);

            return new CreateJobRunResponse(job.Id);
        }
        catch (DuplicateJobRunException)
        {
            logger.LogDebug("UC2.1 CreateJobRun (def={DefId}) -> backstop caught duplicate; slot already recorded by another runner.", request.JobDefinitionId);
            return new CreateJobRunResponse(job.Id, Duplicate: true);
        }
    }
}
