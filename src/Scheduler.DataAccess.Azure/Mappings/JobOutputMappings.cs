using Riok.Mapperly.Abstractions;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Mappings;

[Mapper]
internal static partial class JobOutputMappings
{
    internal static partial JobOutput ToModel(this JobOutputDto dto);
    internal static partial JobOutputDto ToDto(this JobOutput model);

    // Cassandra stores level as lowercase; keep that convention round-trip.
    private static string MapLevel(JobOutputLevel level) => level.ToString().ToLowerInvariant();
    private static JobOutputLevel MapLevel(string level) => Enum.Parse<JobOutputLevel>(level, ignoreCase: true);
}
