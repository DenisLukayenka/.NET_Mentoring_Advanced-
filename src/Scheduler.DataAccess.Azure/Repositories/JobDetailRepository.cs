using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;
using ConsistencyLevel = Scheduler.DataAccess.Abstractions.Consistency.ConsistencyLevel;

namespace Scheduler.DataAccess.Azure.Repositories;

internal class JobDetailRepository(
    [FromKeyedServices(AzureConstants.DocumentDbPrimary)] IMongoClient primaryClient,
    [FromKeyedServices(AzureConstants.DocumentDbReplica)] IMongoClient replicaClient,
    ILogger<JobDetailRepository> logger) : IJobDetailRepository
{
    private const string DatabaseName = "scheduler";
    private const string CollectionName = "jobDetails";

    private readonly IMongoCollection<JobDetailDto> _primaryCollection = primaryClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(CollectionName);
    private readonly IMongoCollection<JobDetailDto> _replicaCollection = replicaClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(CollectionName);

    public async Task<JobDetail> GetByIdAsync(Guid id, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find JobDetail by id={Id} started.", id);

            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, id);
            JobDetailDto dto;

            if (consistencyLevel == ConsistencyLevel.Strong)
            {
                logger.LogDebug("DocumentDB read JobDetail {Id} -> PRIMARY (primary read, default local; correctness from atomic claim).", id);
                dto = await _primaryCollection
                    .Find(filter)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                logger.LogDebug("DocumentDB read JobDetail {Id} -> REPLICA (consistency=Eventual/local).", id);
                dto = await _replicaCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

                if (dto == null)
                {
                    logger.LogDebug("DocumentDB read JobDetail {Id} -> replica miss; falling back to PRIMARY (replication lag window).", id);
                    dto = await _primaryCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            logger.LogDebug("Find JobDetail by id={Id} finished.", id);

            return dto?.ToModel();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job detail {DetailId}", id);
            throw new DataAccessException($"Failed to get job detail {id}", ex);
        }
    }

    public async Task UpdateAsync(JobDetail detail, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Update JobDetail with id={Id} started.", detail.Id);
            logger.LogDebug("DocumentDB write JobDetail {Id} -> PRIMARY.", detail.Id);

            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, detail.Id);
            var update = Builders<JobDetailDto>.Update
                .Set(x => x.Type, detail.Type)
                .Set(x => x.Payload, detail.Payload)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            await _primaryCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Update JobDetail with id={Id} finished.", detail.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update job detail {DetailId}", detail.Id);
            throw new DataAccessException($"Failed to update job detail {detail.Id}", ex);
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Delete JobDetail by id={Id} started.", id);
            logger.LogDebug("DocumentDB delete JobDetail {Id} -> PRIMARY.", id);

            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, id);
            await _primaryCollection.DeleteOneAsync(filter, cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Delete JobDetail by id={Id} finished.", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job detail {DetailId}", id);
            throw new DataAccessException($"Failed to delete job detail {id}", ex);
        }
    }
}
