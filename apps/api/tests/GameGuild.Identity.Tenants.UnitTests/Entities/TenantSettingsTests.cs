using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

/// <summary>
/// Unit tests for TenantSettings entity
/// </summary>
public class TenantSettingsTests
{
    [Fact]
    public void TenantSettings_Should_Be_Created_With_Valid_Properties()
    {
        // Arrange & Act
        var settings = new TenantSettings
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid()
        };

        // Assert
        settings.Should().NotBeNull();
        settings.TenantId.Should().NotBeEmpty();
    }

    [Fact]
    public void TenantSettings_Partial_Constructor_Should_Map_Properties()
    {
        var tenantId = Guid.NewGuid();

        var settings = new TenantSettings(new { TenantId = tenantId, DefaultLanguage = "pt-BR" });

        settings.TenantId.Should().Be(tenantId);
        settings.DefaultLanguage.Should().Be("pt-BR");
    }

    [Fact]
    public void CreateDefault_Should_Set_Default_Values()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var settings = TenantSettings.CreateDefault(tenantId);

        // Assert
        settings.TenantId.Should().Be(tenantId);
        settings.DefaultLanguage.Should().Be("en-US");
        settings.DefaultTimezone.Should().Be("UTC");
        settings.DefaultCurrency.Should().Be("USD");
        settings.AllowUserRegistration.Should().BeTrue();
        settings.RequireRegistrationApproval.Should().BeFalse();
        settings.RequireTwoFactorAuth.Should().BeFalse();
        settings.EnableAuditLogging.Should().BeTrue();
        settings.EnableApiAccess.Should().BeTrue();
    }

    [Fact]
    public void UpdateLanguageSettings_Should_Update_Fields()
    {
        // Arrange
        var settings = new TenantSettings();

        // Act
        settings.UpdateLanguageSettings("pt-BR", "America/Sao_Paulo", "BRL");

        // Assert
        settings.DefaultLanguage.Should().Be("pt-BR");
        settings.DefaultTimezone.Should().Be("America/Sao_Paulo");
        settings.DefaultCurrency.Should().Be("BRL");
    }

    [Fact]
    public void UpdateSecuritySettings_Should_Update_Fields()
    {
        // Arrange
        var settings = new TenantSettings();

        // Act
        settings.UpdateSecuritySettings(requireTwoFactor: true, requireApproval: true);

        // Assert
        settings.RequireTwoFactorAuth.Should().BeTrue();
        settings.RequireRegistrationApproval.Should().BeTrue();
    }

    [Fact]
    public void UpdateQuotaSettings_Should_Update_Fields()
    {
        // Arrange
        var settings = new TenantSettings();

        // Act
        settings.UpdateQuotaSettings(maxUsers: 100, storageQuota: 1024);

        // Assert
        settings.MaxUsers.Should().Be(100);
        settings.StorageQuota.Should().Be(1024);
    }
}
