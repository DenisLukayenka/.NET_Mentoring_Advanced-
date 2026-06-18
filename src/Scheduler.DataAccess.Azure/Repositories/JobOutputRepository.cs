using Cassandra;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Repositories;

public class JobOutputRepository(
    [FromKeyedServices(AzureConstants.CassandraPrimary)] ISession primarySession,
    [FromKeyedServices(AzureConstants.CassandraReplica)] ISession replicaSession,
    ILogger<JobOutputRepository> logger) : IJobOutputRepository
{
    public async Task CreateAsync(JobOutput output, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Create JobOutput with id={Id} in partition JobId={JobId}, clustering Date={Date} started.", output.Id, output.JobId, output.Date);

            var dto = output.ToDto();
            var statement = new SimpleStatement(
                "INSERT INTO scheduler.job_outputs (job_id, date, id, level, message) VALUES (?, ?, ?, ?, ?)",
                dto.JobId,
                dto.Date,
                dto.Id,
                dto.Level,
                dto.Message);
            await primarySession.ExecuteAsync(statement).ConfigureAwait(false);

            logger.LogDebug("Create JobOutput with id={Id} in partition JobId={JobId}, clustering Date={Date} finished.", output.Id, output.JobId, output.Date);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job output {OutputId} for job {JobId}", output.Id, output.JobId);
            throw new DataAccessException($"Failed to create job output {output.Id} for job {output.JobId}", ex);
        }
    }

    public async Task<IReadOnlyList<JobOutput>> GetByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find JobOutputs by partition JobId={JobId} started.", jobId);

            var statement = new SimpleStatement(
                "SELECT job_id, date, id, level, message FROM scheduler.job_outputs WHERE job_id = ?",
                jobId);

            var rowSet = await replicaSession.ExecuteAsync(statement).ConfigureAwait(false);
            var result = rowSet.Select(MapRow).ToList();

            logger.LogDebug("Find JobOutputs by partition JobId={JobId} finished.", jobId);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job outputs for job {JobId}", jobId);
            throw new DataAccessException($"Failed to get job outputs for job {jobId}", ex);
        }
    }

    public async Task<IReadOnlyList<JobOutput>> GetByJobIdAndDateRangeAsync(Guid jobId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find JobOutputs by partition JobId={JobId}, clustering Date between {From} and {To} started.", jobId, from, to);

            var statement = new SimpleStatement(
                "SELECT job_id, date, id, level, message FROM scheduler.job_outputs WHERE job_id = ? AND date >= ? AND date <= ?",
                jobId,
                from,
                to);

            var rowSet = await replicaSession.ExecuteAsync(statement).ConfigureAwait(false);
            var result = rowSet.Select(MapRow).ToList();

            logger.LogDebug("Find JobOutputs by partition JobId={JobId}, clustering Date between {From} and {To} finished.", jobId, from, to);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job outputs for job {JobId} between {From} and {To}", jobId, from, to);
            throw new DataAccessException($"Failed to get job outputs for job {jobId} between {from} and {to}", ex);
        }
    }

    public async Task DeleteByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Delete JobOutputs by partition JobId={JobId} started.", jobId);

            var statement = new SimpleStatement(
                "DELETE FROM scheduler.job_outputs WHERE job_id = ?",
                jobId);

            await primarySession.ExecuteAsync(statement).ConfigureAwait(false);

            logger.LogDebug("Delete JobOutputs by partition JobId={JobId} finished.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job outputs for job {JobId}", jobId);
            throw new DataAccessException($"Failed to delete job outputs for job {jobId}", ex);
        }
    }

    private static JobOutput MapRow(Row row) => new JobOutputDto
    {
        JobId = row.GetValue<Guid>("job_id"),
        Date = row.GetValue<DateTimeOffset>("date").UtcDateTime,
        Id = row.GetValue<Guid>("id"),
        Level = row.GetValue<string>("level"),
        Message = row.GetValue<string>("message")
    }.ToModel();
}
