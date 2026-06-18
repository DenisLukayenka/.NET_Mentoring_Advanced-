using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.ConsoleApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
#if DEBUG
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", Environments.Development);
#endif

        var host = Host.CreateDefaultBuilder(args)
            .UseEnvironment(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environments.Production)
            .ConfigureServices((ctx, services) =>
                services.AddSchedulerDataAccess(ctx.Configuration))
            .Build();

        var configuration = host.Services.GetRequiredService<IConfiguration>();
        var seeding = configuration.GetSection(SeedingOptions.Position).Get<SeedingOptions>() ?? new SeedingOptions();

        var jobDefinitionRepository = host.Services.GetRequiredService<IJobDefinitionRepository>();
        var jobRepository = host.Services.GetRequiredService<IJobRepository>();
        var jobOutputRepository = host.Services.GetRequiredService<IJobOutputRepository>();

        Console.WriteLine("Seeding configuration:");
        Console.WriteLine($"JobDefinitionCount = {seeding.JobDefinitionCount}");
        Console.WriteLine($"JobsPerDefinition  = {seeding.JobsPerDefinition}");
        Console.WriteLine($"OutputsPerJob      = {seeding.OutputsPerJob}");
        Console.WriteLine($"UserPoolSize       = {seeding.UserPoolSize}");

        var userPool = Enumerable.Range(0, Math.Max(1, seeding.UserPoolSize))
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var baseTime = DateTime.UtcNow;

        // DocumentDB: JobDefinition + JobDetail pairs ─────────────────
        var definitionIds = new List<Guid>(seeding.JobDefinitionCount);
        var definitionUsers = new Dictionary<Guid, Guid>(seeding.JobDefinitionCount);

        Console.WriteLine($"[1/3] Writing {seeding.JobDefinitionCount} JobDefinition + JobDetail pairs to DocumentDB.");
        for (var i = 0; i < seeding.JobDefinitionCount; i++)
        {
            var jobDetail = new JobDetail
            {
                Id = Guid.NewGuid(),
                Type = "HttpCall",
                Payload = @"{'url': 'https://google.com'}",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            var userId = userPool[i % userPool.Length];
            var jobDefinition = new JobDefinition
            {
                Id = Guid.NewGuid(),
                Name = $"Job definition #{i:D3}",
                Description = $"Seeded definition {i} for partition distribution demo",
                CronExpression = $"{i % 60} */{(i % 12) + 1} * * *",
                Concurrency = true,
                UserId = userId,
                JobDetailId = jobDetail.Id,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Active = true,
                NextExecutionDate = baseTime.AddMinutes(i)
            };

            Console.WriteLine($"Creating job definition {jobDefinition.Id}...");
            await jobDefinitionRepository.CreateAsync(jobDefinition, jobDetail);
            Console.WriteLine("Written to DocumentDB primary (transaction).");

            definitionIds.Add(jobDefinition.Id);
            definitionUsers[jobDefinition.Id] = userId;
        }

        // ── Cosmos NoSQL (Jobs): partitioned by JobDefinitionId ─────────────────
        var jobsByDefinition = new Dictionary<Guid, List<Guid>>();
        var jobIds = new List<Guid>();
        var scheduleOffset = 0;

        Console.WriteLine($"[2/3] Writing {seeding.JobsPerDefinition} Jobs per definition to Cosmos NoSQL.");
        foreach (var definitionId in definitionIds)
        {
            var perDefinition = new List<Guid>(seeding.JobsPerDefinition);
            for (var j = 0; j < seeding.JobsPerDefinition; j++)
            {
                var job = new Job
                {
                    Id = Guid.NewGuid(),
                    JobDefinitionId = definitionId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    // Distinct ScheduledAt keeps the unique (JobDefinitionId, ScheduledAt) key satisfied.
                    ScheduledAt = baseTime.AddMinutes(scheduleOffset++),
                    Status = JobStatus.Pending
                };

                Console.WriteLine($"\nCreating job execution {job.Id}...");
                await jobRepository.CreateAsync(job);
                await jobRepository.UpdateStatusAsync(job.Id, job.JobDefinitionId, JobStatus.Running);
                Console.WriteLine("Job status: Running (written to Cosmos DB primary).");

                perDefinition.Add(job.Id);
                jobIds.Add(job.Id);
            }
            jobsByDefinition[definitionId] = perDefinition;
        }

        // ── Cosmos Cassandra (JobOutputs): partitioned by JobId, clustered by Date ──
        var outputsByJob = new Dictionary<Guid, int>();

        Console.WriteLine($"[3/3] Writing {seeding.OutputsPerJob} JobOutputs per job to Cassandra.");
        foreach (var jobId in jobIds)
        {
            for (var k = 0; k < seeding.OutputsPerJob; k++)
            {
                var output = new JobOutput
                {
                    Id = Guid.NewGuid(),
                    JobId = jobId,
                    Date = baseTime.AddSeconds(k),
                    Level = (JobOutputLevel)(k % 4),
                    Message = k == 0 ? "Job started." : $"Step {k} completed."
                };
                await jobOutputRepository.CreateAsync(output, ConsistencyLevel.Eventual);
            }
            outputsByJob[jobId] = seeding.OutputsPerJob;
        }

        // ── Distribution summary ────────────────────────────────────────────────
        Console.WriteLine("\n===== Distribution summary =====");
        Console.WriteLine($"DocumentDB  : {definitionIds.Count} JobDefinitions across {definitionUsers.Values.Distinct().Count()} distinct UserId values");
        Console.WriteLine($"Cosmos NoSQL: {jobIds.Count} Jobs across {jobsByDefinition.Count} JobDefinitionId partitions");

        foreach (var kvp in jobsByDefinition.Take(5))
            Console.WriteLine($"    partition JobDefinitionId={kvp.Key} --- {kvp.Value.Count} jobs");

        if (jobsByDefinition.Count > 5)
            Console.WriteLine($"    and {jobsByDefinition.Count - 5} more partitions");

        Console.WriteLine($"Cassandra   : {outputsByJob.Values.Sum()} JobOutputs across {outputsByJob.Count} JobId partitions");

        foreach (var kvp in outputsByJob.Take(5))
            Console.WriteLine($"    partition JobId={kvp.Key} -> {kvp.Value} outputs");

        if (outputsByJob.Count > 5)
            Console.WriteLine($"    ... and {outputsByJob.Count - 5} more partitions");

        // ── Read-back proof: partition-scoped reads return one partition's rows ──
        if (definitionIds.Count > 0)
        {
            var sampleDefinitionId = definitionIds[0];

            // Eventual: hits replica.
            var jobsResult = await jobRepository.GetByJobDefinitionIdAsync(sampleDefinitionId, ConsistencyLevel.Eventual);
            Console.WriteLine($"Read-back (Cosmos replica / Eventual): JobDefinitionId={sampleDefinitionId} returned {jobsResult.Count} jobs.");

            // Strong: write a status transition, then read it back from the primary so the read is
            // guaranteed to see the update.
            if (jobIds.Count > 0)
            {
                var sampleJobId = jobIds[0];
                await jobRepository.UpdateStatusAsync(
                    sampleJobId, sampleDefinitionId, JobStatus.Succeeded, null);

                var job = await jobRepository.GetByIdAsync(sampleJobId, sampleDefinitionId, ConsistencyLevel.Strong);
                Console.WriteLine($"Read-back (Cosmos primary / Strong): job {job?.Id} status={job?.Status} (sees own write).");
            }
        }

        if (jobIds.Count > 0)
        {
            var sampleJobId = jobIds[0];
            var outputsResult = await jobOutputRepository.GetByJobIdAsync(sampleJobId, ConsistencyLevel.Eventual);
            Console.WriteLine($"Read-back (Cassandra replica): JobId={sampleJobId} returned {outputsResult.Count} outputs from one partition.");

            // Clustering-key range read: same partition (JobId), bounded by the Date clustering key.
            var rangeStart = baseTime;
            var rangeEnd = baseTime.AddSeconds(Math.Max(1, seeding.OutputsPerJob));
            var rangeResult = await jobOutputRepository.GetByJobIdAndDateRangeAsync(sampleJobId, rangeStart, rangeEnd, ConsistencyLevel.Eventual);
            Console.WriteLine($"Read-back (Cassandra replica, range): JobId={sampleJobId} Date in [{rangeStart:O}, {rangeEnd:O}] returned {rangeResult.Count} outputs from one partition.");
        }

        Console.WriteLine("\nSeeding complete.");
    }
}
