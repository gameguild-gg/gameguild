using FluentAssertions;
using GameGuild.Core.Exceptions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Exceptions;

/// <summary>
/// Unit tests for domain-specific exceptions
/// </summary>
public class DomainExceptionTests
{
    [Fact]
    public void DomainException_Should_Inherit_From_Exception()
    {
        // Arrange
        const string message = "Domain error occurred";

        // Act - Use concrete BusinessException instead of abstract DomainException
        BusinessException exception = new(message);

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
        _ = exception.Should().BeAssignableTo<DomainException>();
        _ = exception.Message.Should().Be(message);
    }

    [Fact]
    public void DomainException_Should_Accept_InnerException()
    {
        // Arrange
        const string message = "Domain error occurred";
        InvalidOperationException innerException = new("Inner error");

        // Act - Use concrete BusinessException instead of abstract DomainException
        BusinessException exception = new(message, innerException);

        // Assert
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Inherit_From_Exception()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Business rule violated";

        // Act
        BusinessRuleViolationException exception = new(rule, message);

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
        _ = exception.Should().BeAssignableTo<DomainException>();
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Message.Should().Be(message);
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Format_Message_Correctly()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Custom error message";

        // Act
        BusinessRuleViolationException exception = new(rule, message);

        // Assert
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Message.Should().Be(message);
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Accept_InnerException()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Business rule violated";
        InvalidOperationException innerException = new("Inner error");

        // Act
        BusinessRuleViolationException exception = new(rule, message, innerException);

        // Assert
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Have_Context_Property()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Test message";
        object context = new { UserId = 123, Action = "Create" };

        // Act
        BusinessRuleViolationException exception = new(rule, message, context);

        // Assert
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Message.Should().Be(message);
        _ = exception.Context.Should().Be(context);
    }
}

/// <summary>
/// Unit tests for ErrorType enumeration
/// </summary>
public class ErrorTypeTests
{
    [Fact]
    public void ErrorType_Should_Have_Expected_Values()
    {
        // Act & Assert
        _ = Enum.IsDefined(typeof(ErrorType), ErrorType.Failure).Should().BeTrue();
        _ = Enum.IsDefined(typeof(ErrorType), ErrorType.Validation).Should().BeTrue();
        _ = Enum.IsDefined(typeof(ErrorType), ErrorType.Problem).Should().BeTrue();
        _ = Enum.IsDefined(typeof(ErrorType), ErrorType.NotFound).Should().BeTrue();
        _ = Enum.IsDefined(typeof(ErrorType), ErrorType.Conflict).Should().BeTrue();
    }

    [Fact]
    public void ErrorType_Values_Should_Be_Distinct()
    {
        // Arrange
        ErrorType[] allValues = Enum.GetValues<ErrorType>();

        // Act & Assert
        _ = allValues.Should().OnlyHaveUniqueItems();
    }
}

/// <summary>
/// Unit tests for NotFoundException
/// </summary>
public class NotFoundExceptionTests
{
    [Fact]
    public void NotFoundException_Should_Inherit_From_Exception()
    {
        // Arrange
        const string message = "Resource not found";

        // Act
        NotFoundException exception = new(message);

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
        _ = exception.Message.Should().Be(message);
    }

    [Fact]
    public void NotFoundException_Should_Accept_InnerException()
    {
        // Arrange
        const string message = "Resource not found";
        InvalidOperationException innerException = new("Inner error");

        // Act
        NotFoundException exception = new(message, innerException);

        // Assert
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().Be(innerException);
    }
}