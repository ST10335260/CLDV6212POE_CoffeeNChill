using Azure.Data.Tables;

namespace CoffeeNChillFunctions
{
    public static class OrdersTable
    {
        public const string Name = "Orders";

        public static async Task<TableClient> GetClientAsync()
        {
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? "UseDevelopmentStorage=true";

            var tableClient = new TableServiceClient(connectionString).GetTableClient(Name);
            // Creates the Orders table on first use (Microsoft, n.d.d).
            await tableClient.CreateIfNotExistsAsync();
            return tableClient;
        }
    }
}