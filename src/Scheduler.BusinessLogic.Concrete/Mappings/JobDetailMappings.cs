using Riok.Mapperly.Abstractions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDetails;
using Scheduler.Shared.Models;

namespace Scheduler.BusinessLogic.Concrete.Mappings;

[Mapper]
internal static partial class JobDetailMappings
{
    internal static partial GetJobDetailResponse ToResponse(this JobDetail model);
}
