using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.DTOs;

public class TenantDtoTests
{
    private sealed class TestTenantSubscriptionDto : TenantSubscriptionDto { }

    [Fact]
    public void TenantDto_Should_Assign_Properties()
    {
        var dto = new TenantDto
        {
            Id = Guid.NewGuid(),
            Name = "Tenant",
            Slug = "tenant",
            Description = "desc",
            IsActive = true,
            UsersCount = 5,
            CurrentPlan = new TestTenantSubscriptionDto { PlanName = "Pro" }
        };

        dto.Name.Should().Be("Tenant");
        dto.CurrentPlan.Should().NotBeNull();
    }
}
