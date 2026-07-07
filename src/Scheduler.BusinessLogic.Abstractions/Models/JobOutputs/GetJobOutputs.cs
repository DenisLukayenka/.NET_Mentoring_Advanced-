namespace Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;

public record GetJobOutputsRequest(Guid JobId);

public record JobOutputItem(Guid Id, DateTime Date, JobOutputLevel Level, string Message);

public record GetJobOutputsResponse(IReadOnlyList<JobOutputItem> Outputs);
