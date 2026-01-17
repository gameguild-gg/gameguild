using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class TenantCommandsRecordTests
{
    [Fact]
    public void TenantCommands_Should_Assign_Values()
    {
        var tenantId = Guid.NewGuid();
        var updateMetadata = new UpdateTenantMetadataCommand(tenantId, new UpdateTenantMetadataRequest(null, null, null, null, null, null));
        var replaceMetadata = new ReplaceTenantMetadataCommand(tenantId, new ReplaceTenantMetadataRequest(new Dictionary<string, object?>(), new List<string>(), new Dictionary<string, string>(), new UpdateTenantBusinessInfoRequest(null, null, null, null, null), new UpdateTenantContactInfoRequest(null, null, null, null, null, null), null));
        var updateCustomFields = new UpdateTenantCustomFieldsCommand(tenantId, new UpdateTenantCustomFieldsRequest(new Dictionary<string, object?>()));
        var updateTags = new UpdateTenantTagsCommand(tenantId, new UpdateTenantTagsRequest(new List<string>()));
        var replaceTags = new ReplaceTenantTagsCommand(tenantId, new ReplaceTenantTagsRequest(new List<string>()));

        var updateSettings = new UpdateTenantSettingsCommand(tenantId, new UpdateTenantSettingsRequest(null, null, null, null, null, null, null));
        var replaceSettings = new ReplaceTenantSettingsCommand(tenantId, new ReplaceTenantSettingsRequest(
            new UpdateTenantSystemConfigurationRequest(null, null, null, null, null, null),
            new Dictionary<string, bool>(),
            new UpdateTenantBusinessRulesRequest(null, null, null, null),
            new UpdateTenantUiSettingsRequest(null, null, null, null, null),
            new UpdateTenantSecuritySettingsRequest(null, null, null, null, null),
            new UpdateTenantIntegrationSettingsRequest(null, null, null, null),
            new UpdateTenantSystemLimitsRequest(null, null, null, null, null)
        ));
        var updateFeatureFlags = new UpdateTenantFeatureFlagsCommand(tenantId, new UpdateTenantFeatureFlagsRequest(new Dictionary<string, bool>()));
        var updateSystemLimits = new UpdateTenantSystemLimitsCommand(tenantId, new UpdateTenantSystemLimitsRequest(null, null, null, null, null));
        var updateIntegration = new UpdateTenantIntegrationSettingsCommand(tenantId, new UpdateTenantIntegrationSettingsRequest(null, null, null, null));

        updateMetadata.TenantId.Should().Be(tenantId);
        replaceMetadata.TenantId.Should().Be(tenantId);
        updateCustomFields.TenantId.Should().Be(tenantId);
        updateTags.TenantId.Should().Be(tenantId);
        replaceTags.TenantId.Should().Be(tenantId);
        updateSettings.TenantId.Should().Be(tenantId);
        replaceSettings.TenantId.Should().Be(tenantId);
        updateFeatureFlags.TenantId.Should().Be(tenantId);
        updateSystemLimits.TenantId.Should().Be(tenantId);
        updateIntegration.TenantId.Should().Be(tenantId);
    }
}
