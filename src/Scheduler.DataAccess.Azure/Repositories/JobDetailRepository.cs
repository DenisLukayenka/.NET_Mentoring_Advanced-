using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Repositories;

public class JobDetailRepository(
    [FromKeyedServices(AzureConstants.DocumentDbPrimary)] IMongoClient primaryClient,
    [FromKeyedServices(AzureConstants.DocumentDbReplica)] IMongoClient replicaClient,
    ILogger<JobDetailRepository> logger) : IJobDetailRepository
{
    private const string DatabaseName = "scheduler";
    private const string CollectionName = "jobDetails";

    private readonly IMongoCollection<JobDetailDto> _primaryCollection = primaryClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(CollectionName);
    private readonly IMongoCollection<JobDetailDto> _replicaCollection = replicaClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(CollectionName);

    public async Task<JobDetail> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, id);
            var dto = await _replicaCollection.Find(filter).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
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
            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, detail.Id);
            var update = Builders<JobDetailDto>.Update
                .Set(x => x.Type, detail.Type)
                .Set(x => x.Payload, detail.Payload)
                .Set(x => x.UpdatedDate, DateTime.UtcNow);

            await _primaryCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken).ConfigureAwait(false);
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
            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, id);
            await _primaryCollection.DeleteOneAsync(filter, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job detail {DetailId}", id);
            throw new DataAccessException($"Failed to delete job detail {id}", ex);
        }
    }
}
