using Riok.Mapperly.Abstractions;
using Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;
using Scheduler.Shared.Models;

namespace Scheduler.BusinessLogic.Concrete.Mappings;

[Mapper]
internal static partial class JobOutputMappings
{
    internal static partial JobOutputItem ToItem(this JobOutput model);
}
