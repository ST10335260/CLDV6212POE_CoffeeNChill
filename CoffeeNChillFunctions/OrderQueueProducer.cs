using System.Net;
using System.Text.Json;
using Azure;
using Azure.Storage.Queues;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions
{
    public class OrderQueueProducer
    {
        private const string QueueName = "order-processing-queue";
        private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger<OrderQueueProducer> _logger;

        public OrderQueueProducer(ILogger<OrderQueueProducer> logger)
        {
            _logger = logger;
        }

        [Function("PlaceOrderOnQueue")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "orders/queue")] HttpRequestData req)
        {
            string body = await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Request body is required." });
            }

            OrderMessage? order;
            try
            {
                order = JsonSerializer.Deserialize<OrderMessage>(body, ReadOptions);
            }
            catch (JsonException)
            {
                return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Request body is not valid JSON." });
            }

            var errors = Validate(order);
            if (order is null || errors.Count > 0)
            {
                return await WriteJson(req, HttpStatusCode.BadRequest, new { error = "Order validation failed.", details = errors });
            }

            order.OrderTimestamp ??= DateTimeOffset.UtcNow;
            string payload = JsonSerializer.Serialize(order);

            try
            {
                var queueClient = CreateQueueClient();
                await queueClient.CreateIfNotExistsAsync();
                await queueClient.SendMessageAsync(payload);
            }
            catch (Exception ex) when (ex is RequestFailedException or AggregateException)
            {
                _logger.LogError(ex, "Could not place order {OrderId} on {Queue}", order.OrderId, QueueName);
                return await WriteJson(req, HttpStatusCode.ServiceUnavailable,
                    new { error = "The order queue is unavailable. Please try again shortly." });
            }

            _logger.LogInformation("Order {OrderId} placed on {Queue}", order.OrderId, QueueName);

            return await WriteJson(req, HttpStatusCode.Accepted, new
            {
                message = "Order placed on the queue.",
                orderId = order.OrderId,
                queue = QueueName,
                orderTimestamp = order.OrderTimestamp
            });
        }

        private static QueueClient CreateQueueClient()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            var options = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };
            return new QueueClient(connectionString, QueueName, options);
        }

        private static List<string> Validate(OrderMessage? order)
        {
            var errors = new List<string>();

            if (order is null)
            {
                errors.Add("Order payload is required.");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(order.OrderId))
            {
                errors.Add("OrderId is required.");
            }

            if (string.IsNullOrWhiteSpace(order.CustomerName))
            {
                errors.Add("CustomerName is required.");
            }

            if (order.SelectedItemSKUs is null
                || order.SelectedItemSKUs.Count == 0
                || order.SelectedItemSKUs.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add("SelectedItemSKUs must contain at least one non-empty SKU.");
            }

            if (order.TotalPrice <= 0)
            {
                errors.Add("TotalPrice must be greater than zero.");
            }

            return errors;
        }

        private static async Task<HttpResponseData> WriteJson(HttpRequestData req, HttpStatusCode status, object payload)
        {
            var response = req.CreateResponse(status);
            await response.WriteAsJsonAsync(payload);
            return response;
        }
    }
}