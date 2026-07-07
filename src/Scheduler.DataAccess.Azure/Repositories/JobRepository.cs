using System.Net;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;
using ConsistencyLevel = Scheduler.DataAccess.Abstractions.Consistency.ConsistencyLevel;
using CosmosConsistencyLevel = Microsoft.Azure.Cosmos.ConsistencyLevel;

namespace Scheduler.DataAccess.Azure.Repositories;

internal class JobRepository(
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
            logger.LogDebug("Create Job with id={Id} and JobDefinitionId={JobDefinitionId} started.", job.Id, job.JobDefinitionId);
            logger.LogDebug("Cosmos write Job {Id} -> PRIMARY (write-time unique key; consistency level N/A).", job.Id);

            var dto = job.ToDto();
            await _primaryContainer.CreateItemAsync(dto, new PartitionKey(job.JobDefinitionId.ToString()), cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Create Job with id={Id} and JobDefinitionId={JobDefinitionId} finished.", job.Id, job.JobDefinitionId);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            logger.LogDebug("Cosmos write Job {Id} -> duplicate run detected (unique key conflict).", job.Id);
            throw new DuplicateJobRunException($"Duplicate job run: job definition {job.JobDefinitionId} at {job.ScheduledAt} already exists", ex);
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
            logger.LogDebug("Update Job status to {Status} with id={Id} and JobDefinitionId={JobDefinitionId} started.", status, jobId, jobDefinitionId);
            logger.LogDebug("Cosmos write Job {Id} status -> PRIMARY (Session account default).", jobId);

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
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Update Job status to {Status} with id={Id} and JobDefinitionId={JobDefinitionId} finished.", status, jobId, jobDefinitionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for job {JobId} to {Status}", jobId, status);
            throw new DataAccessException($"Failed to update status for job {jobId} to {status}", ex);
        }
    }

    public async Task<IReadOnlyList<Job>> GetByJobDefinitionIdAsync(Guid jobDefinitionId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find Jobs by JobDefinitionId={JobDefinitionId} started.", jobDefinitionId);

            var query = new QueryDefinition("SELECT * FROM c WHERE c.JobDefinitionId = @id")
                .WithParameter("@id", jobDefinitionId.ToString());

            var requestOptions = new QueryRequestOptions { PartitionKey = new PartitionKey(jobDefinitionId.ToString()) };
            Container container;

            if (consistencyLevel == ConsistencyLevel.Eventual)
            {
                logger.LogDebug("Cosmos read Job -> REPLICA (consistency=Eventual).");
                requestOptions.ConsistencyLevel = CosmosConsistencyLevel.Eventual;
                container = _replicaContainer;
            }
            else
            {
                logger.LogDebug("Cosmos read Job -> PRIMARY (Session via same client — read-your-writes).");
                container = _primaryContainer;
            }

            var iterator = container.GetItemQueryIterator<JobDto>(query, requestOptions: requestOptions);
            var results = new List<Job>();

            while (iterator.HasMoreResults)
            {
                var page = await iterator.ReadNextAsync(cancellationToken).ConfigureAwait(false);
                results.AddRange(page.Select(JobMappings.ToModel));
            }

            logger.LogDebug("Find Jobs by JobDefinitionId={JobDefinitionId} finished.", jobDefinitionId);

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get jobs for job definition {JobDefinitionId}", jobDefinitionId);
            throw new DataAccessException($"Failed to get jobs for job definition {jobDefinitionId}", ex);
        }
    }

    public async Task<Job> GetByIdAsync(Guid id, Guid jobDefinitionId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find Job by id={Id} and JobDefinitionId={JobDefinitionId} started.", id, jobDefinitionId);

            ItemRequestOptions requestOptions = null;
            Container container;

            if (consistencyLevel == ConsistencyLevel.Eventual)
            {
                logger.LogDebug("Cosmos read Job -> REPLICA (consistency=Eventual).");
                requestOptions = new ItemRequestOptions { ConsistencyLevel = CosmosConsistencyLevel.Eventual };
                container = _replicaContainer;
            }
            else
            {
                logger.LogDebug("Cosmos read Job -> PRIMARY (Session via same client — read-your-writes).");
                container = _primaryContainer;
            }

            var response = await container.ReadItemAsync<JobDto>(
                id.ToString(),
                new PartitionKey(jobDefinitionId.ToString()),
                requestOptions,
                cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Find Job by id={Id} and JobDefinitionId={JobDefinitionId} finished.", id, jobDefinitionId);

            return response.Resource.ToModel();
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job {JobId}", id);
            throw new DataAccessException($"Failed to get job {id}", ex);
        }
    }

    public async Task DeleteAsync(Guid id, Guid jobDefinitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Delete Job by id={Id} and JobDefinitionId={JobDefinitionId} started.", id, jobDefinitionId);
            logger.LogDebug("Cosmos delete Job {Id} -> PRIMARY.", id);

            await _primaryContainer.DeleteItemAsync<JobDto>(
                id.ToString(),
                new PartitionKey(jobDefinitionId.ToString()),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            logger.LogDebug("Delete Job by id={Id} and JobDefinitionId={JobDefinitionId} finished.", id, jobDefinitionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job {JobId}", id);
            throw new DataAccessException($"Failed to delete job {id}", ex);
        }
    }
}
