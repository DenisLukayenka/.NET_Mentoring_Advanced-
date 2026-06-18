namespace Scheduler.DataAccess.Azure.Options;

public class CassandraOptions
{
    public const string Position = "Cassandra";

    public string ContactPoint { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string PrimaryRegion { get; set; }
    public string SecondaryRegion { get; set; }
}
