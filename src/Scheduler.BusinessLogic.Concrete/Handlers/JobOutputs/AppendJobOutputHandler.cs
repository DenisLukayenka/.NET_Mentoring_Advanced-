using Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;

namespace Scheduler.BusinessLogic.Concrete.Handlers.JobOutputs;

public class AppendJobOutputHandler(
    IJobOutputRepository repository,
    ILogger<AppendJobOutputHandler> logger)
    : IAppendJobOutputHandler
{
    public async Task HandleAsync(AppendJobOutputRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("UC2.1 AppendJobOutput job={JobId} -> Eventual (LOCAL_ONE; append-only immutable log).", request.JobId);

        var output = new JobOutput
        {
            Id = Guid.NewGuid(),
            JobId = request.JobId,
            Date = request.Date,
            Level = request.Level,
            Message = request.Message
        };

        await repository
            .CreateAsync(output, ConsistencyLevel.Eventual, cancellationToken)
            .ConfigureAwait(false);
    }
}
