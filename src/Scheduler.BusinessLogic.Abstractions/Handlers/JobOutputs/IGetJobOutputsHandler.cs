using Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;

namespace Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;

public interface IGetJobOutputsHandler : IHandler<GetJobOutputsRequest, GetJobOutputsResponse>
{
}
