namespace Scheduler.BusinessLogic.Abstractions.Models.Jobs;

public record CreateJobRunRequest(Guid UserId, Guid JobDefinitionId, DateTime ScheduledAt);

public record CreateJobRunResponse(Guid JobId, bool Duplicate = false);
