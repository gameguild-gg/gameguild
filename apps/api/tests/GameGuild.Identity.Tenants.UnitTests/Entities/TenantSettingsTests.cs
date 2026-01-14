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
}
