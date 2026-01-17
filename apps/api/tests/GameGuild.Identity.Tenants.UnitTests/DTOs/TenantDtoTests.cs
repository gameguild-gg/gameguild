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

    [Fact]
    public void CreateTenantResponse_Should_Assign_Properties()
    {
        var tenantId = Guid.NewGuid();
        var response = new CreateTenantResponse(tenantId, "Test Tenant", "test-tenant");

        response.TenantId.Should().Be(tenantId);
        response.Name.Should().Be("Test Tenant");
        response.Slug.Should().Be("test-tenant");
    }

    [Fact]
    public void DeactivateRequest_Should_Assign_Reason()
    {
        var request = new DeactivateRequest("test reason");

        request.Reason.Should().Be("test reason");
    }

    [Fact]
    public void DeactivateTenantResponse_Should_Assign_Properties()
    {
        var tenantId = Guid.NewGuid();
        var response = new DeactivateTenantResponse { Success = true, Message = "done", TenantId = tenantId };

        response.Success.Should().BeTrue();
        response.Message.Should().Be("done");
        response.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void DeleteTenantRequest_Should_Assign_ConfirmationToken()
    {
        var request = new DeleteTenantRequest("confirm-123");

        request.ConfirmationToken.Should().Be("confirm-123");
    }

    [Fact]
    public void DeleteTenantResponse_Should_Assign_Properties()
    {
        var tenantId = Guid.NewGuid();
        var response = new DeleteTenantResponse { Success = true, Message = "deleted", TenantId = tenantId };

        response.Success.Should().BeTrue();
        response.Message.Should().Be("deleted");
        response.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void PlanChangeRequest_Should_Assign_NewPlanId()
    {
        var planId = Guid.NewGuid();
        var request = new PlanChangeRequest(planId);

        request.NewPlanId.Should().Be(planId);
    }

    [Fact]
    public void TenantValidationWarning_Should_Assign_Properties()
    {
        var warning = new TenantValidationWarning { Field = "Name", Code = "TooShort", Message = "Name is too short" };

        warning.Field.Should().Be("Name");
        warning.Code.Should().Be("TooShort");
        warning.Message.Should().Be("Name is too short");
    }

    [Fact]
    public void TenantFeatureAccessResponse_Should_Assign_Properties()
    {
        var response = new TenantFeatureAccessResponse { HasAccess = true, FeatureKey = "feature-1" };

        response.HasAccess.Should().BeTrue();
        response.FeatureKey.Should().Be("feature-1");
    }

    [Fact]
    public void TenantFeatureAccessResponse_Granted_Should_Create_Access()
    {
        var response = TenantFeatureAccessResponse.Granted("test-feature");

        response.HasAccess.Should().BeTrue();
        response.FeatureKey.Should().Be("test-feature");
    }

    [Fact]
    public void TenantFeatureAccessResponse_Denied_Should_Create_Denial()
    {
        var response = TenantFeatureAccessResponse.Denied("test-feature", "Not allowed");

        response.HasAccess.Should().BeFalse();
        response.FeatureKey.Should().Be("test-feature");
        response.DenialReason.Should().Be("Not allowed");
    }

    [Fact]
    public void TenantAddressDto_Should_Assign_Properties()
    {
        var dto = new TenantAddressDto("123 Main St", "Test City", "TS", "12345", "US");

        dto.Street.Should().Be("123 Main St");
        dto.City.Should().Be("Test City");
        dto.State.Should().Be("TS");
        dto.PostalCode.Should().Be("12345");
        dto.Country.Should().Be("US");
    }

    [Fact]
    public void UpdateTenantAddressRequest_Should_Assign_Properties()
    {
        var request = new UpdateTenantAddressRequest("456 Oak Ave", "New City", "NC", "67890", "CA");

        request.Street.Should().Be("456 Oak Ave");
        request.City.Should().Be("New City");
    }

    [Fact]
    public void UpdateTenantCurrencySettingsRequest_Should_Assign_Properties()
    {
        var request = new UpdateTenantCurrencySettingsRequest("USD", "$#,##0.00", 2);

        request.DefaultCurrency.Should().Be("USD");
        request.DisplayFormat.Should().Be("$#,##0.00");
        request.DecimalPlaces.Should().Be(2);
    }

    [Fact]
    public void UpdateTenantBrandingRequest_Should_Assign_Properties()
    {
        var request = new UpdateTenantBrandingRequest(
            "https://example.com/logo.png",
            "https://example.com/favicon.ico",
            "#FF5733",
            "#33FF57",
            "Test Company"
        );

        request.LogoUrl.Should().Be("https://example.com/logo.png");
        request.PrimaryColor.Should().Be("#FF5733");
        request.SecondaryColor.Should().Be("#33FF57");
    }

    [Fact]
    public void TenantUsageValidationResponse_Should_Assign_Properties()
    {
        var response = new TenantUsageValidationResponse { IsValid = true };

        response.IsValid.Should().BeTrue();
    }

    [Fact]
    public void TenantUsageValidationResponse_Valid_Should_Create_Valid_Response()
    {
        var response = TenantUsageValidationResponse.Valid();

        response.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UsageMetric_Should_Assign_Properties()
    {
        var metric = new TestUsageMetric("api_calls", 100, 1000, "calls");

        metric.Name.Should().Be("api_calls");
        metric.Current.Should().Be(100);
        metric.Limit.Should().Be(1000);
        metric.Unit.Should().Be("calls");
    }

    [Fact]
    public void PagedTenantsResponse_Should_Assign_Properties()
    {
        var tenants = new List<Tenant> { new() { Name = "Test", Slug = "test" } };
        var response = new PagedTenantsResponse(tenants, 1, 1, 10, 1);

        response.Items.Should().HaveCount(1);
        response.TotalItems.Should().Be(1);
        response.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
        response.TotalPages.Should().Be(1);
    }

    // Test helper for abstract UsageMetric record
    private sealed record TestUsageMetric(string Name, decimal Current, decimal Limit, string Unit = "") 
        : UsageMetric(Name, Current, Limit, Unit);
}
