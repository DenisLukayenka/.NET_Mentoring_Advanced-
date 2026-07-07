using Scheduler.BusinessLogic.Abstractions.Handlers.Jobs;
using Scheduler.BusinessLogic.Abstractions.Models.Jobs;

namespace Scheduler.BusinessLogic.Presentation.Pages;

public class JobRunModel(
    ICreateJobRunHandler create,
    IGetJobHandler getJob,
    IGetJobHistoryHandler getHistory)
    : PageModel
{
    public CreateJobRunResponse Created { get; private set; }
    public GetJobResponse Job { get; private set; }
    public GetJobHistoryResponse History { get; private set; }

    public void OnGet() { }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();
        var jobDefinitionId = Guid.NewGuid();
        var scheduledAt = DateTime.UtcNow;

        Created = await create.HandleAsync(
            new CreateJobRunRequest(userId, jobDefinitionId, scheduledAt), cancellationToken);

        Job = await getJob.HandleAsync(
            new GetJobRequest(userId, Created.JobId, jobDefinitionId), cancellationToken);

        History = await getHistory.HandleAsync(
            new GetJobHistoryRequest(jobDefinitionId), cancellationToken);
    }
}
