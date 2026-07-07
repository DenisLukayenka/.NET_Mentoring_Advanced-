namespace Scheduler.DataAccess.ConsoleApp;

public class SeedingOptions
{
    public const string Position = "Seeding";

    // Number of JobDefinition + JobDetail pairs to write to DocumentDB (Mongo).
    public int JobDefinitionCount { get; set; } = 25;

    // Jobs created per JobDefinition in Cosmos NoSQL — each becomes one logical partition value.
    public int JobsPerDefinition { get; set; } = 4;

    // JobOutput log entries created per Job in Cassandra — each Job is one partition value.
    public int OutputsPerJob { get; set; } = 2;

    // Distinct UserId values cycled across JobDefinitions, to show shard-key spread.
    public int UserPoolSize { get; set; } = 5;
}
