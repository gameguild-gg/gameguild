using System.Collections.Concurrent;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     In-memory implementation of Access Control List service for development/testing.
///     WARNING: This is NOT suitable for production - data is lost on restart.
///     For production, use DatabaseAccessControlListService with CachedAccessControlListService wrapper.
///     Supports User, Role, Group, and Anonymous principals with deny-first evaluation.
/// </summary>
public sealed class InMemoryAccessControlListService : IAccessControlListService
{
    private readonly ConcurrentDictionary<AclEntryKey, AclEntryValue> _entries = new();

    /// <inheritdoc />
    public Task<AccessLevel> EvaluateAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var principals = subject.GetPrincipals().ToList();
        var matchingEntries = new List<AclEntryValue>();

        foreach (var (principalType, principalId) in principals)
        {
            var key = new AclEntryKey(tenantId, principalType, principalId, resourceType, resourceId);
            if (_entries.TryGetValue(key, out var entry) && entry.IsActive)
            {
                matchingEntries.Add(entry);
            }
        }

        if (matchingEntries.Count == 0)
            return Task.FromResult(AccessLevel.None);

        // Deny-first algorithm
        var denyEntries = matchingEntries.Where(e => e.IsDenied).ToList();
        var allowEntries = matchingEntries.Where(e => !e.IsDenied).ToList();

        if (denyEntries.Count > 0)
        {
            var highestDeny = denyEntries.Max(e => e.AccessLevel);
            if (highestDeny == AccessLevel.None)
                return Task.FromResult(AccessLevel.None);

            if (allowEntries.Count == 0)
                return Task.FromResult(AccessLevel.None);

            var highestAllow = allowEntries.Max(e => e.AccessLevel);
            return Task.FromResult((AccessLevel)Math.Min((int)highestAllow, (int)highestDeny - 1));
        }

        return Task.FromResult(allowEntries.Count > 0 
            ? allowEntries.Max(e => e.AccessLevel) 
            : AccessLevel.None);
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        AclSubject subject,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default)
    {
        var effectiveLevel = await EvaluateAccessAsync(subject, tenantId, resourceType, resourceId, cancellationToken);
        return effectiveLevel >= requiredLevel;
    }

    /// <inheritdoc />
    public Task<AccessLevel> GetAccessLevelAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var subject = AclSubject.ForUser(userId);
        return EvaluateAccessAsync(subject, tenantId, resourceType, resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public Task GrantAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        var key = new AclEntryKey(tenantId, principalType, principalId, resourceType, resourceId);
        _entries[key] = new AclEntryValue(accessLevel, false, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GrantAccessAsync(
        Guid grantorId,
        Guid granteeId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        return GrantAccessAsync(grantorId, AclPrincipalType.User, granteeId, tenantId, resourceType, resourceId, accessLevel, cancellationToken);
    }

    /// <inheritdoc />
    public Task DenyAccessAsync(
        Guid grantorId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel accessLevel,
        CancellationToken cancellationToken = default)
    {
        var key = new AclEntryKey(tenantId, principalType, principalId, resourceType, resourceId);
        _entries[key] = new AclEntryValue(accessLevel, true, true);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RevokeAccessAsync(
        Guid revokerId,
        AclPrincipalType principalType,
        Guid? principalId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        var key = new AclEntryKey(tenantId, principalType, principalId, resourceType, resourceId);
        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RevokeAccessAsync(
        Guid revokerId,
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        CancellationToken cancellationToken = default)
    {
        return RevokeAccessAsync(revokerId, AclPrincipalType.User, userId, tenantId, resourceType, resourceId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        string resourceId,
        AccessLevel requiredLevel,
        CancellationToken cancellationToken = default)
    {
        var subject = AclSubject.ForUser(userId);
        return await HasAccessAsync(subject, tenantId, resourceType, resourceId, requiredLevel, cancellationToken);
    }

    private readonly record struct AclEntryKey(
        Guid TenantId,
        AclPrincipalType PrincipalType,
        Guid? PrincipalId,
        string ResourceType,
        string ResourceId);

    private readonly record struct AclEntryValue(
        AccessLevel AccessLevel,
        bool IsDenied,
        bool IsActive);
}
