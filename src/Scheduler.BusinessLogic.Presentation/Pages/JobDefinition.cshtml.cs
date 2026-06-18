using Scheduler.BusinessLogic.Abstractions.Handlers.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Handlers.JobDetails;
using Scheduler.BusinessLogic.Abstractions.Models.JobDefinitions;
using Scheduler.BusinessLogic.Abstractions.Models.JobDetails;

namespace Scheduler.BusinessLogic.Presentation.Pages;

public class JobDefinitionModel(
    ICreateJobDefinitionHandler create,
    IGetJobDefinitionHandler getDefinition,
    IGetJobDetailHandler getDetail)
    : PageModel
{
    [BindProperty] public string Name { get; set; } = "Nightly report";
    [BindProperty] public string CronExpression { get; set; } = "0 0 * * *";
    [BindProperty] public string DetailType { get; set; } = "HttpCall";
    [BindProperty] public string Payload { get; set; } = "{ \"url\": \"https://example.com\" }";

    public CreateJobDefinitionResponse Created { get; private set; }
    public GetJobDefinitionResponse Definition { get; private set; }
    public GetJobDetailResponse Detail { get; private set; }

    public void OnGet() { }

    public async Task OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = Guid.NewGuid();

        Created = await create.HandleAsync(
            new CreateJobDefinitionRequest(
                userId,
                Name,
                "Granular handler demo (UC 1.1)",
                CronExpression,
                true,
                DateTime.UtcNow.AddMinutes(1),
                DetailType,
                Payload),
            cancellationToken);

        Definition = await getDefinition.HandleAsync(
            new GetJobDefinitionRequest(userId, Created.JobDefinitionId), cancellationToken);

        Detail = await getDetail.HandleAsync(
            new GetJobDetailRequest(Created.JobDetailId), cancellationToken);
    }
}
