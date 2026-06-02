namespace Scheduler.Shared.Models;

public class JobOutput
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public DateTime Date { get; set; }
    public JobOutputLevel Level { get; set; }
    public string Message { get; set; }
}
