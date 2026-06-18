using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;
using ConsistencyLevel = Scheduler.DataAccess.Abstractions.Consistency.ConsistencyLevel;

namespace Scheduler.DataAccess.Azure.Repositories;

internal class JobDefinitionRepository(
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

    public async Task<JobDefinition> GetByIdAsync(Guid id, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find by id {Id} started.", id);

            var filter = Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, id);
            JobDefinitionDto dto;

            if (consistencyLevel == ConsistencyLevel.Strong)
            {
                logger.LogDebug("DocumentDB read JobDefinition {Id} -> PRIMARY (primary read, default local; correctness from atomic claim).", id);
                dto = await _primaryCollection
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                logger.LogDebug("DocumentDB read JobDefinition {Id} -> REPLICA (consistency=Eventual/local).", id);
                dto = await _replicaCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }

            logger.LogDebug("Find by id {Id} finished.", id);

            return dto?.ToModel();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job definition {DefinitionId}", id);
            throw new DataAccessException($"Failed to get job definition {id}", ex);
        }
    }

    public async Task<IReadOnlyList<JobDefinition>> ListByNextExecutionAsync(DateTime asOf, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find by NextExecutionDate <= {AsOf} started", asOf);

            var filter = Builders<JobDefinitionDto>.Filter.And(
                Builders<JobDefinitionDto>.Filter.Lte(x => x.NextExecutionDate, asOf),
                Builders<JobDefinitionDto>.Filter.Eq(x => x.Active, true));

            List<JobDefinitionDto> dtos;

            if (consistencyLevel == ConsistencyLevel.Strong)
            {
                logger.LogDebug("DocumentDB list JobDefinitions asOf={AsOf} -> PRIMARY (primary read, default local; correctness from atomic claim).", asOf);
                dtos = await _primaryCollection
                    .Find(filter)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                logger.LogDebug("DocumentDB list JobDefinitions asOf={AsOf} -> REPLICA (consistency=Eventual/local).", asOf);
                dtos = await _replicaCollection.Find(filter).ToListAsync(cancellationToken).ConfigureAwait(false);
            }

            var result = dtos.Select(JobDefinitionMappings.ToModel).ToList();

            logger.LogDebug("Find by NextExecutionDate <= {AsOf} finished", asOf);

            return result;
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
            using var session = await _primaryClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            session.StartTransaction(new TransactionOptions(writeConcern: WriteConcern.WMajority));

            try
            {
                logger.LogDebug("Create JobDefinition and JobDetail with id={Id} started.", definition.Id);
                logger.LogDebug("DocumentDB write JobDefinition {Id} -> PRIMARY (transaction, w:majority).", definition.Id);

                // IClientSession is not thread-safe — do not use Task.WhenAll here; concurrent ops on the same session corrupt transaction state.
                await _primaryDetailCollection.InsertOneAsync(session, detail.ToDto(), cancellationToken: cancellationToken).ConfigureAwait(false);
                await _primaryCollection.InsertOneAsync(session, definition.ToDto(), cancellationToken: cancellationToken).ConfigureAwait(false);

                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

                logger.LogDebug("Create JobDefinition and JobDetail with id={Id} finished.", definition.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for job definition {DefinitionId}, aborting", definition.Id);
                await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job definition {DefinitionId}", definition.Id);
            throw new DataAccessException($"Failed to create job definition {definition.Id}", ex);
        }
    }

    public async Task UpdateAsync(JobDefinition definition, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Update JobDefinition with id={Id} started", definition.Id);
            logger.LogDebug("DocumentDB write JobDefinition {Id} -> PRIMARY.", definition.Id);

            var filter = Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, definition.Id);
            var update = Builders<JobDefinitionDto>.Update
                .Set(x => x.Name, definition.Name)
                .Set(x => x.Description, definition.Description)
                .Set(x => x.CronExpression, definition.CronExpression)
                .Set(x => x.Concurrency, definition.Concurrency)
                .Set(x => x.Active, definition.Active)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            await _primaryCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Update JobDefinition with id={Id} finished", definition.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job definition {DefinitionId}", definition.Id);
            throw new DataAccessException($"Failed to update job definition {definition.Id}", ex);
        }
    }

    public async Task<bool> TryClaimNextExecutionAsync(Guid id, DateTime expectedNextExecutionDate, DateTime newNextExecutionDate, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("TryClaimNextExecution for JobDefinition {Id} started.", id);
            logger.LogDebug("DocumentDB write JobDefinition {Id} NextExecution -> PRIMARY (atomic findOneAndUpdate, w:majority; claim slot).", id);

            var filter = Builders<JobDefinitionDto>.Filter.And(
                Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, id),
                Builders<JobDefinitionDto>.Filter.Eq(x => x.NextExecutionDate, expectedNextExecutionDate));

            var update = Builders<JobDefinitionDto>.Update
                .Set(x => x.NextExecutionDate, newNextExecutionDate)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            var result = await _primaryCollection
                .WithWriteConcern(WriteConcern.WMajority)
                .FindOneAndUpdateAsync(filter, update, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            var claimed = result != null;
            if (claimed)
                logger.LogDebug("TryClaimNextExecution for JobDefinition {Id} -> WON the slot.", id);
            else
                logger.LogDebug("TryClaimNextExecution for JobDefinition {Id} -> LOST the race (slot already claimed).", id);

            return claimed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to claim next execution for job definition {DefinitionId}", id);
            throw new DataAccessException($"Failed to claim next execution for job definition {id}", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Delete JobDefinition and JobDetail by id={Id} started", id);
            logger.LogDebug("DocumentDB delete JobDefinition {Id} -> PRIMARY (transaction).", id);

            var definitionFilter = Builders<JobDefinitionDto>.Filter.Eq(x => x.Id, id);
            var definition = await _primaryCollection.Find(definitionFilter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            if (definition == null)
                return;

            using var session = await _primaryClient.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            session.StartTransaction();

            try
            {
                var detailFilter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, definition.JobDetailId);
                await _primaryDetailCollection.DeleteOneAsync(session, detailFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
                await _primaryCollection.DeleteOneAsync(session, definitionFilter, cancellationToken: cancellationToken).ConfigureAwait(false);
                await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);

                logger.LogDebug("Delete JobDefinition and JobDetail by id={Id} finished", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction failed for deleting job definition {DefinitionId}, aborting", id);
                await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
                throw;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job definition {DefinitionId}", id);
            throw new DataAccessException($"Failed to delete job definition {id}", ex);
        }
    }
}
