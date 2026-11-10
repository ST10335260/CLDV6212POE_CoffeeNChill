using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions
{
    // GET /api/documents/download/{fileName}
    // Streams the requested recipe/manual/policy PDF straight back to the
    // caller from the "staff-docs" container.
    public class DownloadStaffDocument
    {
        private const string ContainerName = "staff-docs";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<DownloadStaffDocument> _logger;

        public DownloadStaffDocument(BlobServiceClient blobServiceClient, ILogger<DownloadStaffDocument> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        // HTTP trigger with an authorization level and route (Microsoft, n.d.a).
        [Function("DownloadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req,
            string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            // ExistsAsync checks that the blob is there before downloading (Microsoft, n.d.b).
            if (!await blobClient.ExistsAsync())
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(new { error = $"'{fileName}' was not found in {ContainerName}." });
                return notFound;
            }

            // DownloadStreamingAsync returns the blob content as a stream (Microsoft, n.d.b).
            var download = await blobClient.DownloadStreamingAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", download.Value.Details.ContentType ?? "application/octet-stream");
            response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

            await download.Value.Content.CopyToAsync(response.Body);

            _logger.LogInformation("Downloaded {FileName} from {Container}", fileName, ContainerName);

            return response;
        }
    }
}
