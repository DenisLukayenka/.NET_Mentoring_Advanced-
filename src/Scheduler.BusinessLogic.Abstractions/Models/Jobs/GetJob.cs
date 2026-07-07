namespace Scheduler.BusinessLogic.Abstractions.Models.Jobs;

public record GetJobRequest(Guid UserId, Guid JobId, Guid JobDefinitionId);

public record GetJobResponse(
    Guid Id,
    Guid JobDefinitionId,
    JobStatus Status,
    DateTime ScheduledAt,
    string ErrorMessage);
