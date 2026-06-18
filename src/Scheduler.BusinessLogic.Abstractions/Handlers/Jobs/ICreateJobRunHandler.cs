using Scheduler.BusinessLogic.Abstractions.Models.Jobs;

namespace Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;

public interface ICreateJobRunHandler : IHandler<CreateJobRunRequest, CreateJobRunResponse>
{
}
