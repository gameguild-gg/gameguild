using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public sealed class TenantSettingsOperationsTests
{
    private readonly Mock<ITenantSettingsRepository> _repository = new();
    private TenantSettings? _storedSettings;

    public TenantSettingsOperationsTests()
    {
        _repository
            .Setup(repository => repository.GetByTenantIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _storedSettings);
        _repository
            .Setup(repository => repository.CreateAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()))
            .Callback<TenantSettings, CancellationToken>((settings, _) => _storedSettings = settings)
            .ReturnsAsync((TenantSettings settings, CancellationToken _) => settings);
        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()))
            .Callback<TenantSettings, CancellationToken>((settings, _) => _storedSettings = settings)
            .ReturnsAsync((TenantSettings settings, CancellationToken _) => settings);
    }

    [Fact]
    public async Task FeatureFlagHandlers_Should_CreateAndReadTenantFlags()
    {
        var tenantId = Guid.NewGuid();
        var updateHandler = new UpdateTenantFeatureFlagsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantFeatureFlagsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantFeatureFlagsCommand(
                tenantId,
                new UpdateTenantFeatureFlagsRequest(new Dictionary<string, bool> { ["owner_portal"] = true })),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantFeatureFlagsQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!["owner_portal"].Should().BeTrue();
        _repository.Verify(repository => repository.CreateAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(repository => repository.UpdateAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SystemLimitHandlers_Should_UpdateScalarAndCustomLimits()
    {
        var tenantId = Guid.NewGuid();
        var updateHandler = new UpdateTenantSystemLimitsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantSystemLimitsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantSystemLimitsCommand(
                tenantId,
                new UpdateTenantSystemLimitsRequest(
                    MaxUsers: 25,
                    MaxStorage: 4096,
                    MaxApiCalls: 50000,
                    MaxProjects: 8,
                    CustomLimits: new Dictionary<string, int> { ["listing_exports"] = 250 })),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantSystemLimitsQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.MaxUsers.Should().Be(25);
        result.MaxStorage.Should().Be(4096);
        result.MaxApiCalls.Should().Be(50000);
        result.MaxProjects.Should().Be(8);
        result.CustomLimits["listing_exports"].Should().Be(250);
    }

    [Fact]
    public async Task IntegrationSettingsHandlers_Should_MergeAndReadExternalServices()
    {
        var tenantId = Guid.NewGuid();
        var updateHandler = new UpdateTenantIntegrationSettingsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantIntegrationSettingsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantIntegrationSettingsCommand(
                tenantId,
                new UpdateTenantIntegrationSettingsRequest(
                    ExternalServices: new Dictionary<string, object?> { ["crm"] = "hubspot" },
                    WebhookSettings: null,
                    ApiKeys: new Dictionary<string, string> { ["crm"] = "key-ref" },
                    SsoConfiguration: null)),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantIntegrationSettingsQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ExternalServices.Should().ContainKey("crm");
        result.ApiKeys["crm"].Should().Be("key-ref");
    }

    [Fact]
    public async Task SettingsHandlers_Should_UpdateReplaceAndReadCompleteSettings()
    {
        var tenantId = Guid.NewGuid();
        _storedSettings = TenantSettings.CreateDefault(tenantId);
        _storedSettings.CreatedAt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
        _storedSettings.UpdatedAt = new DateTime(2024, 2, 3, 4, 5, 6, DateTimeKind.Utc);

        var updateHandler = new UpdateTenantSettingsCommandHandler(_repository.Object);
        var replaceHandler = new ReplaceTenantSettingsCommandHandler(_repository.Object);
        var queryHandler = new GetTenantSettingsQueryHandler(_repository.Object);

        await updateHandler.Handle(
            new UpdateTenantSettingsCommand(
                tenantId,
                new UpdateTenantSettingsRequest(
                    new UpdateTenantSystemConfigurationRequest(
                        "America/New_York",
                        "en-US",
                        "MM/dd/yyyy",
                        "N0",
                        new UpdateTenantCurrencySettingsRequest("USD", "{0:$0.00}", 2),
                        new Dictionary<string, object?> { ["calendar"] = "gregorian" }),
                    new Dictionary<string, bool> { ["ai_reports"] = true },
                    new UpdateTenantBusinessRulesRequest(
                        new Dictionary<string, object?> { ["workflow"] = "fast" },
                        new Dictionary<string, object?> { ["required"] = true },
                        new Dictionary<string, object?> { ["manager"] = "required" },
                        new Dictionary<string, object?> { ["email"] = true }),
                    new UpdateTenantUiSettingsRequest(
                        "dark",
                        new Dictionary<string, object?> { ["density"] = "compact" },
                        new UpdateTenantBrandingRequest("logo.svg", "favicon.ico", "#111111", "#eeeeee", "GameGuild"),
                        ".app{color:#111}",
                        new Dictionary<string, object?> { ["sidebar"] = "expanded" }),
                    new UpdateTenantSecuritySettingsRequest(
                        new Dictionary<string, object?> { ["minLength"] = 12 },
                        7200,
                        true,
                        new List<string> { "127.0.0.1" },
                        new Dictionary<string, int> { ["api"] = 1000 }),
                    new UpdateTenantIntegrationSettingsRequest(
                        new Dictionary<string, object?> { ["crm"] = "hubspot" },
                        new Dictionary<string, object?> { ["lead"] = "https://example.test/hook" },
                        new Dictionary<string, string> { ["crm"] = "secret-ref" },
                        new Dictionary<string, object?> { ["provider"] = "saml" }),
                    new UpdateTenantSystemLimitsRequest(50, 2048, 25000, 10, new Dictionary<string, int> { ["exports"] = 100 }))),
            CancellationToken.None);

        await updateHandler.Handle(
            new UpdateTenantSettingsCommand(
                tenantId,
                new UpdateTenantSettingsRequest(
                    new UpdateTenantSystemConfigurationRequest(
                        " ",
                        "",
                        null,
                        null,
                        new UpdateTenantCurrencySettingsRequest(null, null, null),
                        null),
                    null,
                    new UpdateTenantBusinessRulesRequest(null, null, null, null),
                    new UpdateTenantUiSettingsRequest(null, null, new UpdateTenantBrandingRequest(null, null, null, null, null), null, null),
                    new UpdateTenantSecuritySettingsRequest(null, null, null, null, null),
                    null,
                    new UpdateTenantSystemLimitsRequest(null, null, null, null, null))),
            CancellationToken.None);

        await updateHandler.Handle(
            new UpdateTenantSettingsCommand(
                tenantId,
                new UpdateTenantSettingsRequest(
                    new UpdateTenantSystemConfigurationRequest(null, null, null, null, null, null),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null)),
            CancellationToken.None);

        await replaceHandler.Handle(
            new ReplaceTenantSettingsCommand(
                tenantId,
                new ReplaceTenantSettingsRequest(
                    new UpdateTenantSystemConfigurationRequest(
                        "Europe/London",
                        "en-GB",
                        "dd/MM/yyyy",
                        "N2",
                        new UpdateTenantCurrencySettingsRequest("GBP", "GBP {0:N2}", 2),
                        new Dictionary<string, object?> { ["weekStart"] = "Monday" }),
                    new Dictionary<string, bool> { ["owner_portal"] = true },
                    new UpdateTenantBusinessRulesRequest(null, null, null, null),
                    new UpdateTenantUiSettingsRequest(
                        "light",
                        new Dictionary<string, object?> { ["density"] = "comfortable" },
                        null,
                        null,
                        new Dictionary<string, object?> { ["cards"] = "standard" }),
                    new UpdateTenantSecuritySettingsRequest(null, null, null, null, null),
                    new UpdateTenantIntegrationSettingsRequest(null, null, null, null),
                    new UpdateTenantSystemLimitsRequest(75, 4096, 50000, 25, new Dictionary<string, int> { ["imports"] = 250 }))),
            CancellationToken.None);

        var result = await queryHandler.Handle(new GetTenantSettingsQuery(tenantId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.SystemConfiguration.TimeZone.Should().Be("Europe/London");
        result.SystemConfiguration.Locale.Should().Be("en-GB");
        result.SystemConfiguration.CurrencySettings.DefaultCurrency.Should().Be("GBP");
        result.SystemConfiguration.CustomConfiguration.Should().ContainKey("weekStart");
        result.FeatureFlags["owner_portal"].Should().BeTrue();
        result.BusinessRules.WorkflowRules.Should().BeEmpty();
        result.UserInterfaceSettings.Theme.Should().Be("light");
        result.UserInterfaceSettings.Branding.LogoUrl.Should().BeNull();
        result.UserInterfaceSettings.ComponentSettings.Should().ContainKey("cards");
        result.SecuritySettings.SessionTimeout.Should().Be(3600);
        result.IntegrationSettings.ExternalServices.Should().BeEmpty();
        result.SystemLimits.MaxUsers.Should().Be(75);
        result.SystemLimits.MaxStorage.Should().Be(4096);
        result.SystemLimits.MaxApiCalls.Should().Be(50000);
        result.SystemLimits.MaxProjects.Should().Be(25);
        result.SystemLimits.CustomLimits["imports"].Should().Be(250);
        result.CreatedAt.Offset.Should().Be(TimeSpan.Zero);
        result.UpdatedAt.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task SettingsHandlers_Should_ReturnDefaultsForMissingAndInvalidJson()
    {
        var tenantId = Guid.NewGuid();
        var queryHandler = new GetTenantSettingsQueryHandler(_repository.Object);

        var missing = await queryHandler.Handle(new GetTenantSettingsQuery(tenantId), CancellationToken.None);

        missing.Should().NotBeNull();
        missing!.Id.Should().Be(tenantId);
        missing.UserInterfaceSettings.Theme.Should().Be("default");
        missing.SecuritySettings.SessionTimeout.Should().Be(3600);
        missing.SystemLimits.MaxApiCalls.Should().Be(10000);

        _storedSettings = TenantSettings.CreateDefault(tenantId);
        _storedSettings.NotificationSettings = "{bad";
        _storedSettings.BrandingSettings = "null";
        _storedSettings.SecuritySettings = "{bad";

        var invalid = await queryHandler.Handle(new GetTenantSettingsQuery(tenantId), CancellationToken.None);

        invalid.Should().NotBeNull();
        invalid!.FeatureFlags.Should().BeEmpty();
        invalid.UserInterfaceSettings.Theme.Should().Be("default");
        invalid.SecuritySettings.ApiRateLimits.Should().BeEmpty();
    }
}
