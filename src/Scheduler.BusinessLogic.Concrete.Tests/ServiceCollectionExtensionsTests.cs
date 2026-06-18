using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Concrete;

namespace Scheduler.BusinessLogic.Concrete.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddSchedulerBusinessLogic_RegistersHandlers()
    {
        var services = new ServiceCollection();

        services.AddSchedulerBusinessLogic();

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd =>
            sd.ServiceType == typeof(ICreateJobDefinitionHandler)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd =>
            sd.ServiceType == typeof(IUpdateNextExecutionHandler)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd =>
            sd.ServiceType == typeof(IAppendJobOutputHandler)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd =>
            sd.ServiceType == typeof(ICreateJobConsistencyDemoHandler)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd =>
            sd.ServiceType == typeof(IExecuteJobConsistencyDemoHandler)));
    }
}
