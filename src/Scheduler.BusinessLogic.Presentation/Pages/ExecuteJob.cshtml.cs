using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

namespace Scheduler.BusinessLogic.Presentation.Pages;

public class ExecuteJobModel(
    IExecuteJobConsistencyDemoHandler demo)
    : PageModel
{
    public ExecuteJobConsistencyDemoResponse Result { get; private set; }

    public void OnGet() { }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        Result = await demo.HandleAsync(new ExecuteJobConsistencyDemoRequest(), cancellationToken);
    }
}
