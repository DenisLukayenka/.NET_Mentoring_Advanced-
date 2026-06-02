using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Scheduler.DataAccess.Azure.Dtos;

internal class JobDetailDto
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Payload { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
