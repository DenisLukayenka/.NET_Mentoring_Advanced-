namespace Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

public record CreateJobConsistencyDemoRequest(string Name, string CronExpression, string Payload);

public record CreateJobConsistencyDemoResponse(
    Guid JobDefinitionId,
    Guid JobDetailId,
    bool TransactionCommitted,
    ReplicaProbe DefinitionReadBack,
    ReplicaProbe DetailReadBack,
    bool ReadYourWritesHeld);
