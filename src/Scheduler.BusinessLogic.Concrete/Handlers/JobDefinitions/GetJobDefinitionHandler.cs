using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobDefinitions;

public class GetJobDefinitionHandler(
    IJobDefinitionRepository repository,
    ILogger<GetJobDefinitionHandler> logger)
    : IGetJobDefinitionHandler
{
    public async Task<GetJobDefinitionResponse> HandleAsync(GetJobDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC1.1 GetJobDefinition {Id} (user={UserId}) -> read-after-write via plain primary read (causal sessions not implemented).", request.JobDefinitionId, request.UserId);

        var definition = await repository
            .GetByIdAsync(request.JobDefinitionId, ConsistencyLevel.Strong, cancellationToken)
            .ConfigureAwait(false);

        return definition?.ToResponse();
    }
}
