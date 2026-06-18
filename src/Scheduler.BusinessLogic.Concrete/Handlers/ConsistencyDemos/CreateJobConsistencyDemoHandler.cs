using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

namespace Scheduler.BusinessLogic.Concrete.Handlers.ConsistencyDemos;

public class CreateJobConsistencyDemoHandler(
    IJobDefinitionRepository definitions,
    IJobDetailRepository details,
    ILogger<CreateJobConsistencyDemoHandler> logger)
    : ICreateJobConsistencyDemoHandler
{
    public async Task<CreateJobConsistencyDemoResponse> HandleAsync(
        CreateJobConsistencyDemoRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var detail = new JobDetail
        {
            Id = Guid.NewGuid(),
            Type = "HttpCall",
            Payload = request.Payload,
            CreatedDate = now,
            UpdatedDate = now
        };
        var definition = new JobDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = "Consistency demo (UC 1.1)",
            CronExpression = request.CronExpression,
            Concurrency = true,
            UserId = Guid.NewGuid(),
            JobDetailId = detail.Id,
            CreatedDate = now,
            UpdatedDate = now,
            Active = true,
            NextExecutionDate = now.AddMinutes(1)
        };

        await definitions.CreateAsync(definition, detail, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("UC1.1 demo: created definition {Id} (Strong write, w:majority).", definition.Id);

        var definitionReadBack = await ConsistencyProbeRunner.ProbeAsync(
            "Read-back JobDefinition", "DocumentDB", "_id",
            level => definitions.GetByIdAsync(definition.Id, level, cancellationToken),
            d => d?.Id.ToString()).ConfigureAwait(false);

        var detailReadBack = await ConsistencyProbeRunner.ProbeAsync(
            "Read-back JobDetail", "DocumentDB", "Type",
            level => details.GetByIdAsync(detail.Id, level, cancellationToken),
            d => d?.Type).ConfigureAwait(false);

        return new CreateJobConsistencyDemoResponse(
            definition.Id,
            detail.Id,
            TransactionCommitted: true,
            DefinitionReadBack: definitionReadBack,
            DetailReadBack: detailReadBack,
            ReadYourWritesHeld: definitionReadBack.PrimaryHasValue);
    }
}
