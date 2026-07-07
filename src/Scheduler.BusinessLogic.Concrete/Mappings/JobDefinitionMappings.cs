using Riok.Mapperly.Abstractions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;
using Scheduler.Shared.Models;

namespace Scheduler.BusinessLogic.Concrete.Mappings;

[Mapper]
internal static partial class JobDefinitionMappings
{
    internal static partial GetJobDefinitionResponse ToResponse(this JobDefinition model);
    internal static partial DueJobDefinition ToDueDefinition(this JobDefinition model);
}
