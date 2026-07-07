namespace Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

public record GetJobDefinitionRequest(Guid UserId, Guid JobDefinitionId);

public record GetJobDefinitionResponse(
    Guid Id,
    string Name,
    string Description,
    string CronExpression,
    bool Active,
    DateTime NextExecutionDate,
    Guid JobDetailId);
