CoffeeNChill — Part 3 (CLDV6212/W, Cloud Development B)

CoffeeNChill is a coffee shop back end built with Azure Functions, Azure Storage and an ASP.NET Core Web API gateway.
Part 3 adds the gateway with Swagger documentation, JWT authentication with role-based access and a production deployment on a Linux VPS.

	
Live URL : http://102.133.167.157:5000/swagger
Demo video :
Group	CLDV6212 Group 13: Reesaido Alagiry (ST10335260), Thato Gumede (ST10275159)



1. Architecture













Linux VPS, Docker Compose
HTTP + Bearer JWT
HttpClient
Users and Orders tables,staff-docs blob
Client: Swagger UI orPostman
Gateway: ASP.NET CoreWeb API
Functions app: menu, orderqueue, documents
Azure Storage or Azurite:Tables, Blobs, Queues

The gateway is the only entry point for clients. It validates the JWT, checks the caller's role, maps request and response DTOs, and forwards most calls to the Functions app through a named HttpClient created by IHttpClientFactory (Microsoft, n.d.e). Two endpoints are handled directly against storage because the Functions app has no equivalent: PUT /api/orders/status and DELETE /api/documents/{fileName}. The Users table is also accessed directly by the gateway.

Order flow. A customer places an order through POST /api/orders/queue. The producer function validates it, encodes the message as Base64 and puts it on the order-processing-queue queue, then responds immediately. A queue-triggered function picks the message up in the background and writes it to the Orders table, and a System.Timers.Timer moves the order through Received, Preparing, Ready and Collected.

2. Changes from Part 2
API gateway (ASP.NET Core Web API) with controllers for menu, orders and documents (Microsoft, n.d.b), DTOs and mapping, and Swagger UI with XML documentation (Microsoft, n.d.c).
Global exception middleware that turns any unhandled exception into one consistent JSON error (Microsoft, n.d.d; Microsoft, n.d.i).
JWT authentication and user profiles: a Users table, registration with hashed passwords, login that issues a signed JWT, and role-based access control (Microsoft, n.d.f).
Docker Compose updated to run the gateway alongside the Functions app and Azurite, using version-tagged images pulled from Docker Hub (Docker, n.d.).
Production deployment on a Linux VPS with a public URL, started with docker-compose up -d. [TODO: confirm with Reesaido, add the CI/CD workflow .github/workflows/deploy.yml here if it was completed.]
Menu endpoints in the Functions app now use anonymous authorization, because access control is enforced by the gateway.
Poison message handling is documented in section 7, following the Part 2 feedback.
3. Endpoints and access
Method	Route	Access
POST	/api/auth/register	Public
POST	/api/auth/login	Public
GET	/api/menu	Public
GET	/api/menu/category/{category}	Public
POST	/api/menu	Admin, Staff
PUT	/api/menu/{category}/{id}	Admin, Staff
DELETE	/api/menu/{category}/{id}	Admin, Staff
POST	/api/orders/queue	Public
GET	/api/orders/{orderDate}/{orderId}	Public
GET	/api/orders	Admin, Staff
PUT	/api/orders/status	Admin, Staff
POST	/api/documents	Admin, Staff
GET	/api/documents	Admin, Staff
GET	/api/documents/{fileName}	Admin, Staff
DELETE	/api/documents/{fileName}	Admin, Staff

Roles are enforced with [Authorize(Roles = "Admin,Staff")] on the controllers (Microsoft, n.d.f). A request with no token receives 401, and a valid token with the wrong role receives 403. The document endpoints use BlobClient for deletion (Microsoft, n.d.a) and IFormFile for uploads (Microsoft, n.d.h). Order status updates read and write the Orders table through TableClient (Microsoft, n.d.g).

4. Authentication and user profiles
Users table. PartitionKey is the role (Student, Staff or Admin) and RowKey is the lower-case email address. Each row also stores the name, a password hash, the salt and the creation date.
Registration. POST /api/auth/register validates the input and creates the account with the Student role, so a caller cannot choose a higher role. Passwords are hashed with PBKDF2 (HMAC-SHA256) and a random salt per user, and the plain password is never stored. A duplicate email returns 409.
Staff and Admin accounts. A start-up seeder creates one demo Staff and one demo Admin account if they do not exist. Their passwords come from configuration, never from the repository.
Login. POST /api/auth/login verifies the password and returns a signed JWT containing the user's name, email and role claims and an expiry time. Unknown email and wrong password return the same 401 message, so the response does not reveal which accounts exist.
Design trade-off. Using the role as PartitionKey means a login lookup by email scans every partition. This is acceptable for a small user table and matches the required schema.
5. Configuration

Secrets are never committed. Locally they live in user secrets, and in Docker they are passed as environment variables, where : becomes __ (for example Jwt__Key).

Setting	Purpose
Jwt:Key	Signing key, required, at least 32 bytes
Jwt:Issuer, Jwt:Audience	Token issuer and audience, with defaults
Jwt:ExpiryMinutes	Token lifetime in minutes, default 60
Storage:ConnectionString	Azure Storage or Azurite connection string
Functions:BaseUrl	Address of the Functions app
Seed:Staff:Password, Seed:Admin:Password	Passwords for the seeded demo accounts
6. Running the project

Locally

Start Azurite: docker run -d --name azurite -p 10000:10000 -p 10001:10001 -p 10002:10002 mcr.microsoft.com/azure-storage/azurite
Start the Functions app: cd CoffeeNChillFunctions, then func start.
Set the gateway secrets from the repository root:
dotnet user-secrets init --project CoffeeNChillGateway
dotnet user-secrets set "Jwt:Key" "<random string of at least 32 characters>" --project CoffeeNChillGateway
dotnet user-secrets set "Seed:Staff:Password" "<staff password>" --project CoffeeNChillGateway
dotnet user-secrets set "Seed:Admin:Password" "<admin password>" --project CoffeeNChillGateway
Run the gateway: dotnet run --project CoffeeNChillGateway, then open the address it prints with /swagger added.

With Docker Compose

Set the settings from section 5 as environment variables, then run docker-compose up -d from the repository root. [TODO: confirm the variable names and the gateway port against the final docker-compose.yml.]

Testing in Swagger

Register with POST /api/auth/register, then log in with POST /api/auth/login.
Click Authorize and paste the token only, because Swagger adds the Bearer prefix.
Call a protected endpoint. A Student token returns 403 on staff endpoints, and a Staff or Admin token succeeds.

Testing in Postman

Import docs/CoffeeNChill_Part3.postman_collection.json. Set the staffPassword and adminPassword variables as current values, and set baseUrl to the gateway address. Attach a small PDF to the "Upload document" request, then run the folders in order. Each role folder starts with a login request that stores the token in {{token}}, and the following requests send it as a Bearer token.

7. Poison message handling

Queue messages are Base64 encoded. The Functions host retries a message that fails processing up to the configured dequeue threshold, then moves it to the poison queue, where LogPoisonOrder records it so that one malformed message cannot block the queue. [TODO: confirm with Reesaido that the log entry includes the order ID and the failure reason, and describe it here.]

8. Repository structure
.github/workflows/deploy.yml   CI/CD workflow
CoffeeNChillFunctions/         Azure Functions app and Dockerfile
CoffeeNChillGateway/           ASP.NET Core Web API gateway
  Auth/                        user entity, password hashing, token service, seeder
  Controllers/                 auth, menu, orders and documents controllers
  Dtos/                        request and response models
  Mapping/                     DTO mapping
  Middleware/                  global exception handling
docs/                          Postman collections
docker-compose.yml             full stack
README.md

[TODO: check this tree against the final repository before submitting.]

9. Team contributions

Reesaido Alagiry: API gateway, Swagger with XML documentation, DTOs and mapping, exception middleware, menu, orders and documents controllers, gateway Dockerfile, Docker Compose, Docker Hub images, VPS deployment and the CI/CD workflow.

Thato Gumede: Users table, registration and login, JWT issuing, role-based access checks, demo account seeding, Postman collection, this README and the demo video.

10. AI tool use

Parts of the authentication code (user entity, password hashing, token service, seeder and auth controller), the Part 3 Postman collection and this README were drafted with AI assistance (Claude, Anthropic), then reviewed, tested and adapted by Thato Gumede. AI-assisted code carries a disclosure comment in the source.

References

Docker. (n.d.) Multi-stage builds. Docker Docs. Available at: https://docs.docker.com/build/building/multi-stage/ (Accessed: 7 November 2026).

Microsoft. (n.d.a) BlobClient Class (Azure.Storage.Blobs). Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.storage.blobs.blobclient (Accessed: 7 November 2026).

Microsoft. (n.d.b) Create web APIs with ASP.NET Core. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/ (Accessed: 7 November 2026).

Microsoft. (n.d.c) Get started with Swashbuckle and ASP.NET Core. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle (Accessed: 7 November 2026).

Microsoft. (n.d.d) Handle errors in ASP.NET Core web APIs. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors (Accessed: 7 November 2026).

Microsoft. (n.d.e) IHttpClientFactory with .NET. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory (Accessed: 7 November 2026).

Microsoft. (n.d.f) Role-based authorization in ASP.NET Core. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles (Accessed: 7 November 2026).

Microsoft. (n.d.g) TableClient Class (Azure.Data.Tables). Microsoft Learn. Available at: https://learn.microsoft.com/en-us/dotnet/api/azure.data.tables.tableclient (Accessed: 7 November 2026).

Microsoft. (n.d.h) Upload files in ASP.NET Core. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads (Accessed: 7 November 2026).

Microsoft. (n.d.i) Write custom ASP.NET Core middleware. Microsoft Learn. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write (Accessed: 7 November 2026).