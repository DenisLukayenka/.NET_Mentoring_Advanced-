using Riok.Mapperly.Abstractions;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;
using Scheduler.Shared.Models;

namespace Scheduler.BusinessLogic.Concrete.Mappings;

[Mapper]
internal static partial class JobMappings
{
    internal static partial GetJobResponse ToResponse(this Job model);
    internal static partial JobHistoryItem ToHistoryItem(this Job model);
}
