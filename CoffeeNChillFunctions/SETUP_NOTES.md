# CoffeeNChill Functions

- `MenuItemEntity.cs` and `MenuFunctions.cs` — Reesaido's side. The `MenuItems` table and the five CRUD functions (Create, GetAll, GetByCategory, Update, Delete).
- `UploadStaffDocument.cs`, `ListStaffDocuments.cs`, `DownloadStaffDocument.cs` — Thato's side. The `staff-docs` Blob container and the three document functions.
- `Program.cs` — one shared entry point for both. It registers a `BlobServiceClient` for the document functions; the menu functions build their own `TableClient` internally, so that's all the DI setup needed.
- `CoffeeNChillFunctions.csproj` — one project referencing both `Azure.Data.Tables` and `Azure.Storage.Blobs`, plus `Microsoft.AspNetCore.WebUtilities` for parsing the multipart upload.
- `Dockerfile` — multi-stage build on the isolated-worker .NET 9 base image.

## Running it locally

1. Visual Studio 2022 17.12 or later, with the Azure development workload installed.
2. Copy `local.settings.json.example` to `local.settings.json`.
3. Start Azurite first and leave it running.
4. Hit F5 and watch the console for the list of endpoint URLs.
5. Test all 8 endpoints in Postman (`docs/CoffeeNChill.postman_collection.json`) before recording anything.
