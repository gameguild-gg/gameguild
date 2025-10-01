using FluentAssertions;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Entities;

/// <summary>
/// Unit tests for TenantSettings entity
/// </summary>
public class TenantSettingsTests
{
    [Fact]
    public void Constructor_Should_Create_TenantSettings_With_Default_Values()
    {
        // Act
        var tenantSettings = new TenantSettings();

        // Assert
        _ = tenantSettings.TenantId.Should().BeNull();
        _ = tenantSettings.Tenant.Should().BeNull();
        _ = tenantSettings.DefaultLanguageId.Should().BeNull();
        _ = tenantSettings.DefaultLanguage.Should().BeNull();
        _ = tenantSettings.DefaultTimezone.Should().Be("UTC");
        _ = tenantSettings.Id.Should().NotBeEmpty();
        _ = tenantSettings.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        _ = tenantSettings.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void TenantSettings_Should_Allow_Setting_Properties()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        const string timezone = "America/New_York";

        // Act
        var tenantSettings = new TenantSettings
        {
            TenantId = tenantId,
            DefaultLanguageId = languageId,
            DefaultTimezone = timezone
        };

        // Assert
        _ = tenantSettings.TenantId.Should().Be(tenantId);
        _ = tenantSettings.DefaultLanguageId.Should().Be(languageId);
        _ = tenantSettings.DefaultTimezone.Should().Be(timezone);
    }

    [Fact]
    public void TenantSettings_Should_Inherit_From_Resource()
    {
        // Arrange & Act
        var tenantSettings = new TenantSettings();

        // Assert
        _ = tenantSettings.Should().BeAssignableTo<Resource>();
    }

    [Fact]
    public void TenantSettings_Should_Support_Null_TenantId_For_Global_Settings()
    {
        // Act
        var globalSettings = new TenantSettings
        {
            TenantId = null,
            DefaultTimezone = "UTC"
        };

        // Assert
        _ = globalSettings.TenantId.Should().BeNull();
        _ = globalSettings.DefaultTimezone.Should().Be("UTC");
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/New_York")]
    [InlineData("Europe/London")]
    [InlineData("Asia/Tokyo")]
    public void TenantSettings_Should_Accept_Valid_Timezone_Values(string timezone)
    {
        // Act
        var tenantSettings = new TenantSettings
        {
            DefaultTimezone = timezone
        };

        // Assert
        _ = tenantSettings.DefaultTimezone.Should().Be(timezone);
    }

    [Fact]
    public void TenantSettings_Should_Allow_Null_DefaultLanguageId()
    {
        // Act
        var tenantSettings = new TenantSettings
        {
            DefaultLanguageId = null
        };

        // Assert
        _ = tenantSettings.DefaultLanguageId.Should().BeNull();
        _ = tenantSettings.DefaultLanguage.Should().BeNull();
    }
}