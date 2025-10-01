using FluentAssertions;
using GameGuild.Core.Exceptions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Exceptions;

/// <summary>
/// Unit tests for BusinessException
/// </summary>
public class BusinessExceptionTests
{
    [Fact]
    public void Constructor_With_Message_Should_Set_Message()
    {
        // Arrange
        const string message = "Business rule violation";

        // Act
        var exception = new BusinessException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_With_Message_And_InnerException_Should_Set_Both()
    {
        // Arrange
        const string message = "Business rule violation";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new BusinessException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void BusinessException_Should_Inherit_From_Exception()
    {
        // Arrange & Act
        var exception = new BusinessException("test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }
}