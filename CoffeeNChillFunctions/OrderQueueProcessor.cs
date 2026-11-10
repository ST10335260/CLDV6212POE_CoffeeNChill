using System.Text.Json;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace CoffeeNChillFunctions
{
    public class OrderQueueProcessor
    {
        private static readonly TimeSpan StepDelay = TimeSpan.FromSeconds(5);
        private static readonly JsonSerializerOptions ReadOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger<OrderQueueProcessor> _logger;

        public OrderQueueProcessor(ILogger<OrderQueueProcessor> logger)
        {
            _logger = logger;
        }

        // Queue trigger; messages that keep failing are moved to the poison queue (Microsoft, n.d.a).
        [Function("ProcessOrderQueue")]
        public async Task ProcessOrderQueue(
            [QueueTrigger("order-processing-queue")] string message)
        {
            OrderMessage? order;
            try
            {
                order = JsonSerializer.Deserialize<OrderMessage>(message, ReadOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Order message is not valid JSON and will be retried, then moved to the poison queue: {Message}", message);
                throw;
            }

            if (order is null || string.IsNullOrWhiteSpace(order.OrderId))
            {
                _logger.LogError("Order message has no OrderId and will be retried, then moved to the poison queue: {Message}", message);
                throw new InvalidOperationException("Order message has no OrderId.");
            }

            var orderTimestamp = order.OrderTimestamp ?? DateTimeOffset.UtcNow;
            var tableClient = await OrdersTable.GetClientAsync();

            var entity = new OrderEntity
            {
                PartitionKey = orderTimestamp.UtcDateTime.ToString("yyyy-MM-dd"),
                RowKey = order.OrderId,
                CustomerName = order.CustomerName,
                SelectedItemSKUs = string.Join(",", order.SelectedItemSKUs ?? new List<string>()),
                TotalPrice = order.TotalPrice,
                OrderTimestamp = orderTimestamp,
                Status = OrderStatus.Received
            };

            // Upsert writes the order to the Orders table (Microsoft, n.d.d).
            await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
            _logger.LogInformation("Order {OrderId} saved with status {Status}", entity.RowKey, entity.Status);

            // Timer simulates the order status changes, based on the System.Timers.Timer class (Microsoft, n.d.f).
            var remainingStatuses = new Queue<string>(new[] { OrderStatus.Preparing, OrderStatus.Ready, OrderStatus.Collected });
            // TaskCompletionSource keeps the function running until the last status is saved (Microsoft, n.d.e).
            var finished = new TaskCompletionSource();

            using var statusTimer = new System.Timers.Timer(StepDelay.TotalMilliseconds) { AutoReset = false };
            statusTimer.Elapsed += async (_, _) =>
            {
                try
                {
                    entity.Status = remainingStatuses.Dequeue();
                    await tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace);
                    _logger.LogInformation("Order {OrderId} moved to status {Status}", entity.RowKey, entity.Status);

                    if (remainingStatuses.Count == 0)
                    {
                        finished.TrySetResult();
                    }
                    else
                    {
                        statusTimer.Start();
                    }
                }
                catch (Exception ex)
                {
                    finished.TrySetException(ex);
                }
            };

            statusTimer.Start();
            await finished.Task;
        }

        // Reads messages from the poison queue created by the queue trigger (Microsoft, n.d.a).
        [Function("LogPoisonOrder")]
        public void LogPoisonOrder(
            [QueueTrigger("order-processing-queue-poison")] string message)
        {
            _logger.LogError("Poison order message moved to order-processing-queue-poison: {Message}", message);
        }
    }
}