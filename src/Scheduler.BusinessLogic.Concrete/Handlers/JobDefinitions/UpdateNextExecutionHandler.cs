using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobDefinitions;

public class UpdateNextExecutionHandler(
    IJobDefinitionRepository repository,
    ILogger<UpdateNextExecutionHandler> logger)
    : IUpdateNextExecutionHandler
{
    public async Task<UpdateNextExecutionResponse> HandleAsync(UpdateNextExecutionRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 TryClaimNextExecution {Id} -> atomic findOneAndUpdate (w:majority; exactly-once slot claim).", request.JobDefinitionId);

        var claimed = await repository
            .TryClaimNextExecutionAsync(request.JobDefinitionId, request.ExpectedNextExecutionDate, request.NextExecutionDate, cancellationToken)
            .ConfigureAwait(false);

        if (claimed)
            logger.LogDebug("UC2.1 TryClaimNextExecution {Id} -> WON the slot.", request.JobDefinitionId);
        else
            logger.LogDebug("UC2.1 TryClaimNextExecution {Id} -> LOST the race; skipping execution.", request.JobDefinitionId);

        return new UpdateNextExecutionResponse(claimed);
    }
}
