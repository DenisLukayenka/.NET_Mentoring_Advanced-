using MongoDB.Driver;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Repositories;

public class JobDetailRepository([FromKeyedServices(AzureConstants.DocumentDbReplica)] IMongoClient replicaClient, ILogger<JobDetailRepository> logger) : IJobDetailRepository
{
    private const string DatabaseName = "scheduler";
    private const string CollectionName = "jobDetails";

    private readonly IMongoCollection<JobDetailDto> _replicaCollection = replicaClient.GetDatabase(DatabaseName).GetCollection<JobDetailDto>(CollectionName);

    public async Task<JobDetail> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = Builders<JobDetailDto>.Filter.Eq(x => x.Id, id);
            var dto = await _replicaCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            return dto?.ToModel();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job detail {DetailId}", id);
            throw new DataAccessException($"Failed to get job detail {id}", ex);
        }
    }

}
