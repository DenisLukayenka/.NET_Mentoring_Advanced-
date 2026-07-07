namespace Scheduler.BusinessLogic.Abstractions.Models.Jobs;

public record GetJobHistoryRequest(Guid JobDefinitionId);

public record JobHistoryItem(Guid Id, JobStatus Status, DateTime ScheduledAt);

public record GetJobHistoryResponse(IReadOnlyList<JobHistoryItem> Jobs);
