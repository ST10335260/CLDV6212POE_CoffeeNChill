using Azure;
using Azure.Data.Tables;
using CoffeeNChillGateway.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Orders. Customers can place and track orders; Admin and Staff manage them.</summary>
[Route("api/orders")]
public class OrdersController : GatewayControllerBase
{
    private static readonly string[] ValidStatuses = { "Received", "Preparing", "Ready", "Collected" };

    private readonly IHttpClientFactory _clients;
    private readonly TableServiceClient _tables;

    public OrdersController(IHttpClientFactory clients, TableServiceClient tables)
    {
        _clients = clients;
        _tables = tables;
    }

    // Creates the named client for the Functions app (Microsoft, n.d.e).
    private HttpClient Functions => _clients.CreateClient("functions");

    /// <summary>Places an order on the order-processing queue.</summary>
    [HttpPost("queue")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PlaceOrderResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var response = await Functions.PostAsync("orders/queue", JsonBody(request));
        return await Relay<PlaceOrderResponse, PlaceOrderResponse>(response, r => r);
    }

    /// <summary>Gets every order, optionally filtered by status (Admin or Staff).</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        string path = string.IsNullOrWhiteSpace(status)
            ? "orders"
            : $"orders?status={Uri.EscapeDataString(status)}";
        var response = await Functions.GetAsync(path);
        return await Relay<List<OrderResponse>, List<OrderResponse>>(response, r => r);
    }

    /// <summary>Gets one order so a customer can track its status.</summary>
    [HttpGet("{orderDate}/{orderId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string orderDate, string orderId)
    {
        var response = await Functions.GetAsync(
            $"orders/{Uri.EscapeDataString(orderDate)}/{Uri.EscapeDataString(orderId)}");
        return await Relay<OrderResponse, OrderResponse>(response, r => r);
    }

    /// <summary>Changes an order's status in the Orders table (Admin or Staff).</summary>
    [HttpPut("status")]
    // Only Admin and Staff roles may call this (Microsoft, n.d.f).
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(UpdateOrderStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateOrderStatusRequest request)
    {
        string? status = ValidStatuses.FirstOrDefault(s =>
            string.Equals(s, request.Status, StringComparison.OrdinalIgnoreCase));

        if (status is null)
        {
            return BadRequest(new ProblemDetails
            {
                Status = 400,
                Title = "Invalid status.",
                Detail = $"Status must be one of: {string.Join(", ", ValidStatuses)}."
            });
        }

        // Reads and updates the order directly in the Orders table (Microsoft, n.d.g).
        var table = _tables.GetTableClient("Orders");

        TableEntity entity;
        try
        {
            entity = (await table.GetEntityAsync<TableEntity>(request.OrderDate, request.OrderId)).Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return NotFound(new ProblemDetails
            {
                Status = 404,
                Title = "Order not found.",
                Detail = $"Order '{request.OrderId}' was not found for {request.OrderDate}."
            });
        }

        entity["Status"] = status;
        await table.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Merge);

        return Ok(new UpdateOrderStatusResponse
        {
            OrderId = request.OrderId,
            OrderDate = request.OrderDate,
            Status = status
        });
    }
}
