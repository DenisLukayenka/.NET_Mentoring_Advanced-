using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;

namespace Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;

public interface ICreateJobDefinitionHandler : IHandler<CreateJobDefinitionRequest, CreateJobDefinitionResponse>
{
}
