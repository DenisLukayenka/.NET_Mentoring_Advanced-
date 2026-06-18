using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scheduler.DataAccess.Azure.Dtos;

internal class JobDefinitionDto
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string CronExpression { get; set; }
    public bool Concurrency { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid UserId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid JobDetailId { get; set; }

    public bool Active { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
    public DateTime NextExecutionDate { get; set; }
}
