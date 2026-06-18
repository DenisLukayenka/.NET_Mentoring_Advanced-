namespace Scheduler.Shared.Models;

public class JobDefinition
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CronExpression { get; set; }
    public bool Concurrency { get; set; }
    public Guid UserId { get; set; }
    public Guid JobDetailId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public bool Active { get; set; }
    public DateTime NextExecutionDate { get; set; }
}
