# CoffeeNChill Functions — Setup Notes (merged project)

## Addendum applied (per updated POE doc)
The module updated the brief after the original version was issued:
- **Staff documents now use Azure Blob Storage**, not Azure File Storage — because
  Azure File Storage isn't emulated by Azurite. Thato already implemented this
  correctly using `Azure.Storage.Blobs` (container name: `staff-docs`).
- The standalone Docker run command for the Functions container **no longer needs
  the `-e AzureWebJobsStorage=...` flag** — the code already falls back to
  `UseDevelopmentStorage=true` when that environment variable isn't set, so it's
  redundant to pass it explicitly.

## What's in this folder (now merged)
- `MenuItemEntity.cs`, `MenuFunctions.cs` — Reesaido's part: the `MenuItems` table +
  5 CRUD functions (Create, GetAll, GetByCategory, Update, Delete)
- `UploadStaffDocument.cs`, `ListStaffDocuments.cs`, `DownloadStaffDocument.cs` —
  Thato's part: the `staff-docs` Blob container + 3 functions (Upload, List, Download)
- `Program.cs` — single shared entry point; registers `BlobServiceClient` for DI
  (used by the document functions). The menu functions build their own `TableClient`
  internally, so no extra DI registration was needed for those.
- `CoffeeNChillFunctions.csproj` — one project, references both `Azure.Data.Tables`
  and `Azure.Storage.Blobs`, plus `Microsoft.AspNetCore.WebUtilities` for parsing the
  multipart upload.
- `Dockerfile` — multi-stage build, isolated-worker .NET 9 base image.

## Do you need to manually create anything in Azure?
**No.** Nothing in this part touches a real Azure subscription. Both storage types are
created automatically the first time they're used:
- `MenuItems` table -> created by `CreateIfNotExistsAsync()` in `MenuFunctions.cs`
- `staff-docs` container -> created by `CreateIfNotExistsAsync()` in
  `UploadStaffDocument.cs` / `ListStaffDocuments.cs`

All you need running is **Azurite** (locally or in its own Docker container) — no
Azure Storage Account, no Azure Portal, no `az` CLI login.

## Running it locally (Visual Studio, .NET 9)
1. Visual Studio 2022 17.12+, with the **Azure development** workload installed.
2. Copy `local.settings.json.example` → `local.settings.json`.
3. Start Azurite first (your existing shortcut is fine — leave that window open).
4. Hit F5. Watch the console window for the list of function URLs.
5. Test all 8 endpoints (5 menu + 3 documents) in Postman before recording anything.

## Running the two standalone Docker containers (per the addendum)
```
# Azurite, in its own container
docker run -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite

# Build & push your Functions image
docker build -t <dockerhub_username>/coffeenchill-functions:v1.0 .
docker push <dockerhub_username>/coffeenchill-functions:v1.0

# Run the Functions container (no connection string needed, per the addendum)
docker run -p 7071:80 <dockerhub_username>/coffeenchill-functions:v1.0
```

**Heads-up (not covered by the addendum, but worth knowing):** two independent
`docker run` containers can't see each other over `localhost` — `UseDevelopmentStorage=true`
resolves to `127.0.0.1` inside the Functions container, which is the container itself,
not your Azurite container. If your video needs Postman hitting the *containerized*
function and it can't reach storage, put both containers on the same custom network
and point the connection string at Azurite by container name instead:
```
docker network create coffeenchill-net
docker run -d --name azurite --network coffeenchill-net -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
docker run -d --name functions --network coffeenchill-net -p 7071:80 ^
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUUwYBI0U55oV9VXfEqoBWhVOKB4EDsD0EiIVjhZmXCbXW5oz2f8g4iCsHfNy9UhQV9zvA==;BlobEndpoint=http://azurite:10000/devstoreaccount1;QueueEndpoint=http://azurite:10001/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;" ^
  <dockerhub_username>/coffeenchill-functions:v1.0
```
If your marker is only checking that each container builds and runs in isolation
(rather than talking to each other), you can skip this and just follow the simpler
commands above exactly as the addendum shows them.

## Before you submit
- Both of you: check the AI-disclosure comment in the file(s) you personally worked
  on and update it with what you actually tested/changed.
- Merge your Postman requests into one collection (8 total requests), export as
  `.json`, commit to `/docs`.
- Record the video showing both containers running and the full Postman collection
  passing.
