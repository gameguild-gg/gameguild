using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Utilities;

public class TenantIntegrationSettingsSerializerTests
{
    [Fact]
    public void Deserialize_Should_Return_Empty_For_Whitespace_Invalid_Json_And_Null_Literal()
    {
        TenantIntegrationSettingsSerializer.Deserialize("   ").Should().BeEquivalentTo(TenantIntegrationSettingsSerializer.Empty());
        TenantIntegrationSettingsSerializer.Deserialize("{invalid").Should().BeEquivalentTo(TenantIntegrationSettingsSerializer.Empty());
        TenantIntegrationSettingsSerializer.Deserialize("null").Should().BeEquivalentTo(TenantIntegrationSettingsSerializer.Empty());
    }

    [Fact]
    public void Serialize_And_Deserialize_Should_Roundtrip_Settings()
    {
        var settings = new TenantIntegrationSettingsDto(
            new Dictionary<string, object?> { ["slack"] = "enabled" },
            new Dictionary<string, object?> { ["url"] = "https://hooks.example.com" },
            new Dictionary<string, string> { ["maps"] = "secret" },
            new Dictionary<string, object?> { ["provider"] = "entra" });

        var json = TenantIntegrationSettingsSerializer.Serialize(settings);
        var roundtrip = TenantIntegrationSettingsSerializer.Deserialize(json);

        roundtrip.ApiKeys.Should().ContainKey("maps").WhoseValue.Should().Be("secret");
        roundtrip.ExternalServices.Should().ContainKey("slack");
        roundtrip.WebhookSettings.Should().ContainKey("url");
        roundtrip.SsoConfiguration.Should().ContainKey("provider");
    }

    [Fact]
    public void Merge_Should_Preserve_Current_Values_When_Update_Values_Are_Null()
    {
        var current = new TenantIntegrationSettingsDto(
            new Dictionary<string, object?> { ["crm"] = "hubspot" },
            new Dictionary<string, object?> { ["url"] = "https://old.example.com" },
            new Dictionary<string, string> { ["service"] = "existing-key" },
            new Dictionary<string, object?> { ["provider"] = "okta" });

        var update = new UpdateTenantIntegrationSettingsRequest(
            null,
            new Dictionary<string, object?> { ["url"] = "https://new.example.com" },
            null,
            null);

        var merged = TenantIntegrationSettingsSerializer.Merge(current, update);

        merged.ExternalServices.Should().BeSameAs(current.ExternalServices);
        merged.ApiKeys.Should().BeSameAs(current.ApiKeys);
        merged.SsoConfiguration.Should().BeSameAs(current.SsoConfiguration);
        merged.WebhookSettings.Should().ContainKey("url");
        merged.WebhookSettings["url"].Should().Be("https://new.example.com");
    }
}