using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobDefinitions;

public class CreateJobDefinitionHandler(
    IJobDefinitionRepository repository,
    ILogger<CreateJobDefinitionHandler> logger)
    : ICreateJobDefinitionHandler
{
    public async Task<CreateJobDefinitionResponse> HandleAsync(CreateJobDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC1.1 CreateJobDefinition (user={UserId}) -> Strong write (atomic JobDefinition+JobDetail on primary).", request.UserId);

        var now = DateTime.UtcNow;
        var detail = new JobDetail
        {
            Id = Guid.NewGuid(),
            Type = request.DetailType,
            Payload = request.Payload,
            CreatedDate = now,
            UpdatedDate = now
        };
        var definition = new JobDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CronExpression = request.CronExpression,
            Concurrency = request.Concurrency,
            UserId = request.UserId,
            JobDetailId = detail.Id,
            CreatedDate = now,
            UpdatedDate = now,
            Active = true,
            NextExecutionDate = request.NextExecutionDate
        };

        await repository
            .CreateAsync(definition, detail, cancellationToken)
            .ConfigureAwait(false);

        return new CreateJobDefinitionResponse(definition.Id, detail.Id);
    }
}
