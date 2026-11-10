using CoffeeNChillGateway.Dtos;

namespace CoffeeNChillGateway.Mapping;

/// <summary>A menu item as the Functions app returns it (a Table Storage entity).</summary>
public class FunctionMenuItem
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Price { get; set; }
    public bool IsAvailable { get; set; }
}

/// <summary>Maps between the gateway DTOs and the Functions app models.</summary>
public static class MenuMapper
{
    public static MenuItemResponse ToResponse(FunctionMenuItem item) => new()
    {
        Category = item.PartitionKey,
        Id = item.RowKey,
        Name = item.Name,
        Description = item.Description,
        Price = item.Price,
        IsAvailable = item.IsAvailable
    };

    public static List<MenuItemResponse> ToResponse(List<FunctionMenuItem> items) =>
        items.Select(ToResponse).ToList();

    public static object ToFunctionCreate(CreateMenuItemRequest request) => new
    {
        category = request.Category,
        id = request.Id,
        name = request.Name,
        description = request.Description,
        price = request.Price,
        isAvailable = request.IsAvailable
    };
}
