using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services => {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var localRoot = Environment.GetEnvironmentVariable("AzureWebJobsScriptRoot");
        var azureRoot = $"{Environment.GetEnvironmentVariable("HOME")}/site/wwwroot";

        var actualRoot = localRoot ?? azureRoot;

        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(actualRoot)
            .AddEnvironmentVariables()
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        IConfiguration configuration = configBuilder.Build();

        services.AddSingleton<IConfiguration>(configuration);
    })
    .Build();

host.Run();
