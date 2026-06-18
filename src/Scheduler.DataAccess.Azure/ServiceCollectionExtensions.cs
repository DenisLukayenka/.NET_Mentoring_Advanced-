using Cassandra;
using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Options;
using Scheduler.DataAccess.Azure.Repositories;

namespace Scheduler.DataAccess.Azure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerDataAccess(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DocumentDbOptions>(configuration.GetSection(DocumentDbOptions.Position));
        services.Configure<CosmosDbOptions>(configuration.GetSection(CosmosDbOptions.Position));
        services.Configure<CassandraOptions>(configuration.GetSection(CassandraOptions.Position));

        services.AddKeyedSingleton<IMongoClient>(AzureConstants.DocumentDbPrimary, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<DocumentDbOptions>>().Value;
            return new MongoClient(options.PrimaryConnectionString);
        });

        services.AddKeyedSingleton<IMongoClient>(AzureConstants.DocumentDbReplica, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<DocumentDbOptions>>().Value;
            return new MongoClient(options.ReplicaConnectionString);
        });

        services.AddKeyedSingleton(AzureConstants.CosmosDbPrimary, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            return new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions
            {
                ApplicationPreferredRegions = [options.PrimaryRegion, options.SecondaryRegion]
            });
        });

        services.AddKeyedSingleton(AzureConstants.CosmosDbReplica, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosDbOptions>>().Value;
            return new CosmosClient(options.Endpoint, options.Key, new CosmosClientOptions
            {
                ApplicationPreferredRegions = [options.SecondaryRegion, options.PrimaryRegion],
            });
        });

        services.AddKeyedSingleton(AzureConstants.CassandraPrimary, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<CassandraOptions>>().Value;
            return BuildCassandraSession(options, options.PrimaryRegion);
        });

        services.AddKeyedSingleton(AzureConstants.CassandraReplica, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<CassandraOptions>>().Value;
            return BuildCassandraSession(options, options.SecondaryRegion);
        });

        services.AddSingleton<IJobDefinitionRepository, JobDefinitionRepository>();
        services.AddSingleton<IJobDetailRepository, JobDetailRepository>();
        services.AddSingleton<IJobRepository, JobRepository>();
        services.AddSingleton<IJobOutputRepository, JobOutputRepository>();

        return services;
    }

    private static ISession BuildCassandraSession(CassandraOptions options, string region)
    {
        var cluster = Cluster.Builder()
            .AddContactPoint(options.ContactPoint)
            .WithPort(options.Port)
            .WithCredentials(options.Username, options.Password)
            .WithSSL(new SSLOptions(SslProtocols.Tls12, false, (_, _, _, _) => true))
            .WithLoadBalancingPolicy(new DCAwareRoundRobinPolicy(region))
            .WithPoolingOptions(PoolingOptions.Create()
                .SetCoreConnectionsPerHost(HostDistance.Local, 10)
                .SetMaxConnectionsPerHost(HostDistance.Local, 10)
                .SetCoreConnectionsPerHost(HostDistance.Remote, 10)
                .SetMaxConnectionsPerHost(HostDistance.Remote, 10))
            .WithSocketOptions(new SocketOptions()
                .SetReadTimeoutMillis(90000))
            .WithReconnectionPolicy(new ConstantReconnectionPolicy(1000))
            .Build();
        return cluster.Connect("scheduler");
    }

}
