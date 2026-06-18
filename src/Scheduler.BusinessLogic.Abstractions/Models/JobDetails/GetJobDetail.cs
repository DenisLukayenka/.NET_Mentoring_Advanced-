namespace Scheduler.BusinessLogic.Abstractions.Models.JobDetails;

public record GetJobDetailRequest(Guid JobDetailId);

public record GetJobDetailResponse(Guid Id, string Type, string Payload);
