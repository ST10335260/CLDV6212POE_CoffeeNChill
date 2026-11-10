using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CoffeeNChillGateway.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(UserEntity user);
}

/// <summary>
/// Issues signed JWTs carrying the user's name, email and role. The issuer, audience and key
/// are the same values Program.cs uses to validate tokens.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(UserEntity user)
    {
        string issuer = _config["Jwt:Issuer"] ?? "CoffeeNChillGateway";
        string audience = _config["Jwt:Audience"] ?? "CoffeeNChillClients";
        string key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        int minutes = int.TryParse(_config["Jwt:ExpiryMinutes"], out int m) ? m : 60;

        DateTime expires = DateTime.UtcNow.AddMinutes(minutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.RowKey),
                new Claim(ClaimTypes.Role, user.PartitionKey)
            }),
            Issuer = issuer,
            Audience = audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expires);
    }
}