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

        // Act
        DomainException exception = new(message);

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
        _ = exception.Message.Should().Be(message);
    }

    [Fact]
    public void DomainException_Should_Accept_InnerException()
    {
        // Arrange
        const string message = "Domain error occurred";
        InvalidOperationException innerException = new("Inner error");

        // Act
        DomainException exception = new(message, innerException);

        // Assert
        _ = exception.Message.Should().Be(message);
        _ = exception.InnerException.Should().Be(innerException);
    }

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

    [Fact]
    public void BusinessRuleViolationException_Should_Inherit_From_Exception()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Business rule violated";
        BusinessRuleSeverity severity = BusinessRuleSeverity.Error;

        // Act
        BusinessRuleViolationException exception = new(rule, message, severity);

        // Assert
        _ = exception.Should().BeAssignableTo<Exception>();
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Severity.Should().Be(severity);
        _ = exception.Message.Should().Contain(rule);
        _ = exception.Message.Should().Contain(message);
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Format_Message_Correctly()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Custom error message";
        BusinessRuleSeverity severity = BusinessRuleSeverity.Warning;

        // Act
        BusinessRuleViolationException exception = new(rule, message, severity);

        // Assert
        _ = exception.Message.Should().Be($"Business rule '{rule}' violated: {message}");
    }

    [Fact]
    public void BusinessRuleViolationException_Should_Accept_InnerException()
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Business rule violated";
        BusinessRuleSeverity severity = BusinessRuleSeverity.Error;
        InvalidOperationException innerException = new("Inner error");

        // Act
        BusinessRuleViolationException exception = new(rule, message, severity, innerException);

        // Assert
        _ = exception.Rule.Should().Be(rule);
        _ = exception.Severity.Should().Be(severity);
        _ = exception.InnerException.Should().Be(innerException);
    }

    [Theory]
    [InlineData(BusinessRuleSeverity.Warning)]
    [InlineData(BusinessRuleSeverity.Error)]
    [InlineData(BusinessRuleSeverity.Critical)]
    public void BusinessRuleViolationException_Should_Accept_All_Severity_Levels(BusinessRuleSeverity severity)
    {
        // Arrange
        const string rule = "TestRule";
        const string message = "Test message";

        // Act
        BusinessRuleViolationException exception = new(rule, message, severity);

        // Assert
        _ = exception.Severity.Should().Be(severity);
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
/// Unit tests for BusinessRuleSeverity enumeration
/// </summary>
public class BusinessRuleSeverityTests
{
    [Fact]
    public void BusinessRuleSeverity_Should_Have_Expected_Values()
    {
        // Act & Assert
        _ = Enum.IsDefined(typeof(BusinessRuleSeverity), BusinessRuleSeverity.Warning).Should().BeTrue();
        _ = Enum.IsDefined(typeof(BusinessRuleSeverity), BusinessRuleSeverity.Error).Should().BeTrue();
        _ = Enum.IsDefined(typeof(BusinessRuleSeverity), BusinessRuleSeverity.Critical).Should().BeTrue();
    }

    [Fact]
    public void BusinessRuleSeverity_Should_Have_Correct_Numeric_Values()
    {
        // Act & Assert
        _ = ((int)BusinessRuleSeverity.Warning).Should().BeLessThan((int)BusinessRuleSeverity.Error);
        _ = ((int)BusinessRuleSeverity.Error).Should().BeLessThan((int)BusinessRuleSeverity.Critical);
    }
}