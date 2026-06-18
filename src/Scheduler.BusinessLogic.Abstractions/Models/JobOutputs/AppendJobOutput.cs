namespace Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;

public record AppendJobOutputRequest(Guid JobId, JobOutputLevel Level, string Message, DateTime Date);
