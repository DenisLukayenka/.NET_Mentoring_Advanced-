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
            var dto = output.ToDto();
            var statement = new SimpleStatement(
                "INSERT INTO scheduler.job_outputs (job_id, date, id, level, message) VALUES (?, ?, ?, ?, ?)",
                dto.JobId,
                dto.Date,
                dto.Id,
                dto.Level,
                dto.Message);
            await primarySession.ExecuteAsync(statement);
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
            var statement = new SimpleStatement(
                "SELECT job_id, date, id, level, message FROM scheduler.job_outputs WHERE job_id = ?",
                jobId);

            var rowSet = await replicaSession.ExecuteAsync(statement);

            return rowSet.Select(row => new JobOutputDto
            {
                JobId = row.GetValue<Guid>("job_id"),
                Date = row.GetValue<DateTimeOffset>("date").UtcDateTime,
                Id = row.GetValue<Guid>("id"),
                Level = row.GetValue<string>("level"),
                Message = row.GetValue<string>("message")
            }.ToModel()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get job outputs for job {JobId}", jobId);
            throw new DataAccessException($"Failed to get job outputs for job {jobId}", ex);
        }
    }
}
