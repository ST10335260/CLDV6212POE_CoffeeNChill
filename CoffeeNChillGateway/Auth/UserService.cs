using Azure.Data.Tables;

namespace CoffeeNChillGateway.Auth;

public interface IUserService
{
    Task<UserEntity?> FindByEmailAsync(string email);
    Task<UserEntity> CreateAsync(string name, string email, string role, string passwordHash, string passwordSalt);
}

/// <summary>Reads and writes users in the Users table.</summary>
public class UserService : IUserService
{
    private const string TableName = "Users";
    private readonly TableServiceClient _tables;

    public UserService(TableServiceClient tables)
    {
        _tables = tables;
    }

    private async Task<TableClient> GetTableAsync()
    {
        var table = _tables.GetTableClient(TableName);
        await table.CreateIfNotExistsAsync();
        return table;
    }

    // The role is the PartitionKey, so a lookup by email scans every partition.
    public async Task<UserEntity?> FindByEmailAsync(string email)
    {
        string key = email.Trim().ToLowerInvariant();
        var table = await GetTableAsync();
        await foreach (var user in table.QueryAsync<UserEntity>(u => u.RowKey == key))
        {
            return user;
        }
        return null;
    }

    public async Task<UserEntity> CreateAsync(string name, string email, string role, string passwordHash, string passwordSalt)
    {
        var user = new UserEntity
        {
            PartitionKey = role,
            RowKey = email.Trim().ToLowerInvariant(),
            Name = name.Trim(),
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var table = await GetTableAsync();
        await table.AddEntityAsync(user);
        return user;
    }
}