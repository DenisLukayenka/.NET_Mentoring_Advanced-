using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.Jobs;

public class GetJobHandler(
    IJobRepository repository,
    ILogger<GetJobHandler> logger)
    : IGetJobHandler
{
    public async Task<GetJobResponse> HandleAsync(GetJobRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 GetJob {JobId} (user={UserId}) -> Eventual (dashboard read).", request.JobId, request.UserId);

        var job = await repository
            .GetByIdAsync(request.JobId, request.JobDefinitionId, ConsistencyLevel.Eventual, cancellationToken)
            .ConfigureAwait(false);

        return job?.ToResponse();
    }
}
