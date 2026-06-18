using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.Jobs;

public class GetJobHistoryHandler(
    IJobRepository repository,
    ILogger<GetJobHistoryHandler> logger)
    : IGetJobHistoryHandler
{
    public async Task<GetJobHistoryResponse> HandleAsync(GetJobHistoryRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 GetJobHistory def={DefId} -> Eventual (dashboard; sub-second freshness not needed).", request.JobDefinitionId);

        var jobsList = await repository
            .GetByJobDefinitionIdAsync(request.JobDefinitionId, ConsistencyLevel.Eventual, cancellationToken)
            .ConfigureAwait(false);

        var jobs = jobsList.Select(JobMappings.ToHistoryItem).ToList();
        return new GetJobHistoryResponse(jobs);
    }
}
