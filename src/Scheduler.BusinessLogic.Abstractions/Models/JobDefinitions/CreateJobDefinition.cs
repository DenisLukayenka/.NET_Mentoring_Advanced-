namespace Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

public record CreateJobDefinitionRequest(
    Guid UserId,
    string Name,
    string Description,
    string CronExpression,
    bool Concurrency,
    DateTime NextExecutionDate,
    string DetailType,
    string Payload);

public record CreateJobDefinitionResponse(Guid JobDefinitionId, Guid JobDetailId);
