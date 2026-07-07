using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;
using Scheduler.DataAccess.Abstractions.Exceptions;

namespace Scheduler.BusinessLogic.Concrete.Handlers.ConsistencyDemos;

public class ExecuteJobConsistencyDemoHandler(
    IJobDefinitionRepository definitions,
    IJobRepository jobs,
    IJobOutputRepository outputs,
    ILogger<ExecuteJobConsistencyDemoHandler> logger)
    : IExecuteJobConsistencyDemoHandler
{
    public async Task<ExecuteJobConsistencyDemoResponse> HandleAsync(
        ExecuteJobConsistencyDemoRequest request, CancellationToken cancellationToken = default)
    {
        // Run the full scheduling-window flow many times concurrently. Hammering the primary with
        // simultaneous writes and then immediately reading the replica makes replica lag (the
        // eventual-consistency hazard this demo exists to show) far more likely to surface on at
        // least one run, where a single sequential run almost never catches it.
        var runCount = Math.Clamp(request.RunCount, 1, 100);

        var runs = Enumerable.Range(0, runCount)
            .Select(index => RunOnceAsync(index, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(runs).ConfigureAwait(false);

        // Task.WhenAll preserves input order, but order explicitly by RunIndex to be safe.
        return new ExecuteJobConsistencyDemoResponse(
            results.OrderBy(r => r.RunIndex).ToArray());
    }

    private async Task<ExecuteJobConsistencyDemoRunResult> RunOnceAsync(
        int runIndex, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var probes = new List<ReplicaProbe>();

        // 1. Create the definition + detail, due now.
        var detail = new JobDetail
        {
            Id = Guid.NewGuid(),
            Type = "HttpCall",
            Payload = "{ \"url\": \"https://example.com\" }",
            CreatedDate = now,
            UpdatedDate = now
        };
        var definition = new JobDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Execute demo job",
            Description = "Consistency demo (UC 2.1)",
            CronExpression = "* * * * *",
            Concurrency = true,
            UserId = Guid.NewGuid(),
            JobDetailId = detail.Id,
            CreatedDate = now,
            UpdatedDate = now,
            Active = true,
            NextExecutionDate = now
        };
        await definitions.CreateAsync(definition, detail, cancellationToken).ConfigureAwait(false);
        logger.LogDebug("UC2.1 demo run {Run}: seeded definition {Id} due now.", runIndex, definition.Id);

        probes.Add(await ConsistencyProbeRunner.ProbeAsync(
            "1 · after create — find JobDefinition", "DocumentDB", "_id",
            level => definitions.GetByIdAsync(definition.Id, level, cancellationToken),
            d => d?.Id.ToString()).ConfigureAwait(false));

        // 2. Claim the slot (atomic findOneAndUpdate, w:majority): update NextExecutionDate.
        var newNextExecution = now.AddMinutes(1);
        var claimWon = await definitions
            .TryClaimNextExecutionAsync(definition.Id, now, newNextExecution, cancellationToken)
            .ConfigureAwait(false);
        logger.LogDebug("UC2.1 demo run {Run}: claim for {Id} -> {Won}.", runIndex, definition.Id, claimWon);

        probes.Add(await ConsistencyProbeRunner.ProbeAsync(
            "2 · after claim — NextExecutionDate", "DocumentDB", "NextExecutionDate",
            level => definitions.GetByIdAsync(definition.Id, level, cancellationToken),
            d => d?.NextExecutionDate.ToString("O")).ConfigureAwait(false));

        // 3. Insert run row (write-time unique key) + status transitions.
        var job = new Job
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = definition.Id,
            CreatedAt = now,
            UpdatedAt = now,
            ScheduledAt = now,
            Status = JobStatus.Pending
        };
        var duplicateBackstopHit = false;
        try
        {
            await jobs.CreateAsync(job, cancellationToken).ConfigureAwait(false);
        }
        catch (DuplicateJobRunException)
        {
            duplicateBackstopHit = true;
            logger.LogDebug("UC2.1 demo run {Run}: duplicate backstop fired for def {Id}.", runIndex, definition.Id);
        }

        if (!duplicateBackstopHit)
        {
            await jobs.UpdateStatusAsync(job.Id, definition.Id, JobStatus.Running, null, cancellationToken).ConfigureAwait(false);
            await jobs.UpdateStatusAsync(job.Id, definition.Id, JobStatus.Succeeded, null, cancellationToken).ConfigureAwait(false);
            probes.Add(await ConsistencyProbeRunner.ProbeAsync(
                "3 · after status — Job.Status", "Cosmos NoSQL", "Status",
                level => jobs.GetByIdAsync(job.Id, definition.Id, level, cancellationToken),
                j => j?.Status.ToString()).ConfigureAwait(false));
        }

        // 4. Append output lines (Eventual / LOCAL_ONE) + read the count.
        await outputs.CreateAsync(new JobOutput
        {
            Id = Guid.NewGuid(), JobId = job.Id, Date = now,
            Level = JobOutputLevel.Info, Message = "Job started."
        }, ConsistencyLevel.Eventual, cancellationToken).ConfigureAwait(false);
        await outputs.CreateAsync(new JobOutput
        {
            Id = Guid.NewGuid(), JobId = job.Id, Date = now.AddSeconds(1),
            Level = JobOutputLevel.Info, Message = "Job finished."
        }, ConsistencyLevel.Eventual, cancellationToken).ConfigureAwait(false);

        probes.Add(await ConsistencyProbeRunner.ProbeAsync(
            "4 · after append — JobOutput lines", "Cassandra", "lines",
            level => outputs.GetByJobIdAsync(job.Id, level, cancellationToken),
            list => list != null && list.Count > 0 ? list.Count.ToString() : null).ConfigureAwait(false));

        var finalStatus = duplicateBackstopHit ? JobStatus.Pending : JobStatus.Succeeded;

        return new ExecuteJobConsistencyDemoRunResult(
            runIndex, definition.Id, detail.Id, job.Id, probes, claimWon, duplicateBackstopHit, finalStatus);
    }
}
