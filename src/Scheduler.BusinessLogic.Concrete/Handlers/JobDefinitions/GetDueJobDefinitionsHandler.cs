using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobDefinitions;

public class GetDueJobDefinitionsHandler(
    IJobDefinitionRepository repository,
    ILogger<GetDueJobDefinitionsHandler> logger)
    : IGetDueJobDefinitionsHandler
{
    public async Task<GetDueJobDefinitionsResponse> HandleAsync(GetDueJobDefinitionsRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 GetDueJobDefinitions asOf={AsOf} -> primary read (local); exactly-once comes from the claim, not this read.", request.AsOf);

        var definitions = await repository
            .ListByNextExecutionAsync(request.AsOf, ConsistencyLevel.Strong, cancellationToken)
            .ConfigureAwait(false);

        var dueDefinitions = definitions.Select(JobDefinitionMappings.ToDueDefinition).ToList();
        return new GetDueJobDefinitionsResponse(dueDefinitions);
    }
}
