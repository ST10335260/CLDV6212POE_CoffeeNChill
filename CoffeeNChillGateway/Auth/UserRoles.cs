namespace CoffeeNChillGateway.Auth;

/// <summary>Role names used in the Users table, the JWT role claim and [Authorize(Roles = ...)].</summary>
public static class UserRoles
{
    public const string Student = "Student";
    public const string Staff = "Staff";
    public const string Admin = "Admin";

    public static readonly string[] All = { Student, Staff, Admin };
}