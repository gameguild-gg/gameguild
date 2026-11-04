using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Adds or replaces a known-attacker IP blocklist entry (issue #1089).
/// </summary>
public sealed class AddIpBlocklistEntryCommand : ICommand<IpBlocklistMutationResult>
{
    /// <summary>The exact client IP address to blocklist.</summary>
    public string IpAddress { get; init; } = string.Empty;

    /// <summary>Operator note recorded with the entry.</summary>
    public string? Reason { get; init; }

    /// <summary>When the entry stops matching (UTC); <c>null</c> applies the configured default lifetime.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>Identity of the administrator who requested the change (from the authenticated principal).</summary>
    public string? ActingAdmin { get; init; }
}

/// <summary>
///     Removes an IP blocklist entry (issue #1089).
/// </summary>
public sealed class RemoveIpBlocklistEntryCommand : ICommand<IpBlocklistMutationResult>
{
    /// <summary>The IP address whose entry is removed.</summary>
    public string IpAddress { get; init; } = string.Empty;
}

/// <summary>Outcome of a blocklist mutation.</summary>
public sealed class IpBlocklistMutationResult
{
    public bool Success { get; init; }

    public string Message { get; init; } = string.Empty;
}
