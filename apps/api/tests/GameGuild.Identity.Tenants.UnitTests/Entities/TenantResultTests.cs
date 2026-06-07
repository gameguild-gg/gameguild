using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantResultTests
{
    [Fact]
    public void TenantMembershipResult_Should_Create_Success_And_Failure()
    {
        var success = TenantMembershipResult.Success();
        var failure = TenantMembershipResult.Failure("error");

        success.IsSuccess.Should().BeTrue();
        success.Error.Should().BeNull();
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("error");
    }

    [Fact]
    public void TenantValidationResult_Should_Create_Success_And_Failure()
    {
        var success = TenantValidationResult.Success();
        var failure = TenantValidationResult.Failure(new[] { "invalid" });

        success.IsSuccess.Should().BeTrue();
        success.Errors.Should().BeEmpty();
        failure.IsSuccess.Should().BeFalse();
        failure.Errors.Should().Contain("invalid");
    }

    [Fact]
    public void TenantArchiveResult_Should_Create_Success_And_Failure()
    {
        var success = TenantArchiveResult.Success(5);
        var failure = TenantArchiveResult.Failure("blocked");

        success.IsSuccess.Should().BeTrue();
        success.AffectedMemberCount.Should().Be(5);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("blocked");
    }

    [Fact]
    public void TenantDeleteResult_Should_Create_Success_And_Failure()
    {
        var success = TenantDeleteResult.Success();
        var failure = TenantDeleteResult.Failure("blocked");

        success.IsSuccess.Should().BeTrue();
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("blocked");
    }

    [Fact]
    public void TenantResolutionResult_None_Should_Represent_Missing_Tenant()
    {
        var result = TenantResolutionResult.None;

        result.Tenant.Should().BeNull();
        result.Source.Should().Be(TenantResolutionSource.None);
        result.HasTenant.Should().BeFalse();
    }

    [Fact]
    public void TenantResolutionResult_WithTenant_Should_Report_HasTenant()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Tenant", Slug = "tenant" };
        var result = new TenantResolutionResult(tenant, TenantResolutionSource.Header);

        result.Tenant.Should().Be(tenant);
        result.Source.Should().Be(TenantResolutionSource.Header);
        result.HasTenant.Should().BeTrue();
    }

    [Fact]
    public void Tenantable_IsGlobal_Should_Depened_On_Tenant_Presence()
    {
        ITenantable resource = new FakeTenantable();

        resource.IsGlobal.Should().BeTrue();

        resource.Tenant = new Tenant { Id = Guid.NewGuid(), Name = "Scoped", Slug = "scoped" };

        resource.IsGlobal.Should().BeFalse();
    }

    private sealed class FakeTenantable : ITenantable
    {
        public Tenant? Tenant { get; set; }
    }
}
