namespace Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

public record ExecuteJobConsistencyDemoRequest();

public record ExecuteJobConsistencyDemoResponse(
    Guid JobDefinitionId,
    Guid JobDetailId,
    Guid JobId,
    IReadOnlyList<ReplicaProbe> Probes,
    bool ClaimWon,
    bool DuplicateBackstopHit,
    JobStatus FinalStatus);
