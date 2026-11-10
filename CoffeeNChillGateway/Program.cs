using System.Reflection;
using System.Security.Claims;
using System.Text;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using CoffeeNChillGateway.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using CoffeeNChillGateway.Auth;

var builder = WebApplication.CreateBuilder(args);

// ---- Settings (user secrets locally, environment variables in Docker) ----
string storageConnection = builder.Configuration["Storage:ConnectionString"] ?? "UseDevelopmentStorage=true";
string functionsBaseUrl = (builder.Configuration["Functions:BaseUrl"] ?? "http://localhost:7071/api/").TrimEnd('/') + "/";
string issuer = builder.Configuration["Jwt:Issuer"] ?? "CoffeeNChillGateway";
string audience = builder.Configuration["Jwt:Audience"] ?? "CoffeeNChillClients";
string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Set Jwt:Key (dotnet user-secrets locally, Jwt__Key in Docker).");
if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Jwt:Key must contain at least 32 bytes.");
}
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

// ---- Backend connections used by the controllers ----
// Named HttpClient that calls the Azure Functions app (menu, order queue, documents).
// Named clients come from IHttpClientFactory (Microsoft, n.d.e).
builder.Services.AddHttpClient("functions", client => client.BaseAddress = new Uri(functionsBaseUrl));
// Direct storage access for the endpoints the Functions app does not have
// (PUT /api/orders/status and DELETE /api/documents/{fileName}).
builder.Services.AddSingleton(new TableServiceClient(storageConnection));
builder.Services.AddSingleton(new BlobServiceClient(storageConnection));

// ---- JWT authentication and role-based authorization ----
// Roles are checked with [Authorize(Roles = ...)] on the controllers (Microsoft, n.d.f).
// Tokens are issued by the auth endpoints (register and login) and validated here.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = issuer,
            ValidateAudience = true, ValidAudience = audience,
            ValidateIssuerSigningKey = true, IssuerSigningKey = signingKey,
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name, RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization();

// ---- Auth services ----
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddHostedService<UserSeeder>();

// Controllers with the [ApiController] behaviour (Microsoft, n.d.b).
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ---- Swagger with XML documentation and a Bearer token box (Microsoft, n.d.c) ----
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CoffeeNChill Gateway", Version = "v1" });

    string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the token only. Swagger adds the Bearer prefix."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

// Catches any unhandled exception and returns one consistent JSON error (Microsoft, n.d.i).
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger stays on in every environment so it can be tested on the VPS.
app.UseSwagger();
app.UseSwaggerUI();

// Order matters: read the token first, then check endpoint permissions.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
