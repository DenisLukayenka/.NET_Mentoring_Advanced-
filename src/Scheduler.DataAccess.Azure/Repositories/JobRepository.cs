using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Repositories;

public class JobRepository(
    [FromKeyedServices(AzureConstants.CosmosDbPrimary)] CosmosClient primaryClient,
    [FromKeyedServices(AzureConstants.CosmosDbReplica)] CosmosClient replicaClient,
    ILogger<JobRepository> logger) : IJobRepository
{
    private const string DatabaseName = "scheduler";
    private const string ContainerName = "jobs";

    private readonly Container _primaryContainer = primaryClient.GetDatabase(DatabaseName).GetContainer(ContainerName);
    private readonly Container _replicaContainer = replicaClient.GetDatabase(DatabaseName).GetContainer(ContainerName);

    public async Task CreateAsync(Job job, CancellationToken cancellationToken = default)
    {
        try
        {
            var dto = job.ToDto();
            await _primaryContainer.CreateItemAsync(dto, new PartitionKey(job.JobDefinitionId.ToString()), cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job {JobId}", job.Id);
            throw new DataAccessException($"Failed to create job {job.Id}", ex);
        }
    }

    public async Task UpdateStatusAsync(Guid jobId, Guid jobDefinitionId, JobStatus status, string errorMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var patches = new List<PatchOperation>
            {
                PatchOperation.Set("/Status", status.ToString()),
                PatchOperation.Set("/UpdatedAt", DateTime.UtcNow)
            };

            if (errorMessage != null)
                patches.Add(PatchOperation.Set("/ErrorMessage", errorMessage));

            await _primaryContainer.PatchItemAsync<JobDto>(
                jobId.ToString(),
                new PartitionKey(jobDefinitionId.ToString()),
                patches,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for job {JobId} to {Status}", jobId, status);
            throw new DataAccessException($"Failed to update status for job {jobId} to {status}", ex);
        }
    }

    public async Task<IReadOnlyList<Job>> GetByJobDefinitionIdAsync(Guid jobDefinitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new QueryDefinition("SELECT * FROM c WHERE c.JobDefinitionId = @id")
                .WithParameter("@id", jobDefinitionId.ToString());

            var iterator = _replicaContainer.GetItemQueryIterator<JobDto>(
                query,
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(jobDefinitionId.ToString()) });

            var results = new List<Job>();

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(cancellationToken);
                results.AddRange(page.Select(JobMappings.ToModel));
            }

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get jobs for job definition {JobDefinitionId}", jobDefinitionId);
            throw new DataAccessException($"Failed to get jobs for job definition {jobDefinitionId}", ex);
        }
    }

}
