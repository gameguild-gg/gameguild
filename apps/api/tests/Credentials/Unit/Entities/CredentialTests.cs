using FluentAssertions;
using GameGuild.Modules.Credentials;
using GameGuild.Modules.Users;

namespace GameGuild.Tests.Credentials.Unit.Entities;

/// <summary>
/// Unit tests for the Credential entity
/// Tests entity behavior, methods, and properties
/// </summary>
public class CredentialTests
{
    [Fact]
    public void Constructor_Default_ShouldInitializeWithDefaultValues()
    {
        // Act
        var credential = new Credential();

        // Assert
        credential.Id.Should().NotBeEmpty();
        credential.UserId.Should().BeEmpty();
        credential.Type.Should().BeEmpty();
        credential.Value.Should().BeEmpty();
        credential.Metadata.Should().BeNull();
        credential.IsActive.Should().BeTrue();
        credential.LastUsedAt.Should().BeNull();
        credential.ExpiresAt.Should().BeNull();
        credential.TenantId.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithUser_ShouldSetUser()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };

        // Act
        var credential = new Credential(user);

        // Assert
        credential.User.Should().Be(user);
    }

    [Fact]
    public void Constructor_WithPartial_ShouldInitializeFromPartial()
    {
        // Arrange
        var partial = new { Type = "password", Value = "hashed_value", IsActive = false };

        // Act
        var credential = new Credential(partial);

        // Assert
        credential.Type.Should().Be("password");
        credential.Value.Should().Be("hashed_value");
        credential.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Constructor_WithPartialAndUser_ShouldInitializeFromPartialAndSetUser()
    {
        // Arrange
        var user = new User { Id = Guid.NewGuid() };
        var partial = new { Type = "api_key", Value = "encrypted_key" };

        // Act
        var credential = new Credential(partial, user);

        // Assert
        credential.Type.Should().Be("api_key");
        credential.Value.Should().Be("encrypted_key");
        credential.User.Should().Be(user);
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsNull_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential { ExpiresAt = null };

        // Act & Assert
        credential.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInFuture_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential { ExpiresAt = DateTime.UtcNow.AddDays(1) };

        // Act & Assert
        credential.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpiresAtIsInPast_ShouldBeTrue()
    {
        // Arrange
        var credential = new Credential { ExpiresAt = DateTime.UtcNow.AddDays(-1) };

        // Act & Assert
        credential.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpiredAndNotDeleted_ShouldBeTrue()
    {
        // Arrange
        var credential = new Credential
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            DeletedAt = null
        };

        // Act & Assert
        credential.IsValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential
        {
            IsActive = false,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            DeletedAt = null
        };

        // Act & Assert
        credential.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            DeletedAt = null
        };

        // Act & Assert
        credential.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenDeleted_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            DeletedAt = DateTime.UtcNow
        };

        // Act & Assert
        credential.IsValid.Should().BeFalse();
    }

    [Fact]
    public void IsGlobal_WhenTenantIdIsNull_ShouldBeTrue()
    {
        // Arrange
        var credential = new Credential { TenantId = null };

        // Act & Assert
        credential.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void IsGlobal_WhenTenantIdIsSet_ShouldBeFalse()
    {
        // Arrange
        var credential = new Credential { TenantId = Guid.NewGuid() };

        // Act & Assert
        credential.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void MarkAsUsed_ShouldUpdateLastUsedAtAndTouchTimestamp()
    {
        // Arrange
        var credential = new Credential();
        var originalUpdatedAt = credential.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(1);

        // Act
        credential.MarkAsUsed();

        // Assert
        credential.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        credential.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalseAndTouchTimestamp()
    {
        // Arrange
        var credential = new Credential { IsActive = true };
        var originalUpdatedAt = credential.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(1);

        // Act
        credential.Deactivate();

        // Assert
        credential.IsActive.Should().BeFalse();
        credential.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrueAndTouchTimestamp()
    {
        // Arrange
        var credential = new Credential { IsActive = false };
        var originalUpdatedAt = credential.UpdatedAt;

        // Wait a small amount to ensure timestamp difference
        Thread.Sleep(1);

        // Act
        credential.Activate();

        // Assert
        credential.IsActive.Should().BeTrue();
        credential.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public void Type_ShouldAcceptValidCredentialTypes(string credentialType)
    {
        // Arrange & Act
        var credential = new Credential { Type = credentialType };

        // Assert
        credential.Type.Should().Be(credentialType);
    }

    [Fact]
    public void Value_ShouldAcceptLongValues()
    {
        // Arrange
        var longValue = new string('x', 999); // Just under the max length
        var credential = new Credential();

        // Act
        credential.Value = longValue;

        // Assert
        credential.Value.Should().Be(longValue);
        credential.Value.Length.Should().Be(999);
    }

    [Fact]
    public void Metadata_ShouldAcceptJsonStrings()
    {
        // Arrange
        var jsonMetadata = """{"salt": "random_salt", "algorithm": "bcrypt", "rounds": 12}""";
        var credential = new Credential();

        // Act
        credential.Metadata = jsonMetadata;

        // Assert
        credential.Metadata.Should().Be(jsonMetadata);
    }
}