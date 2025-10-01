using FluentAssertions;
using GameGuild.Modules.Credentials;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Events;

/// <summary>
/// Unit tests for the CredentialActivatedEvent
/// Tests domain event properties and construction
/// </summary>
public class CredentialActivatedEventTests
{
    [Fact]
    public void Should_Create_Event_With_Required_Properties()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var credentialEvent = new CredentialActivatedEvent(credentialId);

        // Assert
        credentialEvent.CredentialId.Should().Be(credentialId);
        credentialEvent.AggregateId.Should().Be(credentialId);
        credentialEvent.AggregateType.Should().Be(nameof(Credential));
    }

    [Fact]
    public void Should_Have_Empty_Guid_When_Created_With_Empty_Id()
    {
        // Arrange & Act
        var credentialEvent = new CredentialActivatedEvent(Guid.Empty);

        // Assert
        credentialEvent.CredentialId.Should().Be(Guid.Empty);
        credentialEvent.AggregateId.Should().Be(Guid.Empty);
    }

    [Fact]
    public void Should_Be_Derived_From_DomainEventBase()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var credentialEvent = new CredentialActivatedEvent(credentialId);

        // Assert
        credentialEvent.Should().NotBeNull();
        credentialEvent.AggregateId.Should().Be(credentialId);
        credentialEvent.AggregateType.Should().Be(nameof(Credential));
        credentialEvent.EventId.Should().NotBeEmpty();
        credentialEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        credentialEvent.Version.Should().Be(1);
    }

    [Fact]
    public void Should_Support_Different_Credential_Ids()
    {
        // Arrange
        var credentialId1 = Guid.NewGuid();
        var credentialId2 = Guid.NewGuid();

        // Act
        var event1 = new CredentialActivatedEvent(credentialId1);
        var event2 = new CredentialActivatedEvent(credentialId2);

        // Assert
        event1.CredentialId.Should().Be(credentialId1);
        event2.CredentialId.Should().Be(credentialId2);
        event1.CredentialId.Should().NotBe(event2.CredentialId);
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Theory]
    [InlineData("550e8400-e29b-41d4-a716-446655440000")]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8")]
    public void Should_Handle_Specific_Credential_Ids(string guidString)
    {
        // Arrange
        var credentialId = Guid.Parse(guidString);

        // Act
        var credentialEvent = new CredentialActivatedEvent(credentialId);

        // Assert
        credentialEvent.CredentialId.Should().Be(credentialId);
        credentialEvent.AggregateId.Should().Be(credentialId);
    }
}