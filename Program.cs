using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WebsiteWatcher.Services;
using WebsiteWatcher;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(app =>
    {
        app.UseWhen<SafeBrowsingMiddleware>(context =>
        {
            return context.FunctionDefinition.Name == "Register";
        });
    })
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddSingleton<PdfCreatorService> ();
        services.AddSingleton<SafeBrowsingService>();
    })
    .Build();

host.Run();
