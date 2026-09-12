using System.Net;
using System.Text.Json;
using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions
{
    public class MenuFunctions
    {
        private const string TableName = "MenuItems";
        private readonly ILogger<MenuFunctions> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public MenuFunctions(ILogger<MenuFunctions> logger)
        {
            _logger = logger;
        }

        // Builds a TableClient from the AzureWebJobsStorage connection string
        // and makes sure the MenuItems table exists before we use it.
        private static async Task<TableClient> GetTableClientAsync()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            var serviceClient = new TableServiceClient(connectionString);
            var tableClient = serviceClient.GetTableClient(TableName);
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }

        // ---------------------------------------------------------------
        // POST /api/menu  -> CreateMenuItem
        // ---------------------------------------------------------------
        [Function("CreateMenuItem")]
        public async Task<HttpResponseData> CreateMenuItem(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "menu")] HttpRequestData req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();
            MenuItemCreateDto? input;

            try
            {
                input = JsonSerializer.Deserialize<MenuItemCreateDto>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return await BadRequest(req, "Request body is not valid JSON.");
            }

            if (input is null || string.IsNullOrWhiteSpace(input.Category) || string.IsNullOrWhiteSpace(input.Id))
            {
                return await BadRequest(req, "'category' and 'id' are required fields.");
            }

            var entity = new MenuItemEntity
            {
                PartitionKey = input.Category,
                RowKey = input.Id,
                Name = input.Name,
                Description = input.Description,
                Price = input.Price,
                IsAvailable = input.IsAvailable
            };

            var tableClient = await GetTableClientAsync();

            try
            {
                await tableClient.AddEntityAsync(entity);
            }
            catch (RequestFailedException ex) when (ex.Status == 409)
            {
                return await BadRequest(req, "A menu item with that category/id already exists.");
            }

            _logger.LogInformation("Created menu item {Category}/{Id}", entity.PartitionKey, entity.RowKey);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(entity);
            return response;
        }

        // ---------------------------------------------------------------
        // GET /api/menu  -> GetAllMenuItems
        // ---------------------------------------------------------------
        [Function("GetAllMenuItems")]
        public async Task<HttpResponseData> GetAllMenuItems(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu")] HttpRequestData req)
        {
            var tableClient = await GetTableClientAsync();

            var items = new List<MenuItemEntity>();
            await foreach (var entity in tableClient.QueryAsync<MenuItemEntity>())
            {
                items.Add(entity);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

        // ---------------------------------------------------------------
        // GET /api/menu/category/{category}  -> GetMenuItemsByCategory
        // ---------------------------------------------------------------
        [Function("GetMenuItemsByCategory")]
        public async Task<HttpResponseData> GetMenuItemsByCategory(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "menu/category/{category}")] HttpRequestData req,
            string category)
        {
            var tableClient = await GetTableClientAsync();

            var items = new List<MenuItemEntity>();
            await foreach (var entity in tableClient.QueryAsync<MenuItemEntity>(e => e.PartitionKey == category))
            {
                items.Add(entity);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }

        // ---------------------------------------------------------------
        // PUT /api/menu/{category}/{id}  -> UpdateMenuItem
        // ---------------------------------------------------------------
        [Function("UpdateMenuItem")]
        public async Task<HttpResponseData> UpdateMenuItem(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            var tableClient = await GetTableClientAsync();

            Response<MenuItemEntity> existingResponse;
            try
            {
                existingResponse = await tableClient.GetEntityAsync<MenuItemEntity>(category, id);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return await NotFound(req, "Menu item not found.");
            }

            var existing = existingResponse.Value;

            string body = await new StreamReader(req.Body).ReadToEndAsync();
            MenuItemUpdateDto? updateDto;
            try
            {
                updateDto = JsonSerializer.Deserialize<MenuItemUpdateDto>(body, JsonOptions);
            }
            catch (JsonException)
            {
                return await BadRequest(req, "Request body is not valid JSON.");
            }

            if (updateDto is null)
            {
                return await BadRequest(req, "Request body is required.");
            }

            if (updateDto.Price.HasValue) existing.Price = updateDto.Price.Value;
            if (updateDto.IsAvailable.HasValue) existing.IsAvailable = updateDto.IsAvailable.Value;
            if (!string.IsNullOrWhiteSpace(updateDto.Name)) existing.Name = updateDto.Name!;
            if (!string.IsNullOrWhiteSpace(updateDto.Description)) existing.Description = updateDto.Description!;

            await tableClient.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Replace);

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(existing);
            return response;
        }

        // ---------------------------------------------------------------
        // DELETE /api/menu/{category}/{id}  -> DeleteMenuItem
        // ---------------------------------------------------------------
        [Function("DeleteMenuItem")]
        public async Task<HttpResponseData> DeleteMenuItem(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "menu/{category}/{id}")] HttpRequestData req,
            string category, string id)
        {
            var tableClient = await GetTableClientAsync();

            try
            {
                await tableClient.DeleteEntityAsync(category, id);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                return await NotFound(req, "Menu item not found.");
            }

            _logger.LogInformation("Deleted menu item {Category}/{Id}", category, id);
            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        // ---------------------------------------------------------------
        // Small helpers
        // ---------------------------------------------------------------
        private static async Task<HttpResponseData> BadRequest(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            await response.WriteStringAsync(message);
            return response;
        }

        private static async Task<HttpResponseData> NotFound(HttpRequestData req, string message)
        {
            var response = req.CreateResponse(HttpStatusCode.NotFound);
            await response.WriteStringAsync(message);
            return response;
        }
    }
}
