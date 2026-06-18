namespace Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

public record GetDueJobDefinitionsRequest(DateTime AsOf);

public record DueJobDefinition(Guid Id, string Name, Guid JobDetailId, DateTime NextExecutionDate);

public record GetDueJobDefinitionsResponse(IReadOnlyList<DueJobDefinition> Definitions);
