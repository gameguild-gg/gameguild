using FluentAssertions;

using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Events;

public class TenantEventTests
{
    /// <summary>
    ///     Issue #1287 T8: the seven discarded-publisher events (Activated, Deactivated,
    ///     Updated, Archived, Restored, MemberAdded, MemberRemoved) were deleted along
    ///     with their raises — the persistence pipeline collected and cleared them
    ///     without dispatching (#1202 precedent). The three never-constructed events
    ///     remain baselined by the G4 wiring-gap guard (issue #1184) and keep their
    ///     construction test here.
    /// </summary>
    [Fact]
    public void TenantEvents_Should_Store_Values()
    {
        var tenantId = Guid.NewGuid();

        var deleted = new TenantDeletedEvent(tenantId, "Tenant");
        var created = new TenantCreatedEvent(tenantId, "Tenant", "tenant", new EmailAddress("admin@example.com"));
        var planChanged = new TenantSubscriptionPlanChangedEvent(tenantId, Guid.NewGuid(), Guid.NewGuid(), true);

        deleted.Name.Should().Be("Tenant");
        created.Name.Should().Be("Tenant");
        planChanged.IsUpgrade.Should().BeTrue();
    }
}
