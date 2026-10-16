 # CLDV6212POE_CoffeeNChill — Part 1 

Azure Functions, Table Storage, Blob Storage and standalone Docker containers for the CoffeeNChill digital menu and staff-document system. 

1. Local setup (no Docker)

1. Install the .NET 9 SDK, Azure Functions Core Tools v4 and Azurite (`npm install -g azurite`, or the VS Code extension) (Microsoft, n.d.i). Visual Studio users need VS 2022 17.12 or later for .NET 9 Functions support.
2. Copy `CoffeeNChillFunctions/local.settings.json.example` to `local.settings.json` in the same folder. The real file is gitignored and must never be committed.
3. Start Azurite: `azurite --silent --location ./azurite-data`
4. Restore and run:

```
cd CoffeeNChillFunctions
dotnet restore
func start
```

The `MenuItems` table and the `staff-docs` blob container are created automatically on first use with `CreateIfNotExistsAsync` .

2. Endpoints

All endpoints are HTTP-triggered functions.

 Menu items (Table Storage — Reesaido)

Menu entities are read and written through `TableClient` . Request bodies are deserialised with the System.Text.Json web defaults.

| Method | Route | Description |
|---|---|---|
| POST | `/api/menu` | Insert a new menu entity into `MenuItems`. |
| GET | `/api/menu` | Return all menu entities. |
| GET | `/api/menu/category/{category}` | Filter by PartitionKey (category). |
| PUT | `/api/menu/{category}/{id}` | Update price/availability. |
| DELETE | `/api/menu/{category}/{id}` | Remove an item. |

Staff documents (Blob Storage — Thato)

Files are uploaded, listed and downloaded through `BlobContainerClient` and `BlobClient` . The upload function parses the multipart body with `MultipartReader`, following Microsoft's guidance on handling file uploads.

| Method | Route | Description |
|---|---|---|
| POST | `/api/documents/upload` | `multipart/form-data`, field name `file`. Streams the file into `staff-docs`. |
| GET | `/api/documents` | Lists every blob in `staff-docs`: name, size, last modified. |
| GET | `/api/documents/download/{fileName}` | Streams the named blob back to the client. |

3. Standalone Docker execution

One image contains both function groups. The Dockerfile follows Docker's multi-stage build approach (Docker, n.d.), and the container setup follows Microsoft's guidance for running Azure Functions in containers (Microsoft, n.d.j). Azurite runs as its own container (Microsoft, n.d.i), linked to the Functions container over a user-defined network.

## 4. Postman collection

`docs/CoffeeNChill.postman_collection.json` covers all 8 endpoints (Menu and Staff Documents), each with pass/fail assertions, split into two folders. This is the single collection to export before submission. No separate merge step is needed now that both members' requests are in it.

 5. Members

Reesaido Alagiry — Menu Table Storage: schema design, all five menu functions (`MenuFunctions.cs`, `MenuItemEntity.cs`) and the Postman requests for the menu endpoints.

Thato Gumede — Staff document Blob Storage: all three document functions (`UploadStaffDocument.cs`, `ListStaffDocuments.cs`, `DownloadStaffDocument.cs`), the `Program.cs` and `.csproj` build fixes, Docker and Docker Hub setup for both images, this README, the demo video and the staff-document Postman requests.

6. AI tool use

Parts of the staff-document functions bugs were able to be be diagonised and explained by chatgpt.

## References

Docker. (n.d.) *Multi-stage builds*. Docker Docs. Available at: https://docs.docker.com/build/building/multi-stage/ (Accessed: 16 October 2026).

Microsoft. (n.d.a) *Azure Functions HTTP trigger*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-http-webhook-trigger (Accessed: 16 October 2026).

Microsoft. (n.d.b) *BlobClient Class (Azure.Storage.Blobs)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient (Accessed: 16 October 2026).

Microsoft. (n.d.c) *BlobContainerClient Class (Azure.Storage.Blobs)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobcontainerclient (Accessed: 16 October 2026).

Microsoft. (n.d.d) *Guide for running C# Azure Functions in an isolated worker process*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide (Accessed: 16 October 2026).

Microsoft. (n.d.e) *JsonSerializerDefaults Enum (System.Text.Json)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializerdefaults (Accessed: 16 October 2026).

Microsoft. (n.d.f) *MultipartReader Class (Microsoft.AspNetCore.WebUtilities)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.multipartreader (Accessed: 16 October 2026).

Microsoft. (n.d.g) *TableClient Class (Azure.Data.Tables)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient (Accessed: 16 October 2026).

Microsoft. (n.d.h) *Upload files in ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads (Accessed: 16 October 2026).

Microsoft. (n.d.i) *Use the Azurite emulator for local Azure Storage development*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite (Accessed: 16 October 2026).

Microsoft. (n.d.j) *Work with Azure Functions in Containers*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-how-to-custom-container (Accessed: 16 October 2026).
