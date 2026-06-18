using Riok.Mapperly.Abstractions;
using Scheduler.DataAccess.Azure.Dtos;
using Scheduler.Shared.Models;

namespace Scheduler.DataAccess.Azure.Mappings;

[Mapper]
internal static partial class JobDefinitionMappings
{
    internal static partial JobDefinition ToModel(this JobDefinitionDto dto);
    internal static partial JobDefinitionDto ToDto(this JobDefinition model);
}
