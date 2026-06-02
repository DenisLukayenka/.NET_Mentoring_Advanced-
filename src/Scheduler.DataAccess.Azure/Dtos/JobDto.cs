using Newtonsoft.Json;

namespace Scheduler.DataAccess.Azure.Dtos;

internal class JobDto
{
    [JsonProperty("id")]
    public string Id { get; set; }
    public string JobDefinitionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; }
    public string ErrorMessage { get; set; }
}
