using CoffeeNChillGateway.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeNChillGateway.Controllers;

/// <summary>Registration and login.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserService _users;
    private readonly IPasswordHasher _hasher;

    public AuthController(IUserService users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    /// <summary>Registers a new user. Every new account is created with the Student role.</summary>
    /// <response code="201">User created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="409">That email is already registered.</response>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (await _users.FindByEmailAsync(request.Email) is not null)
        {
            return Conflict(new { error = "That email is already registered." });
        }

        var (hash, salt) = _hasher.HashPassword(request.Password);
        var user = await _users.CreateAsync(request.Name, request.Email, UserRoles.Student, hash, salt);

        return StatusCode(StatusCodes.Status201Created,
            new RegisterResponse { Name = user.Name, Email = user.RowKey, Role = user.PartitionKey });
    }
}