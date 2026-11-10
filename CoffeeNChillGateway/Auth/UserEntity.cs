using Azure;
using Azure.Data.Tables;

namespace CoffeeNChillGateway.Auth;

/// <summary>
/// A registered user in the Users table. PartitionKey is the role and RowKey is the
/// lower-case email address 
/// </summary>
public class UserEntity : ITableEntity
{
	/// <summary>The user's role: Student, Staff or Admin.</summary>
	public string PartitionKey { get; set; } = default!;

	/// <summary>The user's email address in lower case.</summary>
	public string RowKey { get; set; } = default!;

	/// <summary>The user's display name.</summary>
	public string Name { get; set; } = default!;

	/// <summary>Base64 PBKDF2 hash of the password. The plain password is never stored.</summary>
	public string PasswordHash { get; set; } = default!;

	/// <summary>Base64 random salt used for this user's hash.</summary>
	public string PasswordSalt { get; set; } = default!;

	/// <summary>When the account was created.</summary>
	public DateTimeOffset? CreatedAt { get; set; }

	public DateTimeOffset? Timestamp { get; set; }
	public ETag ETag { get; set; }
}