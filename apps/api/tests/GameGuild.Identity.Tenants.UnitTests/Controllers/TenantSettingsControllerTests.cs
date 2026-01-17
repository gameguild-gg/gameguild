using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class TenantSettingsControllerTests
{
    [Fact]
    public async Task Settings_Endpoints_Should_Return_Expected_Results()
    {
        var controller = new TenantSettingsController();
        var tenantId = Guid.NewGuid();

        var updateRequest = new UpdateTenantSettingsRequest(
            SystemConfiguration: null,
            FeatureFlags: null,
            BusinessRules: null,
            UserInterfaceSettings: null,
            SecuritySettings: null,
            IntegrationSettings: null,
            SystemLimits: null
        );

        var replaceRequest = new ReplaceTenantSettingsRequest(
            new UpdateTenantSystemConfigurationRequest(null, null, null, null, null, null),
            new Dictionary<string, bool>(),
            new UpdateTenantBusinessRulesRequest(null, null, null, null),
            new UpdateTenantUiSettingsRequest(null, null, null, null, null),
            new UpdateTenantSecuritySettingsRequest(null, null, null, null, null),
            new UpdateTenantIntegrationSettingsRequest(null, null, null, null),
            new UpdateTenantSystemLimitsRequest(null, null, null, null, null)
        );

        (await controller.GetSettings(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateSettings(tenantId, updateRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.ReplaceSettings(tenantId, replaceRequest, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetFeatureFlags(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateFeatureFlags(tenantId, new Dictionary<string, bool>(), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetSystemLimits(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateSystemLimits(tenantId, new UpdateTenantSystemLimitsRequest(null, null, null, null, null), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetIntegrationSettings(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateIntegrationSettings(tenantId, new UpdateTenantIntegrationSettingsRequest(null, null, null, null), CancellationToken.None)).Should().BeOfType<NoContentResult>();
    }
}
