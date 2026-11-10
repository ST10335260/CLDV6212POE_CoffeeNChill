using System.ComponentModel.DataAnnotations;

namespace CoffeeNChillGateway.Auth;

/// <summary>Request body for registering a new user.</summary>
public class RegisterRequest
{
    /// <summary>Display name.</summary>
    [Required, StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = default!;

    /// <summary>Email address, which also acts as the login name.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    /// <summary>Password, at least 8 characters.</summary>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = default!;
}

/// <summary>Returned after a successful registration. Never includes password data.</summary>
public class RegisterResponse
{
    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
}

/// <summary>Request body for logging in.</summary>
public class LoginRequest
{
    /// <summary>The email address used to register.</summary>
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    /// <summary>The account password.</summary>
    [Required]
    public string Password { get; set; } = default!;
}

/// <summary>Returned after a successful login.</summary>
public class LoginResponse
{
    /// <summary>The signed JWT. Send it as a Bearer token.</summary>
    public string Token { get; set; } = default!;

    /// <summary>When the token expires (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    public string Name { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
}