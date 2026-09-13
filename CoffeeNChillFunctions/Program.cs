using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // Used by the staff-document functions (Upload/List/Download).
        // AzureWebJobsStorage is set in local.settings.json for `func start`,
        // and passed as a Docker environment variable for the standalone
        // Functions container (see README).
        services.AddSingleton(_ =>
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
