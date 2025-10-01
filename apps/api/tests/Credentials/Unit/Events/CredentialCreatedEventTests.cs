using FluentAssertions;
using GameGuild.Modules.Credentials;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Events;

/// <summary>
/// Unit tests for the CredentialCreatedEvent
/// Tests event creation and properties
/// </summary>
public class CredentialCreatedEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = "password";
        var createdAt = DateTime.UtcNow;

        // Act
        var eventObj = new CredentialCreatedEvent(credentialId, userId, type, createdAt);

        // Assert
        eventObj.CredentialId.Should().Be(credentialId);
        eventObj.UserId.Should().Be(userId);
        eventObj.Type.Should().Be(type);
        eventObj.CreatedAt.Should().Be(createdAt);
        eventObj.AggregateId.Should().Be(credentialId);
        eventObj.AggregateType.Should().Be(nameof(Credential));
    }

    [Fact]
    public void Constructor_ShouldInheritFromDomainEventBase()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = "api_key";
        var createdAt = DateTime.UtcNow;

        // Act
        var eventObj = new CredentialCreatedEvent(credentialId, userId, type, createdAt);

        // Assert
        eventObj.Should().BeAssignableTo<GameGuild.CQRS.DomainEventBase>();
        eventObj.EventId.Should().NotBeEmpty();
        eventObj.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("api_key")]
    [InlineData("oauth_token")]
    [InlineData("2fa_secret")]
    public void Constructor_ShouldAcceptDifferentCredentialTypes(string credentialType)
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        // Act
        var eventObj = new CredentialCreatedEvent(credentialId, userId, credentialType, createdAt);

        // Assert
        eventObj.Type.Should().Be(credentialType);
    }

    [Fact]
    public void Properties_ShouldBeReadOnly()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var type = "password";
        var createdAt = DateTime.UtcNow;

        // Act
        var eventObj = new CredentialCreatedEvent(credentialId, userId, type, createdAt);

        // Assert - Properties should only have getters
        var credentialIdProperty = typeof(CredentialCreatedEvent).GetProperty(nameof(CredentialCreatedEvent.CredentialId));
        var userIdProperty = typeof(CredentialCreatedEvent).GetProperty(nameof(CredentialCreatedEvent.UserId));
        var typeProperty = typeof(CredentialCreatedEvent).GetProperty(nameof(CredentialCreatedEvent.Type));
        var createdAtProperty = typeof(CredentialCreatedEvent).GetProperty(nameof(CredentialCreatedEvent.CreatedAt));

        credentialIdProperty.Should().NotBeNull();
        credentialIdProperty!.CanWrite.Should().BeFalse();
        credentialIdProperty.CanRead.Should().BeTrue();

        userIdProperty.Should().NotBeNull();
        userIdProperty!.CanWrite.Should().BeFalse();
        userIdProperty.CanRead.Should().BeTrue();

        typeProperty.Should().NotBeNull();
        typeProperty!.CanWrite.Should().BeFalse();
        typeProperty.CanRead.Should().BeTrue();

        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.CanWrite.Should().BeFalse();
        createdAtProperty.CanRead.Should().BeTrue();
    }
}