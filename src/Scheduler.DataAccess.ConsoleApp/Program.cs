using Scheduler.DataAccess.Abstractions.Repositories;
using Scheduler.DataAccess.Azure;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.ConsoleApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((ctx, services) =>
                services.AddSchedulerDataAccess(ctx.Configuration))
            .Build();

        // UC 1.1 — Create a new job
        var jobDefinitionRepository = host.Services.GetRequiredService<IJobDefinitionRepository>();
        var jobDetailRepository = host.Services.GetRequiredService<IJobDetailRepository>();

        var jobDetail = new JobDetail
        {
            Id = Guid.NewGuid(),
            Type = "HttpCall",
            Payload = "{\"url\":\"https://example.com\"}",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };
        var jobDefinition = new JobDefinition
        {
            Id = Guid.NewGuid(),
            Name = "Daily report",
            Description = "Sends daily report via HTTP",
            CronExpression = "0 9 * * *",
            Concurrency = false,
            UserId = Guid.NewGuid(),
            JobDetailId = jobDetail.Id,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            Active = true,
            NextExecutionDate = DateTime.UtcNow.AddDays(1)
        };

        Console.WriteLine($"Creating job definition {jobDefinition.Id}...");
        await jobDefinitionRepository.CreateAsync(jobDefinition, jobDetail);
        Console.WriteLine("Written to DocumentDB primary (transaction).");

        var retrievedJobDefinition = await jobDefinitionRepository.GetByIdAsync(jobDefinition.Id);
        Console.WriteLine($"Read from DocumentDB replica: {retrievedJobDefinition?.Name ?? "not found"}");

        var retrievedJobDetail = await jobDetailRepository.GetByIdAsync(jobDetail.Id);
        Console.WriteLine($"Read job detail from DocumentDB replica: type={retrievedJobDetail?.Type ?? "not found"}");

        // UC 2.1 — Execute a job
        var jobRepository = host.Services.GetRequiredService<IJobRepository>();
        var jobOutputRepository = host.Services.GetRequiredService<IJobOutputRepository>();

        var job = new Job
        {
            Id = Guid.NewGuid(),
            JobDefinitionId = jobDefinition.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ScheduledAt = DateTime.UtcNow,
            Status = JobStatus.Pending
        };

        Console.WriteLine($"\nCreating job execution {job.Id}...");
        await jobRepository.CreateAsync(job);
        await jobRepository.UpdateStatusAsync(job.Id, job.JobDefinitionId, JobStatus.Running);
        Console.WriteLine("Job status: Running (written to Cosmos DB primary).");

        await jobOutputRepository.CreateAsync(new JobOutput { Id = Guid.NewGuid(), JobId = job.Id, Date = DateTime.UtcNow, Level = JobOutputLevel.Info, Message = "Job started." });
        await jobOutputRepository.CreateAsync(new JobOutput { Id = Guid.NewGuid(), JobId = job.Id, Date = DateTime.UtcNow, Level = JobOutputLevel.Info, Message = "Job completed." });
        Console.WriteLine("Job outputs written to Cassandra primary.");

        await jobRepository.UpdateStatusAsync(job.Id, job.JobDefinitionId, JobStatus.Succeeded);
        Console.WriteLine("Job status: Succeeded.");

        var jobOutputs = await jobOutputRepository.GetByJobIdAsync(job.Id);
        Console.WriteLine($"\nRead {jobOutputs.Count} job output(s) from Cassandra replica:");
        foreach (var jobOutput in jobOutputs)
            Console.WriteLine($"  [{jobOutput.Level}] {jobOutput.Message}");
    }
}
