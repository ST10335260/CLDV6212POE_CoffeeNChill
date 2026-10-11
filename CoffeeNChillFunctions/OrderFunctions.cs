using System.Net;
using Azure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace CoffeeNChillFunctions
{
    public class OrderFunctions
    {
        [Function("GetAllOrders")]
        public async Task<HttpResponseData> GetAllOrders(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders")] HttpRequestData req)
        {
            string? statusFilter = System.Web.HttpUtility.ParseQueryString(req.Url.Query)["status"];
            var tableClient = await OrdersTable.GetClientAsync();

            var orders = new List<object>();
            await foreach (var entity in tableClient.QueryAsync<OrderEntity>())
            {
                if (!string.IsNullOrWhiteSpace(statusFilter)
                    && !string.Equals(entity.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                orders.Add(ToResponse(entity));
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(orders, HttpStatusCode.OK);
            return response;
        }

        [Function("GetOrderById")]
        public async Task<HttpResponseData> GetOrderById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "orders/{orderDate}/{orderId}")] HttpRequestData req,
            string orderDate, string orderId)
        {
            var tableClient = await OrdersTable.GetClientAsync();

            try
            {
                var result = await tableClient.GetEntityAsync<OrderEntity>(orderDate, orderId);
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(ToResponse(result.Value), HttpStatusCode.OK);
                return response;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
                var notFound = req.CreateResponse(HttpStatusCode.NotFound);
                await notFound.WriteAsJsonAsync(
                    new { error = $"Order '{orderId}' was not found for {orderDate}." },
                    HttpStatusCode.NotFound);
                return notFound;
            }
        }

        private static object ToResponse(OrderEntity entity) => new
        {
            orderId = entity.RowKey,
            orderDate = entity.PartitionKey,
            customerName = entity.CustomerName,
            selectedItemSKUs = entity.SelectedItemSKUs.Split(',', StringSplitOptions.RemoveEmptyEntries),
            totalPrice = entity.TotalPrice,
            orderTimestamp = entity.OrderTimestamp,
            status = entity.Status
        };
    }
}