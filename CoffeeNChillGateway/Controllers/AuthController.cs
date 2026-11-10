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
    private readonly ITokenService _tokens;

    public AuthController(IUserService users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
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

    /// <summary>Logs in with email and password and returns a signed JWT.</summary>
    /// <response code="200">Login succeeded, token returned.</response>
    /// <response code="401">Email or password is incorrect.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByEmailAsync(request.Email);

        // The same message for an unknown email and a wrong password, so the response does not reveal which accounts exist.
        if (user is null || !_hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { error = "Invalid email or password." });
        }

        var (token, expiresAt) = _tokens.CreateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Name = user.Name,
            Email = user.RowKey,
            Role = user.PartitionKey
        });
    }
}