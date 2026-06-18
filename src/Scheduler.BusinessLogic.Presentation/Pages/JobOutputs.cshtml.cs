using Scheduler.BusinessLogic.Abstractions.Handlers.JobOutputs;
using Scheduler.BusinessLogic.Abstractions.Models.JobOutputs;
using Scheduler.Shared.Models;

namespace Scheduler.BusinessLogic.Presentation.Pages;

public class JobOutputsModel(
    IAppendJobOutputHandler append,
    IGetJobOutputsHandler getOutputs)
    : PageModel
{
    [BindProperty] public string Message { get; set; } = "Job started.";

    public Guid JobId { get; private set; }
    public int AppendedCount { get; private set; }
    public GetJobOutputsResponse Outputs { get; private set; }

    public void OnGet() { }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        JobId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        await append.HandleAsync(
            new AppendJobOutputRequest(JobId, JobOutputLevel.Info, Message, now), cancellationToken);
        await append.HandleAsync(
            new AppendJobOutputRequest(JobId, JobOutputLevel.Info, "Job finished.", now.AddSeconds(1)), cancellationToken);
        AppendedCount = 2;

        Outputs = await getOutputs.HandleAsync(
            new GetJobOutputsRequest(JobId), cancellationToken);
    }
}
