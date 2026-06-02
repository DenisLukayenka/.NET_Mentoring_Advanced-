using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Repositories;

public class JobDefinitionRepository(
    [FromKeyedServices(AzureConstants.DocumentDbPrimary)] IMongoClient primaryClient,
    [FromKeyedServices(AzureConstants.DocumentDbReplica)] IMongoClient replicaClient,
    ILogger<JobDefinitionRepository> logger) : IJobDefinitionRepository
{
    private const string DatabaseName = "scheduler";
    private const string DetailCollectionName = "jobDetails";
    private const string DefinitionCollectionName = "jobDefinitions";

    private readonly IMongoClient _primaryClient = primaryClient;
    private readonly IMongoCollection<JobDefinitionDto> _primaryCollection = primaryClient.GetDatabase(DatabaseName).GetCollection<JobDefinitionDto>(DefinitionCollectionName);
    private readonly IMongoCollection<JobDefinitionDto> _replicaCollection = replicaClient.GetDatabase(DatabaseName).GetCollection<JobDefinitionDto>(DefinitionCollectionName);
    private readonly IMongoCollection<JobDetailDto> _primaryDetailCollection = primaryClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(DetailCollectionName);

    public async Task<JobDefinition> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, id);
            var dto = await _replicaCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return dto?.ToModel();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job definition {DefinitionId}", id);
            throw new DataAccessException($"Failed to get job definition {id}", ex);
        }
    }

    public async Task<IReadOnlyList<JobDefinition>> ListByNextExecutionAsync(DateTime asOf, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobDefinitionDto>.Filter.And(
                Builders<JobDefinitionDto>.Filter.Lte(x => x.NextExecutionDate, asOf),
                Builders<JobDefinitionDto>.Filter.Eq(x => x.Active, true));

            var dtos = await _replicaCollection.Find(filter).ToListAsync(cancellationToken);

            return dtos.Select(JobDefinitionMappings.ToModel).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to list job definitions by next execution as of {AsOf}", asOf);
            throw new DataAccessException($"Failed to list job definitions by next execution as of {asOf}", ex);
        }
    }

    public async Task CreateAsync(JobDefinition definition, JobDetail detail, CancellationToken cancellationToken = default)
    {
        try
        {
            using var session = await _primaryClient.StartSessionAsync(cancellationToken: cancellationToken);
            session.StartTransaction();

            try
            {
                // IClientSession is not thread-safe — do not use Task.WhenAll here; concurrent ops on the same session corrupt transaction state.
                await _primaryDetailCollection.InsertOneAsync(session, detail.ToDto(), cancellationToken: cancellationToken);
                await _primaryCollection.InsertOneAsync(session, definition.ToDto(), cancellationToken: cancellationToken);

                await session.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for job definition {DefinitionId}, aborting", definition.Id);
                await session.AbortTransactionAsync(cancellationToken);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job definition {DefinitionId}", definition.Id);
            throw new DataAccessException($"Failed to create job definition {definition.Id}", ex);
        }
    }

    public async Task UpdateNextExecutionAsync(Guid id, DateTime nextExecutionDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, id);
            var update = Builders<JobDefinitionDto>.Update
                .Set(x => x.NextExecutionDate, nextExecutionDate)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            await _primaryCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update next execution for job definition {DefinitionId}", id);
            throw new DataAccessException($"Failed to update next execution for job definition {id}", ex);
        }
    }
}
