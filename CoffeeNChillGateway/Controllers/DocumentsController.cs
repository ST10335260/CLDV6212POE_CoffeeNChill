using Azure.Storage.Blobs;
using CoffeeNChillGateway.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Staff documents (recipes, manuals, policies). Admin and Staff only.</summary>
[Route("api/documents")]
// Only Admin and Staff roles may use these endpoints (Microsoft, n.d.f).
[Authorize(Roles = "Admin,Staff")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class DocumentsController : GatewayControllerBase
{
    private const string ContainerName = "staff-docs";

    private readonly IHttpClientFactory _clients;
    private readonly BlobServiceClient _blobs;

    public DocumentsController(IHttpClientFactory clients, BlobServiceClient blobs)
    {
        _clients = clients;
        _blobs = blobs;
    }

    private HttpClient Functions => _clients.CreateClient("functions");

    /// <summary>Uploads a document to the staff-docs container.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadDocumentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails { Status = 400, Title = "A non-empty file is required." });
        }

        // Forwards the uploaded file as multipart/form-data. IFormFile handling follows Microsoft (n.d.h).
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        content.Add(new StreamContent(stream), "file", file.FileName);

        var response = await Functions.PostAsync("documents/upload", await Buffered(content));
        return await Relay<UploadDocumentResponse, UploadDocumentResponse>(response, r => r);
    }

    /// <summary>Lists the documents in the staff-docs container.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DocumentInfoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List()
    {
        var response = await Functions.GetAsync("documents");
        return await Relay<List<DocumentInfoResponse>, List<DocumentInfoResponse>>(response, r => r);
    }

    /// <summary>Downloads one document.</summary>
    [HttpGet("{fileName}")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(string fileName)
    {
        var response = await Functions.GetAsync(
            $"documents/download/{Uri.EscapeDataString(fileName)}", HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return FunctionsError(response, await response.Content.ReadAsStringAsync());
        }

        string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        return File(await response.Content.ReadAsStreamAsync(), contentType, fileName);
    }

    /// <summary>Deletes one document from the staff-docs container.</summary>
    [HttpDelete("{fileName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string fileName)
    {
        var container = _blobs.GetBlobContainerClient(ContainerName);
        // Deletes the blob if it exists (Microsoft, n.d.a).
        bool deleted = await container.GetBlobClient(fileName).DeleteIfExistsAsync();

        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Document not found.",
                Detail = $"'{fileName}' was not found in {ContainerName}."
            });
        }

        return NoContent();
    }
}
