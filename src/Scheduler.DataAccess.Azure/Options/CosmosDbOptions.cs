namespace Scheduler.DataAccess.Azure.Options;

public class CosmosDbOptions
{
    public const string Position = "CosmosDb";

    public string Endpoint { get; set; }
    public string Key { get; set; }
    public string PrimaryRegion { get; set; }
    public string SecondaryRegion { get; set; }
}
