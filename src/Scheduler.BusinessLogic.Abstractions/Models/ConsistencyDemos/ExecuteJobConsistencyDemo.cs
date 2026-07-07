using System.Linq;

namespace Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

public record ExecuteJobConsistencyDemoRequest(int RunCount = 100);

public record ExecuteJobConsistencyDemoRunResult(
    int RunIndex,
    Guid JobDefinitionId,
    Guid JobDetailId,
    Guid JobId,
    IReadOnlyList<ReplicaProbe> Probes,
    bool ClaimWon,
    bool DuplicateBackstopHit,
    JobStatus FinalStatus)
{
    // True if any probe in this run caught the replica disagreeing with the primary.
    public bool HasStaleProbe => Probes.Any(p => !p.Match);
}

public record ExecuteJobConsistencyDemoResponse(
    IReadOnlyList<ExecuteJobConsistencyDemoRunResult> Runs)
{
    public int RunCount => Runs.Count;

    // How many of the concurrent runs caught at least one stale replica read.
    public int StaleRunCount => Runs.Count(r => r.HasStaleProbe);

    // Total number of stale probes across every run.
    public int StaleProbeCount => Runs.Sum(r => r.Probes.Count(p => !p.Match));
}
