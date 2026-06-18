using Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobOutputs;

public class GetJobOutputsHandler(
    IJobOutputRepository repository,
    ILogger<GetJobOutputsHandler> logger)
    : IGetJobOutputsHandler
{
    public async Task<GetJobOutputsResponse> HandleAsync(GetJobOutputsRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 GetJobOutputs job={JobId} -> Eventual (LOCAL_ONE; diagnostics).", request.JobId);

        var jobOutputs = await repository
            .GetByJobIdAsync(request.JobId, ConsistencyLevel.Eventual, cancellationToken)
            .ConfigureAwait(false);

        var outputs = jobOutputs.Select(JobOutputMappings.ToItem).ToList();
        return new GetJobOutputsResponse(outputs);
    }
}
