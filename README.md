# CLDV6212POE_CoffeeNChill

**Live URL (Part 3): http://102.133.167.157:5000/swagger**
**Demo video (Part 3):https://youtu.be/7cg_YdAXGMo?si=y4oeh3ALIViCKh5y
**Docker Hub:** `st10335260/coffeenchill-gateway:v3.0` and `thatogumede/coffeenchill-functions:v2.0`

Group 13 – CLDV6212/W, Cloud Development B: Reesaido Alagiry (ST10335260) and Thato Gumede (ST10275159)

Part 3 adds an ASP.NET Core API gateway in front of the Functions app, JWT authentication with role-based access, and a production deployment on a Linux VPS with a CI/CD pipeline.

1. API gateway with Swagger UI, DTOs and global exception middleware is added.
2. JWT authentication and user profiles (Student, Staff and Admin roles) are added.
3. The stack is deployed on an Azure Linux VPS and redeployed automatically by GitHub Actions.

## Architecture

<img width="961" height="452" alt="CLDV6212 architecture diagram: Swagger UI or Postman calls the ASP.NET Core gateway, which forwards to the Azure Functions app and reads Azure Storage or Azurite tables, blobs and queues, all running with Docker Compose on a Linux VPS" src="https://github.com/user-attachments/assets/b1938dbf-c0f2-473c-8c38-0d739b5b4642" />

The request path is: **client** (Swagger UI or Postman) → **gateway** (ASP.NET Core Web API) → **Functions app** (menu, order queue and documents) → **Azure Storage or Azurite** (Tables, Blobs and Queues). Clients talk over HTTP with a Bearer JWT, and the whole stack runs with Docker Compose on a Linux VPS.

The gateway is the only entry point for clients. It validates the JWT, checks the caller's role, maps request and response DTOs, and forwards most calls to the Functions app through a named `HttpClient` created by `IHttpClientFactory` (Microsoft, n.d.g). Two endpoints are handled directly against storage because the Functions app has no equivalent: `PUT /api/orders/status` and `DELETE /api/documents/{fileName}`. The Users table is also accessed directly by the gateway.

## 1. Endpoints

Roles are enforced with `[Authorize(Roles = "Admin,Staff")]` on the protected endpoints (Microsoft, n.d.j). A request with no token receives **401**, a valid token with the wrong role receives **403**, and invalid input receives **400** with a `ProblemDetails` body that lists each invalid field.

### Authentication (Users table – Thato)

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Public | Creates a `Student` account with a hashed password. Duplicate email returns 409. |
| POST | `/api/auth/login` | Public | Returns a signed JWT with the user's name, email and role. |

### Menu items (Table Storage – Reesaido)

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| GET | `/api/menu` | Public | Return all menu items. |
| GET | `/api/menu/category/{category}` | Public | Filter by category. |
| POST | `/api/menu` | Admin, Staff | Insert a new menu item. |
| PUT | `/api/menu/{category}/{id}` | Admin, Staff | Update price and availability. |
| DELETE | `/api/menu/{category}/{id}` | Admin, Staff | Remove an item. |

### Orders (Queue and Table Storage – Reesaido)

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| POST | `/api/orders/queue` | Public | Validates the order and places it on `order-processing-queue`. Returns immediately. |
| GET | `/api/orders/{orderDate}/{orderId}` | Public | Track one order through its status lifecycle. |
| GET | `/api/orders` | Admin, Staff | List all orders. |
| PUT | `/api/orders/status` | Admin, Staff | Update an order's status. Handled by the gateway through `TableClient` (Microsoft, n.d.k). |

### Staff documents (Blob Storage – Reesaido)

| Method | Route | Access | Description |
| --- | --- | --- | --- |
| POST | `/api/documents` | Admin, Staff | `multipart/form-data`, field name `file`, using `IFormFile` (Microsoft, n.d.l). |
| GET | `/api/documents` | Admin, Staff | Lists every blob in `staff-docs`. |
| GET | `/api/documents/{fileName}` | Admin, Staff | Streams the named blob back to the client. |
| DELETE | `/api/documents/{fileName}` | Admin, Staff | Deletes the blob. Handled by the gateway through `BlobClient` (Microsoft, n.d.b). |

## 2. Postman collection

`CoffeeNChillFunctions/docs/CoffeeNChill_Part3.postman_collection.json` holds 46 tests, each with pass/fail assertions, split into six folders: authentication edge cases, public endpoints, protected endpoints without a token (401), the student role (403), the staff role and the admin role. The collection is executed with the Collection Runner (Postman, n.d.).

To run it:

1. Import the collection into Postman.
2. On the collection's **Variables** tab, set the **Current value** of `baseUrl` (for example `http://102.133.167.157:5000`), `staffPassword` and `adminPassword`. The passwords are the values of `SEED_STAFF_PASSWORD` and `SEED_ADMIN_PASSWORD`, which are not stored in the repository.
3. Attach a small PDF to the **Upload document** request, then run the collection.

# Part 3: API Gateway, JWT Authentication and VPS Deployment

## 3. Part 3 components

| Component | Location | Description |
| --- | --- | --- |
| Controllers | `CoffeeNChillGateway/Controllers` | Auth, menu, orders and documents controllers (Microsoft, n.d.d). |
| DTOs and mapping | `Dtos/`, `Mapping/` | Request and response models with data annotation validation. |
| Exception middleware | `Middleware/` | Turns any unhandled exception into one consistent `ProblemDetails` JSON error (Microsoft, n.d.f; Microsoft, n.d.m). |
| Authentication | `Auth/` | Users table, password hashing, JWT issuing and the demo account seeder (Microsoft, n.d.c). |
| Swagger UI | `/swagger` | Documents every endpoint from the XML comments and has a Bearer **Authorize** button (Microsoft, n.d.e). |
| Gateway Dockerfile | `CoffeeNChillGateway/Dockerfile` | Multi-stage build (Docker, n.d.b). |
| Workflow | `.github/workflows/deploy.yml` | Builds, pushes and deploys on every push to `main` (GitHub, n.d.c). |

The Functions app is unchanged apart from its menu endpoints, which are now anonymous because access control is enforced by the gateway.

## 4. Authentication and user profiles

- **Users table.** `PartitionKey` is the role (`Student`, `Staff` or `Admin`) and `RowKey` is the lower-case email address. Each row also stores the name, a password hash, the salt and the creation date.
- **Registration.** The account is always created with the `Student` role, so a caller cannot choose a higher role. Passwords are hashed with PBKDF2 (HMAC-SHA256, 600,000 iterations) and a random 16-byte salt per user (Microsoft, n.d.i), and the plain password is never stored.
- **Login.** The gateway checks the email and password and returns a signed JWT. The token carries the name, email and role claims, is signed with `Jwt:Key`, and expires after 60 minutes by default (Microsoft, n.d.c). A wrong email or password returns 401.
- **Seeded demo accounts.** At start-up the gateway creates `staff@coffeenchill.local` (Staff) and `admin@coffeenchill.local` (Admin) when their passwords are configured, so no account password is stored in the repository.

## 5. Configuration

Secrets are never committed. Locally they live in user secrets, and in Docker they are passed as environment variables from a `.env` file that is excluded from Git (see `.env.example`).

| Setting | Environment variable (Docker) | Purpose |
| --- | --- | --- |
| `Jwt:Key` | `JWT_KEY` | Signing key, required, at least 32 characters. |
| `Jwt:Issuer`, `Jwt:Audience` | set in `docker-compose.yml` | Token issuer and audience, with defaults. |
| `Jwt:ExpiryMinutes` | – | Token lifetime in minutes, default 60. |
| `Storage:ConnectionString` | set in `docker-compose.yml` | Azure Storage or Azurite connection string. |
| `Functions:BaseUrl` | set in `docker-compose.yml` | Address of the Functions app. |
| `Seed:Staff:Password` | `SEED_STAFF_PASSWORD` | Password for the seeded Staff account. |
| `Seed:Admin:Password` | `SEED_ADMIN_PASSWORD` | Password for the seeded Admin account. |

## 6. `docker-compose.yml`

The file lives in the root of the repository. It starts three services: Azurite (storage emulator), the Functions app and the gateway, using version-tagged images from Docker Hub (Docker, n.d.a).

### Start the stack

1. Copy `.env.example` to `.env` and set your own values for `JWT_KEY`, `SEED_STAFF_PASSWORD` and `SEED_ADMIN_PASSWORD`. Never commit `.env`.
2. Start the stack:

```bash
docker compose up -d
```

3. Open `http://localhost:5000/swagger`.

To stop everything and start again from a clean state:

```bash
docker compose down
docker compose up -d
```

### Running the gateway without Docker

1. Start Azurite:

```bash
docker run -d --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
```

2. Start the Functions app:

```bash
cd CoffeeNChillFunctions
func start
```

3. Set the gateway secrets from the repository root:

```bash
dotnet user-secrets init --project CoffeeNChillGateway
dotnet user-secrets set "Jwt:Key" "<random string of at least 32 characters>" --project CoffeeNChillGateway
dotnet user-secrets set "Seed:Staff:Password" "<staff password>" --project CoffeeNChillGateway
dotnet user-secrets set "Seed:Admin:Password" "<admin password>" --project CoffeeNChillGateway
```

4. Run the gateway and open the address it prints with `/swagger` added:

```bash
dotnet run --project CoffeeNChillGateway
```

## 7. Deployment on a Linux VPS

The stack runs on a Microsoft Azure virtual machine (Ubuntu 24.04, Standard B2ls v2, South Africa North) created in the Azure portal (Microsoft, n.d.h). The network security group allows inbound SSH (port 22) and the gateway (port 5000).

- The VM runs Docker and Docker Compose. The compose file pulls the published images from Docker Hub (Docker, n.d.a).
- The gateway secrets come from a `.env` file that exists **only on the server** and is never committed.
- The gateway is exposed on port 5000, and the Swagger UI is served at the live URL above.

To deploy by hand, SSH into the VM and run:

```bash
cd ~/CLDV6212POE_CoffeeNChill
docker compose pull
docker compose up -d
```

## 8. CI/CD pipeline

The workflow `.github/workflows/deploy.yml` runs on every push to `main` and can also be started manually (GitHub, n.d.c). It has these steps:

1. Check out the code and set up the .NET SDK.
2. Restore and build the gateway.
3. Log in to Docker Hub with a personal access token (Docker, n.d.c).
4. Build the gateway image and push it with two tags: `v3.0` and the commit hash.
5. Connect to the VPS over SSH, pull the new image and restart the stack with `docker compose up -d`.

All credentials are stored as encrypted GitHub repository secrets and are never written in the workflow file or the repository (GitHub, n.d.b): `DOCKERHUB_USERNAME`, `DOCKERHUB_TOKEN`, `VPS_HOST`, `VPS_USER` and `VPS_PASSWORD`.

## 9. Testing the gateway

1. Open the live URL (or `http://localhost:5000/swagger` locally).
2. Register with `POST /api/auth/register`, then log in with `POST /api/auth/login`.
3. Click **Authorize** and paste the token only, because Swagger adds the `Bearer` prefix.
4. Call a protected endpoint such as `POST /api/menu`. A Student token returns 403, no token returns 401, and a Staff or Admin token succeeds with 201.
5. Send an invalid order body to `POST /api/orders/queue` and check that it returns 400 with the list of invalid fields.
6. Run the full Postman collection (section 2). All 46 tests pass against the live server.

## 10. Order flow and poison message handling

A customer places an order through `POST /api/orders/queue`. The producer function validates it, encodes the message as Base64 and puts it on `order-processing-queue`, then responds immediately. A queue-triggered function writes it to the Orders table, and a `System.Timers.Timer` moves it through Received, Preparing, Ready and Collected.

Messages that fail processing are retried up to the configured dequeue threshold (`maxDequeueCount` is 3 in `host.json`), then moved to the `order-processing-queue-poison` queue (Microsoft, n.d.a). A second queue-triggered function, `LogPoisonOrder`, reads that queue and logs the failed message, so one bad message cannot stop the rest of the queue from being processed.

## 11. Repository structure

```text
.github/workflows/deploy.yml   CI/CD workflow
CoffeeNChillFunctions/         Azure Functions app and Dockerfile
  docs/                        Postman collections
CoffeeNChillGateway/           ASP.NET Core Web API gateway and Dockerfile
  Auth/                        user entity, password hashing, token service, seeder
  Controllers/                 auth, menu, orders and documents controllers
  Dtos/                        request and response models
  Mapping/                     DTO mapping
  Middleware/                  global exception handling
.env.example                   template for the secrets (copy to .env)
docker-compose.yml             full stack: Azurite, Functions and gateway
README.md
```

The repository is public. Each member worked on a feature branch, and the Part 3 work was merged into `main` through pull request #1 (GitHub, n.d.a). Both members have more than five commits spread over several days.

## 12. Members

- Reesaido Alagiry – API gateway, Swagger with XML documentation, DTOs and mapping, exception middleware, menu, orders and documents controllers, gateway Dockerfile, Docker Compose, Docker Hub images, VPS deployment and the CI/CD workflow.
- Thato Gumede – Users table, registration and login, JWT issuing, role-based access checks, demo account seeding, the Postman collection, this README and the demo video.

## 13. References

Docker. (n.d.a) *Docker Compose overview*. Docker Docs. Available at: https://docs.docker.com/compose/ (Accessed: 10 November 2026).

Docker. (n.d.b) *Multi-stage builds*. Docker Docs. Available at: https://docs.docker.com/build/building/multi-stage/ (Accessed: 7 November 2026).

Docker. (n.d.c) *Personal access tokens*. Docker Docs. Available at: https://docs.docker.com/security/access-tokens/personal-access-tokens/ (Accessed: 10 November 2026).

GitHub. (n.d.a) *About pull requests*. GitHub Docs. Available at: https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests (Accessed: 10 November 2026).

GitHub. (n.d.b) *Using secrets in GitHub Actions*. GitHub Docs. Available at: https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets (Accessed: 10 November 2026).

GitHub. (n.d.c) *Workflows*. GitHub Docs. Available at: https://docs.github.com/en/actions/concepts/workflows-and-actions/workflows (Accessed: 10 November 2026).

Microsoft. (n.d.a) *Azure Queue storage trigger for Azure Functions*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-storage-queue-trigger (Accessed: 10 November 2026).

Microsoft. (n.d.b) *BlobClient Class (Azure.Storage.Blobs)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient (Accessed: 7 November 2026).

Microsoft. (n.d.c) *Configure JWT bearer authentication in ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication (Accessed: 10 November 2026).

Microsoft. (n.d.d) *Create web APIs with ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/ (Accessed: 7 November 2026).

Microsoft. (n.d.e) *Get started with Swashbuckle and ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle (Accessed: 7 November 2026).

Microsoft. (n.d.f) *Handle errors in ASP.NET Core web APIs*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors (Accessed: 7 November 2026).

Microsoft. (n.d.g) *IHttpClientFactory with .NET*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory (Accessed: 7 November 2026).

Microsoft. (n.d.h) *Quickstart: Create a Linux virtual machine in the Azure portal*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal (Accessed: 10 November 2026).

Microsoft. (n.d.i) *Rfc2898DeriveBytes.Pbkdf2 Method*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2 (Accessed: 10 November 2026).

Microsoft. (n.d.j) *Role-based authorization in ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles (Accessed: 7 November 2026).

Microsoft. (n.d.k) *TableClient Class (Azure.Data.Tables)*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient (Accessed: 7 November 2026).

Microsoft. (n.d.l) *Upload files in ASP.NET Core*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads (Accessed: 7 November 2026).

Microsoft. (n.d.m) *Write custom ASP.NET Core middleware*. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write (Accessed: 7 November 2026).

Postman. (n.d.) *Test your API using the Collection Runner*. Postman Docs. Available at: https://learning.postman.com/docs/collections/running-collections/intro-to-collection-runs/ (Accessed: 10 November 2026).

