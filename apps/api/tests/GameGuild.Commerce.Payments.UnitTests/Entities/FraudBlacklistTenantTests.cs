using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

public sealed class FraudBlacklistTenantTests
{
    [Fact]
    public void Entry_ExposesTheSameTenantToSharedAuditAndPaymentServices()
    {
        var tenantId = Guid.NewGuid();
        var entry = FraudBlacklistEntry.Create(tenantId, default, "test-pattern", null, false, default)!;

        entry.TenantId.Should().Be(tenantId);
        ((EntityBase)entry).TenantId.Should().Be(tenantId);
        entry.IsGlobal.Should().BeFalse();

        var reassignedTenantId = Guid.NewGuid();
        entry.SetTenantId(reassignedTenantId);
        entry.TenantId.Should().Be(reassignedTenantId);
    }

    [Fact]
    public void Entry_RejectsAnEmptyTenant()
    {
        FraudBlacklistEntry.Create(Guid.Empty, default, "test-pattern", null, false, default).Should().BeNull();
    }
}
