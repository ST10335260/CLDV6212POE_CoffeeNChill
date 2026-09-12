using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillFunctions
{
    // Represents one row in the "MenuItems" Azure Table.
    // PartitionKey = Category (e.g. "Hot Drinks", "Cold Drinks", "Pastries", "Sandwiches")
    // RowKey       = unique item SKU / ID (e.g. "COF-001", "PAS-104")
    public class MenuItemEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = default!;
        public string RowKey { get; set; } = default!;

        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public double Price { get; set; }
        public bool IsAvailable { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

    // DTO used when a client creates a new menu item via POST /api/menu
    public class MenuItemCreateDto
    {
        public string Category { get; set; } = default!; // becomes PartitionKey
        public string Id { get; set; } = default!;        // becomes RowKey
        public string Name { get; set; } = default!;
        public string Description { get; set; } = default!;
        public double Price { get; set; }
        public bool IsAvailable { get; set; } = true;
    }

    // DTO used for partial updates via PUT /api/menu/{category}/{id}
    public class MenuItemUpdateDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public double? Price { get; set; }
        public bool? IsAvailable { get; set; }
    }
}
