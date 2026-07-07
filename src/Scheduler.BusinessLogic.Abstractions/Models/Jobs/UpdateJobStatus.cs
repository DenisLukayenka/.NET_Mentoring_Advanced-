namespace Scheduler.BusinessLogic.Abstractions.Models.Jobs;

public record UpdateJobStatusRequest(
    Guid UserId,
    Guid JobId,
    Guid JobDefinitionId,
    JobStatus Status,
    string ErrorMessage);

public record UpdateJobStatusResponse(Guid JobId, JobStatus Status);
