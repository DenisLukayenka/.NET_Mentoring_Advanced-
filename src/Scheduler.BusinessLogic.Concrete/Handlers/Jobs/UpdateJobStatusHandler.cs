using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;

namespace Scheduler.BusinessLogic.Concrete.Handlers.Jobs;

public class UpdateJobStatusHandler(
    IJobRepository repository,
    ILogger<UpdateJobStatusHandler> logger)
    : IUpdateJobStatusHandler
{
    public async Task<UpdateJobStatusResponse> HandleAsync(UpdateJobStatusRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 UpdateJobStatus {JobId} -> {Status} (Session account default).", request.JobId, request.Status);

        await repository
            .UpdateStatusAsync(request.JobId, request.JobDefinitionId, request.Status, request.ErrorMessage,
                cancellationToken)
            .ConfigureAwait(false);

        return new UpdateJobStatusResponse(request.JobId, request.Status);
    }
}
