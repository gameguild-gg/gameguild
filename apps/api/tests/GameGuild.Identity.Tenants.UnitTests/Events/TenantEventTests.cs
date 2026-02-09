using FluentAssertions;

using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Events;

public class TenantEventTests
{
    [Fact]
    public void TenantEvents_Should_Store_Values()
    {
        var tenantId = Guid.NewGuid();

        var activated = new TenantActivatedEvent(tenantId, "Tenant");
        var deactivated = new TenantDeactivatedEvent(tenantId, "Tenant");
        var updated = new TenantUpdatedEvent(tenantId, "Tenant", "Desc");
        var archived = new TenantArchivedEvent(tenantId, "Reason");
        var restored = new TenantRestoredEvent(tenantId, "Tenant");
        var deleted = new TenantDeletedEvent(tenantId, "Tenant");
        var created = new TenantCreatedEvent(tenantId, "Tenant", "tenant", new EmailAddress("admin@example.com"));
        var memberAdded = new TenantMemberAddedEvent(tenantId, Guid.NewGuid(), "member@example.com", "Member");
        var memberRemoved = new TenantMemberRemovedEvent(tenantId, Guid.NewGuid(), "member@example.com", "left");
        var planChanged = new TenantSubscriptionPlanChangedEvent(tenantId, Guid.NewGuid(), Guid.NewGuid(), true);

        activated.TenantId.Should().Be(tenantId);
        deactivated.TenantId.Should().Be(tenantId);
        updated.Name.Should().Be("Tenant");
        archived.Reason.Should().Be("Reason");
        restored.Name.Should().Be("Tenant");
        deleted.Name.Should().Be("Tenant");
        created.Name.Should().Be("Tenant");
        memberAdded.TenantId.Value.Should().Be(tenantId);
        memberRemoved.TenantId.Value.Should().Be(tenantId);
        planChanged.IsUpgrade.Should().BeTrue();
    }
}
