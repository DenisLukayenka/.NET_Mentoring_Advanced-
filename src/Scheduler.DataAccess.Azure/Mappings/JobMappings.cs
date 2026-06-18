using Riok.Mapperly.Abstractions;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Mappings;

[Mapper]
internal static partial class JobMappings
{
    internal static partial Job ToModel(this JobDto dto);
    internal static partial JobDto ToDto(this Job model);
}
