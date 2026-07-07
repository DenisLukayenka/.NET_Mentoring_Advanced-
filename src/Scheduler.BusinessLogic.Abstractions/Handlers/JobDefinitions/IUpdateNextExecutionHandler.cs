using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

namespace Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;

public interface IUpdateNextExecutionHandler : IHandler<UpdateNextExecutionRequest, UpdateNextExecutionResponse>
{
}
