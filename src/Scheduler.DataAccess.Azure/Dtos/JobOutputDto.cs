namespace Scheduler.DataAccess.Azure.Dtos;

internal class JobOutputDto
{
    public Guid JobId { get; set; }
    public DateTime Date { get; set; }
    public Guid Id { get; set; }
    public string Level { get; set; }
    public string Message { get; set; }
}
