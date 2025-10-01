using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Exceptions;

/// <summary>
/// Unit tests for ValidationException
/// </summary>
public class ValidationExceptionTests
{
    [Fact]
    public void Constructor_With_Message_Should_Set_Message()
    {
        // Arrange
        const string message = "Validation failed";

        // Act
        ValidationException exception = new(message);

        // Assert
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().BeNull();
        _ = exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_With_Message_And_InnerException_Should_Set_Both()
    {
        // Arrange
        const string message = "Validation failed";
        InvalidOperationException innerException = new("Inner error");

        // Act
        ValidationException exception = new(message, innerException);

        // Assert
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().Be(innerException);
        _ = exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_With_Errors_Should_Set_Message_And_Errors()
    {
        // Arrange
        string[] errors = ["Error 1", "Error 2", "Error 3"];

        // Act
        ValidationException exception = new(errors);

        // Assert
        _ = exception.Message.Should().Be("Error 1; Error 2; Error 3");
        _ = exception.Errors.Should().HaveCount(3);
        _ = exception.Errors.Should().ContainInOrder("Error 1", "Error 2", "Error 3");
    }

    [Fact]
    public void Constructor_With_Empty_Errors_Should_Set_Empty_Message()
    {
        // Arrange
        string[] errors = [];

        // Act
        ValidationException exception = new(errors);

        // Assert
        _ = exception.Message.Should().BeEmpty();
        _ = exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_Should_Inherit_From_Exception()
    {
        // Arrange & Act
        ValidationException exception = new("test");

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Errors_Property_Should_Be_Initialized_Empty_By_Default()
    {
        // Arrange & Act
        ValidationException exception = new("test");

        // Assert
        _ = exception.Errors.Should().NotBeNull();
        _ = exception.Errors.Should().BeEmpty();
    }
}