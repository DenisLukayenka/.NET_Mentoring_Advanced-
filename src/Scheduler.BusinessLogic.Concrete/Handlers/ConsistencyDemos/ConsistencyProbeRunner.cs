using System.Diagnostics;
using Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

namespace Scheduler.BusinessLogic.Concrete.Handlers.ConsistencyDemos;

internal static class ConsistencyProbeRunner
{
    public static async Task<ReplicaProbe> ProbeAsync<T>(
        string step,
        string database,
        string field,
        Func<ConsistencyLevel, Task<T>> read,
        Func<T, string> project)
    {
        var primaryTask = TimeAsync(() => read(ConsistencyLevel.Strong), project);
        var replicaTask = TimeAsync(() => read(ConsistencyLevel.Eventual), project);

        await Task.WhenAll(primaryTask, replicaTask).ConfigureAwait(false);

        var (primaryHas, primaryValue, primaryMs) = primaryTask.GetAwaiter().GetResult();
        var (replicaHas, replicaValue, replicaMs) = replicaTask.GetAwaiter().GetResult();

        return ReplicaProbe.From(
            step, database, field,
            primaryHas, primaryValue, primaryMs,
            replicaHas, replicaValue, replicaMs);
    }

    private static async Task<(bool has, string value, long ms)> TimeAsync<T>(
        Func<Task<T>> read,
        Func<T, string> project)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await read().ConfigureAwait(false);
        stopwatch.Stop();

        var value = project(result);
        return (value != null, value ?? "<none>", stopwatch.ElapsedMilliseconds);
    }
}
