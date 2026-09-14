# CLDV6212POE_CoffeeNChill — Part 1 (CLDV6212/W, Cloud Development B)

Azure Functions, Table Storage, Blob Storage and standalone Docker containers
for the CoffeeNChill digital menu and staff-document system.

> **Addendum applied:** staff documents are stored in **Azure Blob Storage**
> (container `staff-docs`), not Azure File Storage — File Storage is not
> emulated by Azurite.

## Fixes applied to get this building

The project wouldn't restore/build as originally pushed, for two reasons —
both fixed in this version:

1. `Program.cs` called `ConfigureFunctionsWebApplication()`, which only
   exists if you reference the `Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore`
   package. That package wasn't referenced, so this failed to compile. Since
   every function in this project already uses the classic
   `HttpRequestData`/`HttpResponseData` model, the fix was to switch back to
   `ConfigureFunctionsWorkerDefaults()` — no new package needed.
2. `CoffeeNChillFunctions.csproj` referenced `local.settings.json.example`
   for the build to copy, but that file was never committed (only
   `local.settings.json` was `.gitignore`d — its example counterpart never
   made it into the last push). It's included now.

## 1. Local setup (no Docker)

1. Install the [.NET 9 SDK](https://dotnet.microsoft.com/download),
   [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local),
   and [Azurite](https://github.com/Azure/Azurite)
   (`npm install -g azurite`, or the VS Code extension). Visual Studio users
   need VS 2022 17.12+ for .NET 9 Functions support.
2. Copy `CoffeeNChillFunctions/local.settings.json.example` to
   `local.settings.json` in the same folder (gitignored — never commit the
   real one).
3. Start Azurite: `azurite --silent --location ./azurite-data`
4. Restore and run:
   ```bash
   cd CoffeeNChillFunctions
   dotnet restore
   func start
   ```
5. Both the `MenuItems` table and the `staff-docs` blob container are
   created automatically on first use (`CreateIfNotExistsAsync`) — nobody
   needs to pre-create anything by hand or touch a real Azure subscription.

## 2. Endpoints

### Menu items (Table Storage — Reesaido)

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/menu` | Insert a new menu entity into `MenuItems`. |
| GET | `/api/menu` | Return all menu entities. |
| GET | `/api/menu/category/{category}` | Filter by `PartitionKey` (category). |
| PUT | `/api/menu/{category}/{id}` | Update price/availability. |
| DELETE | `/api/menu/{category}/{id}` | Remove an item. |

### Staff documents (Blob Storage — Thato)

| Method | Route | Description |
| --- | --- | --- |
| POST | `/api/documents/upload` | `multipart/form-data`, field name `file`. Streams the file into `staff-docs`. |
| GET | `/api/documents` | Lists every blob in `staff-docs`: name, size, last modified. |
| GET | `/api/documents/download/{fileName}` | Streams the named blob back to the client. |

**Note on auth levels:** the menu functions use `AuthorizationLevel.Function`
and the document functions use `AuthorizationLevel.Anonymous`. Azure
Functions Core Tools doesn't enforce function keys locally either way, so
this won't affect local testing — but worth agreeing on one level between
you before an eventual real Azure deployment.

## 3. Standalone Docker execution (no Docker Compose — Part 1 requirement)

One image now contains both function groups. Azurite still runs as its own
separate container, linked over a user-defined network.

```bash
# 1. Shared network so the containers can see each other by name
docker network create coffeenchill-net

# 2. Azurite, bound to the default storage ports
docker run -d --name azurite --network coffeenchill-net \
  -p 10000:10000 -p 10001:10001 -p 10002:10002 \
  mcr.microsoft.com/azure-storage/azurite

# 3. Tag + push the Azurite image too (per the deliverables checklist)
docker tag mcr.microsoft.com/azure-storage/azurite <dockerhub_username>/coffeenchill-azurite:v1.0
docker push <dockerhub_username>/coffeenchill-azurite:v1.0

# 4. Build the Functions image (contains menu + document functions)
cd CoffeeNChillFunctions
docker build -t <dockerhub_username>/coffeenchill-functions:v1.0 .

# 5. Push it
docker push <dockerhub_username>/coffeenchill-functions:v1.0

# 6. Run it standalone, on the same network as Azurite
docker run -d --name coffeenchill-functions --network coffeenchill-net \
  -p 7071:80 \
  -e FUNCTIONS_WORKER_RUNTIME="dotnet-isolated" \
  -e AzureWebJobsStorage="DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://azurite:10000/devstoreaccount1;TableEndpoint=http://azurite:10002/devstoreaccount1;" \
  <dockerhub_username>/coffeenchill-functions:v1.0
```

Notes:
- The `AccountName`/`AccountKey` above are Azurite's fixed, publicly
  documented development credentials — not a secret, safe to commit.
- `http://azurite:10000/...` uses the Docker network alias `azurite`, not
  `localhost` — inside a container, `localhost` means "this container."
- **On the addendum's note** that passing the connection string in the
  `docker run` script "has also been omitted, as it is not required": the
  brief's own example command is missing the actual flag content, so it's
  genuinely ambiguous whether they mean it can be baked into the image
  instead. The `-e` approach above is the standard, portable pattern — flag
  the wording with your lecturer if it's still unclear before recording.
- Confirm both function groups respond once the container is up:
  `curl http://localhost:7071/api/documents` and
  `curl http://localhost:7071/api/menu`.

## 4. Postman collection

`docs/CoffeeNChill.postman_collection.json` covers all 8 endpoints (Menu +
Staff Documents), each with pass/fail assertions, split into two folders.
This is the single collection to export/re-export before submission — no
separate merge step needed now that both members' requests are in it.

## 5. What each member worked on

- **Reesaido Alagiry** — Menu Table Storage: schema design, all five Menu
  functions (`MenuFunctions.cs`, `MenuItemEntity.cs`), Postman requests for
  the menu endpoints.
- **Thato Gumede** — Staff Document Blob Storage: all three document
  functions (`UploadStaffDocument.cs`, `ListStaffDocuments.cs`,
  `DownloadStaffDocument.cs`), the `Program.cs`/`.csproj` build fixes,
  Docker/Docker Hub setup for both images, this README, the demo video, and
  the staff-document Postman requests.

## 6. AI tool use

Parts of the staff-document functions, the `Program.cs`/`.csproj` fixes,
Dockerfile, and Postman collection were scaffolded with AI assistance
(Claude) and adjustments to comments and reviewed/tested against Azurite by Thato Gumede.
