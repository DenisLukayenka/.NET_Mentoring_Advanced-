using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobDetails;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Concrete.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Concrete.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Concrete.Handlers.JobDetails;
using Scheduler.BusinessLogic.Concrete.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Concrete.Handlers.Jobs;

namespace Scheduler.BusinessLogic.Concrete;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerBusinessLogic(this IServiceCollection services)
    {
        services.AddSingleton<ICreateJobDefinitionHandler, CreateJobDefinitionHandler>();
        services.AddSingleton<IGetJobDefinitionHandler, GetJobDefinitionHandler>();
        services.AddSingleton<IGetDueJobDefinitionsHandler, GetDueJobDefinitionsHandler>();
        services.AddSingleton<IUpdateNextExecutionHandler, UpdateNextExecutionHandler>();

        services.AddSingleton<IGetJobDetailHandler, GetJobDetailHandler>();

        services.AddSingleton<ICreateJobRunHandler, CreateJobRunHandler>();
        services.AddSingleton<IUpdateJobStatusHandler, UpdateJobStatusHandler>();
        services.AddSingleton<IGetJobHandler, GetJobHandler>();
        services.AddSingleton<IGetJobHistoryHandler, GetJobHistoryHandler>();

        services.AddSingleton<IAppendJobOutputHandler, AppendJobOutputHandler>();
        services.AddSingleton<IGetJobOutputsHandler, GetJobOutputsHandler>();

        services.AddSingleton<ICreateJobConsistencyDemoHandler, CreateJobConsistencyDemoHandler>();
        services.AddSingleton<IExecuteJobConsistencyDemoHandler, ExecuteJobConsistencyDemoHandler>();

        return services;
    }
}
