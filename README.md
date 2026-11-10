# CLDV6212POE_CoffeeNChill


**Demo video (Part 2): https://youtube.com/shorts/-iVjr0i9TGg?si=jfOpFzsayWVFLtOZ
Docker Hub: `thatogumede/coffeenchill-functions:v2.0`


1. Asynchronous order processing and Docker Compose is added. 


## 1. Endpoints
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


## 2. Postman collection

`docs/CoffeeNChill.postman_collection.json` covers all 8 endpoints (Menu +
Staff Documents), each with pass/fail assertions, split into two folders.
Part 2: this collection now also includes the order-queue request.


# Part 2: Queue Triggers & Docker Compose Orchestration


1. Asynchronous order processing. An HTTP-triggered function accepts an
   order and drops it onto an Azure Storage Queue straight away. A separate
   queue-triggered function picks it up in the background, logs it to an
   **Orders** table, and tracks it through its status lifecycle.
2. Docker Compose orchestration Azurite and the Functions app, start together
   from a single `docker-compose.yml` that pulls our published Docker Hub
   image (`v2.0`).


## 3. Part 2 endpoints and functions

| Trigger | Route / Queue | Description |
| --- | --- | --- |
| HTTP POST | `/api/orders/queue` | Validates the order payload and places it on `order-processing-queue`. Returns immediately. |
| Queue | `order-processing-queue` | `ProcessOrderQueue` picks up each message, writes it to the Orders table and moves it through its status lifecycle. |

### 4 Order producer: `POST /api/orders/queue`

An HTTP-triggered Azure Function that:

- accepts the order in the request body,
- validates the payload first, rejecting requests with missing required
  fields such as `OrderId` or `TotalPrice`,
- serializes the validated order to a JSON message,
- pushes it onto `order-processing-queue`, creating the queue first if it
  does not exist (`CreateIfNotExistsAsync`),
- returns immediately. It only places a message on the queue and does not
  wait for the order to be processed.


### Queue-triggered processing: `ProcessOrderQueue`

A queue-triggered function bound to `order-processing-queue`. The Functions
runtime runs it automatically whenever a new message arrives, so no HTTP call
is needed (Microsoft Learn, n.d.-b). For each message it:

1. parses the JSON payload back into an `Order` object,
2. writes a new entry to the **Orders** table with `Status = "Received"`,
3. simulates the order progressing through the rest of its lifecycle,
   updating the table at each stage.


### 6. Status lifecycle

Received,  Preparing,  Ready, Collected

### 7 Failure handling
Messages with an invalid JSON payload are handled so that one bad message cannot stop the
rest of the queue from being processed.


### 8 `docker-compose.yml`

The file lives in the root of the repository.


### Start the stack

docker-compose up


That single command starts both containers. To stop everything and start
again from a clean state:

docker-compose down
docker-compose up


## 9. Testing the order flow

1. Run docker-compose up and wait until both containers are running.
2. Import the Postman collection from the /docs folder.
3. Send POST /api/orders/queue with the sample body.
   The response comes back quickly because the order has only been placed on
   the queue.
4. Watch the message land on order-processing-queue (via Azure Storage
   Explorer pointed at Azurite).
5. Check the Orders table, where the order appears as Received and then
   moves through Preparing, Ready and Collected.
6. Run the full Postman collection. The added checks confirm that messages
   are placed on the queue and that status updates appear in the Orders table.

## 10. Postman collection 

`docs/CoffeeNChill.postman_collection.json` now holds the original 8 Part 1
requests plus the new `POST /api/orders/queue` request, with sample order data
and checks for queue placement and Orders table status updates. 

## 11.Members

- Reesaido Alagiry — Order producer endpoint, queue placement,
  ProcessOrderQueue function, Orders table design, status lifecycle, failure
  handling, and the Postman request for POST /api/orders/queue.
- Thato Gumede — docker-compose.yml, Docker Hub v2.0 image, wiring the
  services together, the merged Postman collection, and
  the demo video.


## 12. References

Docker Inc. (n.d.-a) *Docker Compose overview*. Available at: https://docs.docker.com/compose/ .

Docker Inc. (n.d.-b) *Networking in Compose*. Available at: https://docs.docker.com/compose/networking/ .

Microsoft Learn (n.d.-a) *What is Azure Queue Storage?* Available at: https://learn.microsoft.com/en-us/azure/storage/queues/storage-queues-introduction .

Microsoft Learn (n.d.-b) *Azure Queue storage trigger and bindings for Azure Functions overview*. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger 

Microsoft Learn (n.d.-c) *Use the Azurite emulator for local Azure Storage development*. Available at: https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite 
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
