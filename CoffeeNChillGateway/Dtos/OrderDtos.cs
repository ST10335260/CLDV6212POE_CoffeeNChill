using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Dtos;

/// <summary>Request body for placing an order on the order-processing queue.</summary>
public class PlaceOrderRequest
{
    [Required] public string OrderId { get; set; } = string.Empty;

    [Required] public string CustomerName { get; set; } = string.Empty;

    /// <summary>SKUs of the menu items ordered.</summary>
    [Required, MinLength(1)] public List<string> SelectedItemSKUs { get; set; } = new();

    [Range(0.01, 100000)] public double TotalPrice { get; set; }
}

/// <summary>Confirmation returned after an order is placed on the queue.</summary>
public class PlaceOrderResponse
{
    public string Message { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string Queue { get; set; } = string.Empty;
    public DateTimeOffset? OrderTimestamp { get; set; }
}

/// <summary>An order as stored in the Orders table.</summary>
public class OrderResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public List<string> SelectedItemSKUs { get; set; } = new();
    public double TotalPrice { get; set; }
    public DateTimeOffset? OrderTimestamp { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>Request body for changing an order's status.</summary>
public class UpdateOrderStatusRequest
{
    /// <summary>The order's date, for example "2026-11-07". Stored as the PartitionKey.</summary>
    [Required] public string OrderDate { get; set; } = string.Empty;

    [Required] public string OrderId { get; set; } = string.Empty;

    /// <summary>One of Received, Preparing, Ready or Collected.</summary>
    [Required] public string Status { get; set; } = string.Empty;
}

/// <summary>Confirmation returned after an order's status changes.</summary>
public class UpdateOrderStatusResponse
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
