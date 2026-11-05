namespace GameGuild.Identity.Tenants.UnitTests;

/// <summary>
///     Test double for <see cref="ITenantAdministrationGuard"/> that allows every
/// mutation, for handler tests that exercise mutation logic rather than the
/// authorization gate (the gate itself has dedicated tests; issue #1287 T1).
/// </summary>
public sealed class AllowAllTenantAdministrationGuard : ITenantAdministrationGuard
{
    public Task EnsureCanMutateAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task EnsureCanPurgeAsync(Guid tenantId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
