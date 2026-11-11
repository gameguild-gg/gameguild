using FluentAssertions;

using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Events;

public class TenantEventTests
{
    /// <summary>
    ///     Issue #1287 T8: the seven discarded-publisher events (Activated, Deactivated,
    ///     Updated, Archived, Restored, MemberAdded, MemberRemoved) were deleted along
    ///     with their raises — the persistence pipeline collected and cleared them
    ///     without dispatching (#1202 precedent). Issue #1184 G4 then wired the two
    ///     remaining events to their producing commands (CreateTenant/DeleteTenant) and
    ///     removed TenantSubscriptionPlanChangedEvent outright — no runtime flow changes a
    ///     tenant's subscription plan, so the event was dead machinery with no producer.
    /// </summary>
    [Fact]
    public void TenantEvents_Should_Store_Values()
    {
        var tenantId = Guid.NewGuid();

        var deleted = new TenantDeletedEvent(tenantId, "Tenant");
        var created = new TenantCreatedEvent(tenantId, "Tenant", "tenant", new EmailAddress("admin@example.com"));

        deleted.Name.Should().Be("Tenant");
        created.Name.Should().Be("Tenant");
    }
}
