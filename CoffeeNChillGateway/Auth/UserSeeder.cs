namespace CoffeeNChillGateway.Auth;

public class UserSeeder : IHostedService
{
    private readonly IUserService _users;
    private readonly IPasswordHasher _hasher;
    private readonly IConfiguration _config;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(IUserService users, IPasswordHasher hasher, IConfiguration config, ILogger<UserSeeder> logger)
    {
        _users = users;
        _hasher = hasher;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync("Seed:Staff", UserRoles.Staff, "Demo Staff", "staff@coffeenchill.local");
            await SeedAsync("Seed:Admin", UserRoles.Admin, "Demo Admin", "admin@coffeenchill.local");
        }
        catch (Exception ex)
        {
            
            _logger.LogWarning(ex, "Could not seed demo users. Storage may be unavailable.");
        }
    }

    private async Task SeedAsync(string section, string role, string name, string defaultEmail)
    {
        string? password = _config[$"{section}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No password configured for {Section}, skipping.", section);
            return;
        }

        string email = _config[$"{section}:Email"] ?? defaultEmail;
        if (await _users.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var (hash, salt) = _hasher.HashPassword(password);
        await _users.CreateAsync(name, email, role, hash, salt);
        _logger.LogInformation("Seeded {Role} account {Email}.", role, email);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}