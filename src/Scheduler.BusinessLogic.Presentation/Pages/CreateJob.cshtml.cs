using Scheduler.BusinessLogic.Abstractions.Handlers.ConsistencyDemos;
using Scheduler.BusinessLogic.Abstractions.Models.ConsistencyDemos;

namespace Scheduler.BusinessLogic.Presentation.Pages;

public class CreateJobModel(
    ICreateJobConsistencyDemoHandler demo)
    : PageModel
{
    [BindProperty] public string Name { get; set; } = "Nightly report";
    [BindProperty] public string CronExpression { get; set; } = "0 0 * * *";
    [BindProperty] public string Payload { get; set; } = "{ \"url\": \"https://example.com\" }";

    public CreateJobConsistencyDemoResponse Result { get; private set; }

    public void OnGet() { }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        Result = await demo.HandleAsync(
            new CreateJobConsistencyDemoRequest(Name, CronExpression, Payload), cancellationToken);
    }
}
