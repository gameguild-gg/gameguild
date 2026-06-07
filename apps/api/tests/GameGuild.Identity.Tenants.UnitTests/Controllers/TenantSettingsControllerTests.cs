using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using GameGuild.CQRS;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Controllers;

public class TenantSettingsControllerTests
{
    [Fact]
    public async Task Settings_Endpoints_Should_Return_Expected_Results()
    {
        var sender = new Mock<ISender>();
        sender.Setup(s => s.Send<TenantSettingsDto?>(It.IsAny<GetTenantSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettingsDto?)null);
        sender.Setup(s => s.Send<Dictionary<string, bool>?>(It.IsAny<GetTenantFeatureFlagsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>());
        sender.Setup(s => s.Send<TenantSystemLimitsDto?>(It.IsAny<GetTenantSystemLimitsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSystemLimitsDto?)null);
        sender.Setup(s => s.Send<TenantIntegrationSettingsDto?>(It.IsAny<GetTenantIntegrationSettingsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantIntegrationSettingsDto?)null);
        sender.Setup(s => s.Send<UpdateTenantSettingsCommand>(It.IsAny<UpdateTenantSettingsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<ReplaceTenantSettingsCommand>(It.IsAny<ReplaceTenantSettingsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<UpdateTenantFeatureFlagsCommand>(It.IsAny<UpdateTenantFeatureFlagsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<UpdateTenantSystemLimitsCommand>(It.IsAny<UpdateTenantSystemLimitsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        sender.Setup(s => s.Send<UpdateTenantIntegrationSettingsCommand>(It.IsAny<UpdateTenantIntegrationSettingsCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new TenantSettingsController(sender.Object);
        var tenantId = Guid.NewGuid();
        var featureFlags = new Dictionary<string, bool> { ["owner_portal"] = true };

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
        (await controller.UpdateFeatureFlags(tenantId, featureFlags, CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetSystemLimits(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateSystemLimits(tenantId, new UpdateTenantSystemLimitsRequest(null, null, null, null, null), CancellationToken.None)).Should().BeOfType<NoContentResult>();
        (await controller.GetIntegrationSettings(tenantId, CancellationToken.None)).Should().BeOfType<OkObjectResult>();
        (await controller.UpdateIntegrationSettings(tenantId, new UpdateTenantIntegrationSettingsRequest(null, null, null, null), CancellationToken.None)).Should().BeOfType<NoContentResult>();

        sender.Verify(
            s => s.Send<UpdateTenantFeatureFlagsCommand>(
                It.Is<UpdateTenantFeatureFlagsCommand>(command => command.Request.FeatureFlags["owner_portal"]),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
