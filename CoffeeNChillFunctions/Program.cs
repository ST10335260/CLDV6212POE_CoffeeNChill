using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Host set up for the isolated worker model (Microsoft, n.d.d).
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
            // Falls back to the local Azurite storage emulator when no connection string is set (Microsoft, n.d.i).
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            return new BlobServiceClient(connectionString);
        });
    })
    .Build();

host.Run();
