namespace Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

public record ReplicaProbe(
    string Step,
    string Database,
    string Field,
    bool PrimaryHasValue,
    string PrimaryValue,
    long PrimaryMs,
    bool ReplicaHasValue,
    string ReplicaValue,
    long ReplicaMs,
    bool Match)
{
    public static ReplicaProbe From(
        string step, string database, string field,
        bool primaryHasValue, string primaryValue, long primaryMs,
        bool replicaHasValue, string replicaValue, long replicaMs)
        => new(step, database, field,
            primaryHasValue, primaryValue, primaryMs,
            replicaHasValue, replicaValue, replicaMs,
            Match: primaryHasValue == replicaHasValue && primaryValue == replicaValue);
}
