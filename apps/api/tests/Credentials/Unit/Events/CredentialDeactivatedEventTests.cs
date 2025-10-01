using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Credentials;
using Xunit;

namespace GameGuild.Tests.Credentials.Unit.Events;

/// <summary>
/// Unit tests for the CredentialDeactivatedEvent
/// Tests event creation, properties, and inheritance
/// </summary>
public class CredentialDeactivatedEventTests
{
    [Fact]
    public void Constructor_Should_Set_Properties_Correctly()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Assert
        @event.AggregateId.Should().Be(credentialId);
        @event.AggregateType.Should().Be(nameof(Credential));
        @event.EventId.Should().NotBeEmpty();
        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_Should_Generate_Unique_EventId()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var event1 = new CredentialDeactivatedEvent(credentialId);
        var event2 = new CredentialDeactivatedEvent(credentialId);

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void Constructor_Should_Set_OccurredAt_To_Current_Time()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var beforeTime = DateTime.UtcNow;

        // Act
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Assert
        var afterTime = DateTime.UtcNow;
        @event.OccurredAt.Should().BeOnOrAfter(beforeTime);
        @event.OccurredAt.Should().BeOnOrBefore(afterTime);
    }

    [Fact]
    public void Should_Inherit_From_DomainEventBase()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Assert
        @event.Should().BeAssignableTo<DomainEventBase>();
    }

    [Fact]
    public void Should_Have_Correct_AggregateType()
    {
        // Arrange
        var credentialId = Guid.NewGuid();

        // Act
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Assert
        @event.AggregateType.Should().Be(nameof(Credential));
    }

    [Fact]
    public void Should_Accept_Any_Valid_Guid_As_CredentialId()
    {
        // Arrange & Act & Assert
        var credentialId1 = Guid.NewGuid();
        var event1 = new CredentialDeactivatedEvent(credentialId1);
        event1.AggregateId.Should().Be(credentialId1);

        var credentialId2 = Guid.NewGuid();
        var event2 = new CredentialDeactivatedEvent(credentialId2);
        event2.AggregateId.Should().Be(credentialId2);

        credentialId1.Should().NotBe(credentialId2);
        event1.AggregateId.Should().NotBe(event2.AggregateId);
    }

    [Fact]
    public void Should_Handle_Empty_Guid()
    {
        // Arrange
        var emptyGuid = Guid.Empty;

        // Act
        var @event = new CredentialDeactivatedEvent(emptyGuid);

        // Assert
        @event.AggregateId.Should().Be(Guid.Empty);
        @event.AggregateType.Should().Be(nameof(Credential));
        @event.EventId.Should().NotBeEmpty(); // EventId should still be generated
    }

    [Fact]
    public void ToString_Should_Return_Meaningful_String()
    {
        // Arrange
        var credentialId = Guid.NewGuid();
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Act
        var result = @event.ToString();

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        // The actual ToString implementation would depend on the base class implementation
    }

    [Theory]
    [InlineData("12345678-1234-1234-1234-123456789012")]
    [InlineData("87654321-4321-4321-4321-210987654321")]
    [InlineData("11111111-2222-3333-4444-555555555555")]
    public void Should_Handle_Different_Guid_Formats(string guidString)
    {
        // Arrange
        var credentialId = Guid.Parse(guidString);

        // Act
        var @event = new CredentialDeactivatedEvent(credentialId);

        // Assert
        @event.AggregateId.Should().Be(credentialId);
        @event.AggregateType.Should().Be(nameof(Credential));
    }
}