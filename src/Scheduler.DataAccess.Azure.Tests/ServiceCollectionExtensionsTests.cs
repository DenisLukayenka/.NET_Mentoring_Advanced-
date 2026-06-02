using NUnit.Framework;
using Scheduler.DataAccess.Abstractions.Repositories;

namespace Scheduler.DataAccess.Azure.Tests;

[TestFixture]
public class ServiceCollectionExtensionsTests
{
    [Test]
    public void AddSchedulerDataAccess_RegistersAllRepositories()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                { "DocumentDb:PrimaryConnectionString", "mongodb://localhost:27017" },
                { "DocumentDb:ReplicaConnectionString", "mongodb://localhost:27018" },
                { "CosmosDb:Endpoint", "https://localhost:8081" },
                { "CosmosDb:Key", "test-key" },
                { "CosmosDb:PrimaryRegion", "East US" },
                { "CosmosDb:SecondaryRegion", "West US" },
                { "Cassandra:ContactPoint", "localhost" },
                { "Cassandra:Port", "9042" },
                { "Cassandra:Username", "user" },
                { "Cassandra:Password", "password" },
                { "Cassandra:PrimaryRegion", "East US" },
                { "Cassandra:SecondaryRegion", "West US" },
            })
            .Build();

        services.AddSchedulerDataAccess(configuration);

        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobDefinitionRepository)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobDetailRepository)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobRepository)));
        Assert.That(services, Has.Some.Matches<ServiceDescriptor>(sd => sd.ServiceType == typeof(IJobOutputRepository)));
    }
}
