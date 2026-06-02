using Riok.Mapperly.Abstractions;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Mappings;

[Mapper]
internal static partial class JobDetailMappings
{
    internal static partial JobDetail ToModel(this JobDetailDto dto);
    internal static partial JobDetailDto ToDto(this JobDetail model);
}
