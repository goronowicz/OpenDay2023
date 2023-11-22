using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration(builder =>
    {
        if (IsDevelopmentEnvironment())
        {
            builder
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        }

        var configuration = builder
            .AddEnvironmentVariables()
            .Build();

        builder.AddConfiguration(configuration);
    })
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();

static bool IsDevelopmentEnvironment()
{
    const string developmentEnvironment = "Development";
    const string azureFunctionsEnvironmentVariableName = "AZURE_FUNCTIONS_ENVIRONMENT";

    return developmentEnvironment.Equals(
        Environment.GetEnvironmentVariable(azureFunctionsEnvironmentVariableName),
        StringComparison.OrdinalIgnoreCase);
}