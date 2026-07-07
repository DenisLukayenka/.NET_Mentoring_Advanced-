using Cassandra;
using Scheduler.DataAccess.Abstractions.Exceptions;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.DataAccess.Azure.Mappings;
using Scheduler.Shared.Models;
using CassandraConsistencyLevel = Cassandra.ConsistencyLevel;
using ConsistencyLevel = Scheduler.DataAccess.Abstractions.Consistency.ConsistencyLevel;

namespace Scheduler.DataAccess.Azure.Repositories;

internal class JobOutputRepository(
    [FromKeyedServices(AzureConstants.CassandraPrimary)] ISession primarySession,
    [FromKeyedServices(AzureConstants.CassandraReplica)] ISession replicaSession,
    ILogger<JobOutputRepository> logger) : IJobOutputRepository
{
    public async Task CreateAsync(JobOutput output, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Create JobOutput with id={Id} in partition JobId={JobId}, clustering Date={Date} started.", output.Id, output.JobId, output.Date);
            logger.LogDebug("Cassandra write JobOutput {Id} -> PRIMARY (consistency={Level}).", output.Id, consistencyLevel == ConsistencyLevel.Eventual ? "LocalOne" : "LocalQuorum");

            var dto = output.ToDto();
            var statement = new SimpleStatement(
                "INSERT INTO scheduler.job_outputs (job_id, date, id, level, message) VALUES (?, ?, ?, ?, ?)",
                dto.JobId, dto.Date, dto.Id, dto.Level, dto.Message);
            statement.SetConsistencyLevel(
                consistencyLevel == ConsistencyLevel.Eventual
                    ? CassandraConsistencyLevel.LocalOne
                    : CassandraConsistencyLevel.LocalQuorum);

            await primarySession.ExecuteAsync(statement).ConfigureAwait(false);

            logger.LogDebug("Create JobOutput with id={Id} in partition JobId={JobId}, clustering Date={Date} finished.", output.Id, output.JobId, output.Date);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create job output {OutputId} for job {JobId}", output.Id, output.JobId);
            throw new DataAccessException($"Failed to create job output {output.Id} for job {output.JobId}", ex);
        }
    }

    public async Task<IReadOnlyList<JobOutput>> GetByJobIdAsync(Guid jobId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find JobOutputs by partition JobId={JobId} started.", jobId);

            var statement = new SimpleStatement(
                "SELECT job_id, date, id, level, message FROM scheduler.job_outputs WHERE job_id = ?",
                jobId);
            var (session, level) = ResolveReadConsistency(consistencyLevel);
            logger.LogDebug("Cassandra read JobOutputs job={JobId} -> {Node} (consistency={Level}).", jobId, consistencyLevel == ConsistencyLevel.Eventual ? "REPLICA" : "PRIMARY", level);
            statement.SetConsistencyLevel(level);

            var rowSet = await session.ExecuteAsync(statement).ConfigureAwait(false);
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

    public async Task<IReadOnlyList<JobOutput>> GetByJobIdAndDateRangeAsync(Guid jobId, DateTime from, DateTime to, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Find JobOutputs by partition JobId={JobId}, clustering Date between {From} and {To} started.", jobId, from, to);

            var statement = new SimpleStatement(
                "SELECT job_id, date, id, level, message FROM scheduler.job_outputs WHERE job_id = ? AND date >= ? AND date <= ?",
                jobId, from, to);
            var (session, level) = ResolveReadConsistency(consistencyLevel);
            logger.LogDebug("Cassandra read JobOutputs job={JobId} -> {Node} (consistency={Level}).", jobId, consistencyLevel == ConsistencyLevel.Eventual ? "REPLICA" : "PRIMARY", level);
            statement.SetConsistencyLevel(level);

            var rowSet = await session.ExecuteAsync(statement).ConfigureAwait(false);
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

    public async Task DeleteByJobIdAsync(Guid jobId, ConsistencyLevel consistencyLevel, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Delete JobOutputs by partition JobId={JobId} started.", jobId);
            logger.LogDebug("Cassandra delete JobOutputs job={JobId} -> PRIMARY.", jobId);

            var statement = new SimpleStatement(
                "DELETE FROM scheduler.job_outputs WHERE job_id = ?",
                jobId);
            statement.SetConsistencyLevel(
                consistencyLevel == ConsistencyLevel.Eventual
                    ? CassandraConsistencyLevel.LocalOne
                    : CassandraConsistencyLevel.LocalQuorum);

            await primarySession.ExecuteAsync(statement).ConfigureAwait(false);

            logger.LogDebug("Delete JobOutputs by partition JobId={JobId} finished.", jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete job outputs for job {JobId}", jobId);
            throw new DataAccessException($"Failed to delete job outputs for job {jobId}", ex);
        }
    }

    private (ISession session, CassandraConsistencyLevel level) ResolveReadConsistency(ConsistencyLevel consistencyLevel) =>
        consistencyLevel == ConsistencyLevel.Eventual
            ? (replicaSession, CassandraConsistencyLevel.LocalOne)
            : (primarySession, CassandraConsistencyLevel.LocalQuorum);

    private static JobOutput MapRow(Row row) => new JobOutputDto
    {
        JobId = row.GetValue<Guid>("job_id"),
        Date = row.GetValue<DateTimeOffset>("date").UtcDateTime,
        Id = row.GetValue<Guid>("id"),
        Level = row.GetValue<string>("level"),
        Message = row.GetValue<string>("message")
    }.ToModel();
}
