namespace Scheduler.Shared.Models;

public class Job
{
    public Guid Id { get; set; }
    public Guid JobDefinitionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ScheduledAt { get; set; }
    public JobStatus Status { get; set; }
    public string ErrorMessage { get; set; }
}
