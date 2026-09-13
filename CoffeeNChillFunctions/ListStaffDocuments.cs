using System.Net;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions
{
    // GET /api/documents
    // Lists every operational file currently stored in the "staff-docs"
    // container, with name, size, and last-modified date.
    public class ListStaffDocuments
    {
        private const string ContainerName = "staff-docs";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<ListStaffDocuments> _logger;

        public ListStaffDocuments(BlobServiceClient blobServiceClient, ILogger<ListStaffDocuments> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("ListStaffDocuments")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var documents = new List<object>();

            await foreach (var blobItem in containerClient.GetBlobsAsync())
            {
                documents.Add(new
                {
                    fileName = blobItem.Name,
                    sizeInBytes = blobItem.Properties.ContentLength ?? 0,
                    lastModified = blobItem.Properties.LastModified
                });
            }

            _logger.LogInformation("Listed {Count} staff documents from {Container}", documents.Count, ContainerName);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(documents);
            return response;
        }
    }
}
