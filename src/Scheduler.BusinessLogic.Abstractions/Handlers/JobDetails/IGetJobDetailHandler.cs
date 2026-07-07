using Scheduler.BusinessLogic.Abstractions.Models.JobDetails;

namespace Scheduler.BusinessLogic.Abstractions.Handlers.JobDetails;

public interface IGetJobDetailHandler : IHandler<GetJobDetailRequest, GetJobDetailResponse>
{
}
