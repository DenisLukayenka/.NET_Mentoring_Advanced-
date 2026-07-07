using Scheduler.BusinessLogic.Abstractions.Handlers.JobDetails;
using Scheduler.BusinessLogic.Abstractions.Models.JobDetails;
using Scheduler.BusinessLogic.Concrete.Mappings;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobDetails;

public class GetJobDetailHandler(
    IJobDetailRepository repository,
    ILogger<GetJobDetailHandler> logger)
    : IGetJobDetailHandler
{
    public async Task<GetJobDetailResponse> HandleAsync(GetJobDetailRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 GetJobDetail {Id} -> Eventual (replica with primary fallback on miss).", request.JobDetailId);

        var detail = await repository
            .GetByIdAsync(request.JobDetailId, ConsistencyLevel.Eventual, cancellationToken)
            .ConfigureAwait(false);

        return detail?.ToResponse();
    }
}
