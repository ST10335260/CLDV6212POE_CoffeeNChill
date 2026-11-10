using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Dtos;

/// <summary>Request body for creating a menu item.</summary>
public class CreateMenuItemRequest
{
    /// <summary>Menu category, for example "Hot Drinks". Stored as the PartitionKey.</summary>
    [Required] public string Category { get; set; } = string.Empty;

    /// <summary>Unique item ID, for example "COF-001". Stored as the RowKey.</summary>
    [Required] public string Id { get; set; } = string.Empty;

    [Required] public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [Range(0.01, 100000)] public double Price { get; set; }

    public bool IsAvailable { get; set; } = true;
}

/// <summary>Request body for updating a menu item. Only the fields sent are changed.</summary>
public class UpdateMenuItemRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }

    [Range(0.01, 100000)] public double? Price { get; set; }

    public bool? IsAvailable { get; set; }
}

/// <summary>A menu item returned to clients.</summary>
public class MenuItemResponse
{
    public string Category { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public bool IsAvailable { get; set; }
}
