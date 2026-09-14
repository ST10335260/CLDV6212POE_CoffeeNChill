using System.Net;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace CoffeeNChillFunctions
{
    // POST /api/documents/upload
    // Accepts a multipart/form-data file (e.g. a barista recipe sheet or
    // cleaning manual) and streams it straight into the "staff-docs" Blob
    // Storage container - no temp file is buffered on disk.
    public class UploadStaffDocument
    {
        private const string ContainerName = "staff-docs";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<UploadStaffDocument> _logger;

        public UploadStaffDocument(BlobServiceClient blobServiceClient, ILogger<UploadStaffDocument> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        [Function("UploadStaffDocument")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
        {
            if (!req.Headers.TryGetValues("Content-Type", out var contentTypeValues))
            {
                return await WriteError(req, HttpStatusCode.BadRequest, "Missing Content-Type header.");
            }

            string contentType = contentTypeValues.First();
            string? boundary = GetBoundary(contentType);

            if (string.IsNullOrEmpty(boundary))
            {
                return await WriteError(req, HttpStatusCode.BadRequest,
                    "Request must be multipart/form-data with a valid boundary.");
            }

            var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
            await containerClient.CreateIfNotExistsAsync();

            var reader = new MultipartReader(boundary, req.Body);
            string? uploadedFileName = null;
            long uploadedBytes = 0;

            MultipartSection? section;
            while ((section = await reader.ReadNextSectionAsync()) != null)
            {
                var contentDisposition = section.GetContentDispositionHeader();

                if (!IsFileSection(contentDisposition))
                {
                    continue; // skip plain form fields - we only care about the file part
                }

                string? fileName = contentDisposition!.FileName.Value ?? contentDisposition.FileNameStar.Value;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = $"upload-{Guid.NewGuid()}";
                }

                var blobClient = containerClient.GetBlobClient(fileName);

                // Stream directly from the request body to the blob - no disk buffering.
                await blobClient.UploadAsync(section.Body, overwrite: true);

                var properties = await blobClient.GetPropertiesAsync();
                uploadedFileName = fileName;
                uploadedBytes = properties.Value.ContentLength;

                _logger.LogInformation("Uploaded {FileName} ({Bytes} bytes) to {Container}",
                    fileName, uploadedBytes, ContainerName);
            }

            if (uploadedFileName is null)
            {
                return await WriteError(req, HttpStatusCode.BadRequest,
                    "No file part found in the multipart body. Use a form field named 'file'.");
            }

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(new
            {
                message = "File uploaded successfully.",
                fileName = uploadedFileName,
                sizeInBytes = uploadedBytes
            });
            return response;
        }

        // Mirrors the disposition check Microsoft's own samples use, since
        // Microsoft.AspNetCore.WebUtilities doesn't ship one out of the box.
        private static bool IsFileSection(ContentDispositionHeaderValue? contentDisposition)
        {
            return contentDisposition is not null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }

        private static string? GetBoundary(string contentType)
        {
            string? boundaryPart = contentType
                .Split(';')
                .Select(p => p.Trim())
                .FirstOrDefault(p => p.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase));

            return boundaryPart?["boundary=".Length..].Trim('"');
        }

        private static async Task<HttpResponseData> WriteError(HttpRequestData req, HttpStatusCode status, string message)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(new { error = message });
            return response;
        }
    }
}
