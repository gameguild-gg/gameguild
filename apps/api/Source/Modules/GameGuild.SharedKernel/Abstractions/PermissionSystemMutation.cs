namespace GameGuild;

/// <summary>
///     Requests a named internal maintenance operation, not an authenticated user identity.
///     The host must validate its real ambient system actor before accepting the scope.
/// </summary>
public sealed record PermissionSystemMutation(string Operation)
{
    /// <summary>The reviewed maintenance operation for automatic permission expiration.</summary>
    public static string ExpirationOperation { get; } = "PermissionExpiration";
}
