using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Services;

/// <summary>
/// Unit tests for tenant settings operations used by tenant configuration services.
/// </summary>
public class TenantSettingsServiceTests
{
    [Fact]
    public void TenantIntegrationSettingsSerializer_ShouldRoundTripAndMergeSettings()
    {
        var current = new TenantIntegrationSettingsDto(
            new Dictionary<string, object?> { ["stripe"] = true },
            new Dictionary<string, object?> { ["billing"] = "https://billing.example.test" },
            new Dictionary<string, string> { ["legacy"] = "keep" },
            new Dictionary<string, object?> { ["provider"] = "saml" });

        var json = TenantIntegrationSettingsSerializer.Serialize(current);
        var deserialized = TenantIntegrationSettingsSerializer.Deserialize(json);

        deserialized.ExternalServices.Should().ContainKey("stripe");
        deserialized.WebhookSettings["billing"]?.ToString().Should().Be("https://billing.example.test");

        var merged = TenantIntegrationSettingsSerializer.Merge(
            deserialized,
            new UpdateTenantIntegrationSettingsRequest(
                null,
                null,
                new Dictionary<string, string> { ["stripe"] = "sk_test" },
                null));

        merged.ExternalServices.Should().ContainKey("stripe");
        merged.ApiKeys.Should().Contain("stripe", "sk_test");
        merged.SsoConfiguration.Should().ContainKey("provider");
    }

    [Fact]
    public async Task UpdateTenantIntegrationSettingsCommandHandler_ShouldPersistMergedSettings()
    {
        var tenantId = Guid.NewGuid();
        var settings = TenantSettings.CreateDefault(tenantId);
        settings.IntegrationSettingsJson = TenantIntegrationSettingsSerializer.Serialize(
            new TenantIntegrationSettingsDto(
                new Dictionary<string, object?> { ["mail"] = "enabled" },
                new Dictionary<string, object?>(),
                new Dictionary<string, string> { ["existing"] = "value" },
                new Dictionary<string, object?>()));

        TenantSettings? persisted = null;
        var repository = new Mock<ITenantSettingsRepository>();
        repository
            .Setup(repo => repo.GetByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);
        repository
            .Setup(repo => repo.UpdateAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()))
            .Callback<TenantSettings, CancellationToken>((updated, _) => persisted = updated)
            .ReturnsAsync((TenantSettings updated, CancellationToken _) => updated);

        var handler = new UpdateTenantIntegrationSettingsCommandHandler(repository.Object);

        await handler.Handle(
            new UpdateTenantIntegrationSettingsCommand(
                tenantId,
                new UpdateTenantIntegrationSettingsRequest(
                    null,
                    new Dictionary<string, object?> { ["productLaunch"] = "https://hooks.example.test" },
                    null,
                    new Dictionary<string, object?> { ["provider"] = "oidc" })),
            CancellationToken.None);

        persisted.Should().NotBeNull();
        var saved = TenantIntegrationSettingsSerializer.Deserialize(persisted!.IntegrationSettingsJson);
        saved.ExternalServices.Should().ContainKey("mail");
        saved.WebhookSettings["productLaunch"]?.ToString().Should().Be("https://hooks.example.test");
        saved.ApiKeys.Should().Contain("existing", "value");
        saved.SsoConfiguration["provider"]?.ToString().Should().Be("oidc");
    }

    [Fact]
    public void TenantIntegrationSettingsSerializer_ShouldReturnEmptySettingsForInvalidJson()
    {
        var settings = TenantIntegrationSettingsSerializer.Deserialize("{not-json");

        settings.ExternalServices.Should().BeEmpty();
        settings.WebhookSettings.Should().BeEmpty();
        settings.ApiKeys.Should().BeEmpty();
        settings.SsoConfiguration.Should().BeEmpty();
    }
}
