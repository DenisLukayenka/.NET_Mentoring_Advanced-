namespace Scheduler.DataAccess.Azure.Options;

public class DocumentDbOptions
{
    public const string Position = "DocumentDb";

    public string PrimaryConnectionString { get; set; }
    public string ReplicaConnectionString { get; set; }
}
