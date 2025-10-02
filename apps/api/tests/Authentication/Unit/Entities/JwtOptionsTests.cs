using FluentAssertions;
using GameGuild.Modules.Authentication;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Entities;

/// <summary>
/// Unit tests for the JwtOptions configuration
/// Tests the properties and behavior of JWT configuration options
/// </summary>
public class JwtOptionsTests
{
    [Fact]
    public void JwtOptions_ShouldSetPropertiesCorrectly()
    {
        // Arrange & Act
        var options = new JwtOptions
        {
            SecretKey = "supersecretkey123456789012345678",
            Issuer = "https://gameguild.gg",
            Audience = "https://api.gameguild.gg",
            ExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        // Assert
        options.SecretKey.Should().Be("supersecretkey123456789012345678");
        options.Issuer.Should().Be("https://gameguild.gg");
        options.Audience.Should().Be("https://api.gameguild.gg");
        options.ExpirationMinutes.Should().Be(15);
        options.RefreshTokenExpirationDays.Should().Be(7);
    }
}
