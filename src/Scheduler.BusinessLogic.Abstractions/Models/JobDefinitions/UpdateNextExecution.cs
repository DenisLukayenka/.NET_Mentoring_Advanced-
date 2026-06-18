namespace Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

public record UpdateNextExecutionRequest(Guid JobDefinitionId, DateTime ExpectedNextExecutionDate, DateTime NextExecutionDate);

public record UpdateNextExecutionResponse(bool Claimed);
