using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scheduler.BusinessLogic.Concrete;
using Scheduler.DataAccess.Azure;

namespace Scheduler.BusinessLogic.Presentation;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services
            .AddSchedulerDataAccess(builder.Configuration)
            .AddSchedulerBusinessLogic();

        // Show DAL routing + BL use-case debug lines on the console for capture.
        builder.Logging.AddSimpleConsole(o => o.SingleLine = true);
        builder.Logging.SetMinimumLevel(LogLevel.Debug);

        var app = builder.Build();

        app.UseStaticFiles();
        app.MapRazorPages();
        app.Run();
    }
}
