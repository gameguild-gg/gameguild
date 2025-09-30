using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Exceptions;

/// <summary>
/// Unit tests for Error record
/// </summary>
public class ErrorTests
{
    [Fact]
    public void None_Should_Have_Empty_Code_And_Message()
    {
        // Act
        Error none = Error.None;

        // Assert
        _ = none.Code.Should().BeEmpty();
        _ = none.Message.Should().BeEmpty();
        _ = none.Type.Should().Be(ErrorType.Failure);
        _ = none.Metadata.Should().BeNull();
    }

    [Fact]
    public void NullValue_Should_Have_Predefined_Values()
    {
        // Act
        Error nullValue = Error.NullValue;

        // Assert
        _ = nullValue.Code.Should().Be("General.Null");
        _ = nullValue.Message.Should().Be("Null value was provided");
        _ = nullValue.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public void Failure_Should_Create_Failure_Error()
    {
        // Arrange
        const string code = "Test.Failure";
        const string message = "Test failure message";

        // Act
        Error error = Error.Failure(code, message);

        // Assert
        _ = error.Code.Should().Be(code);
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Failure);
        _ = error.Metadata.Should().BeNull();
    }

    [Fact]
    public void NotFound_Should_Create_NotFound_Error()
    {
        // Arrange
        const string code = "User.NotFound";
        const string message = "User not found";

        // Act
        Error error = Error.NotFound(code, message);

        // Assert
        _ = error.Code.Should().Be(code);
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.NotFound);
        _ = error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Problem_Should_Create_Problem_Error()
    {
        // Arrange
        const string code = "Business.Problem";
        const string message = "Business rule violation";

        // Act
        Error error = Error.Problem(code, message);

        // Assert
        _ = error.Code.Should().Be(code);
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Problem);
        _ = error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Conflict_Should_Create_Conflict_Error()
    {
        // Arrange
        const string code = "Resource.Conflict";
        const string message = "Resource already exists";

        // Act
        Error error = Error.Conflict(code, message);

        // Assert
        _ = error.Code.Should().Be(code);
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Conflict);
        _ = error.Metadata.Should().BeNull();
    }

    [Fact]
    public void Validation_Should_Create_Validation_Error()
    {
        // Arrange
        const string code = "Field.Invalid";
        const string message = "Field is invalid";

        // Act
        Error error = Error.Validation(code, message);

        // Assert
        _ = error.Code.Should().Be(code);
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.Metadata.Should().BeNull();
    }

    [Fact]
    public void ValidationFailure_Should_Create_Validation_Error_With_Metadata()
    {
        // Arrange
        const string propertyName = "Email";
        const string message = "Email is required";
        const string attemptedValue = "";

        // Act
        Error error = Error.ValidationFailure(propertyName, message, attemptedValue);

        // Assert
        _ = error.Code.Should().Be($"Validation.{propertyName}");
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.Metadata.Should().NotBeNull();
        _ = error.Metadata!["property"].Should().Be(propertyName);
        _ = error.Metadata["attemptedValue"].Should().Be(attemptedValue);
    }

    [Fact]
    public void ValidationFailure_Without_AttemptedValue_Should_Create_Error_With_Property_Only()
    {
        // Arrange
        const string propertyName = "Name";
        const string message = "Name is required";

        // Act
        Error error = Error.ValidationFailure(propertyName, message);

        // Assert
        _ = error.Code.Should().Be($"Validation.{propertyName}");
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.Metadata.Should().NotBeNull();
        _ = error.Metadata!["property"].Should().Be(propertyName);
        _ = error.Metadata.Should().NotContainKey("attemptedValue");
    }

    [Fact]
    public void RequiredField_Should_Create_Required_Validation_Error()
    {
        // Arrange
        const string propertyName = "FirstName";

        // Act
        Error error = Error.RequiredField(propertyName);

        // Assert
        _ = error.Code.Should().Be($"Validation.{propertyName}");
        _ = error.Message.Should().Be($"{propertyName} is required");
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.GetProperty().Should().Be(propertyName);
    }

    [Fact]
    public void InvalidFormat_Should_Create_Format_Validation_Error()
    {
        // Arrange
        const string propertyName = "Email";
        const string attemptedValue = "invalid-email";

        // Act
        Error error = Error.InvalidFormat(propertyName, attemptedValue);

        // Assert
        _ = error.Code.Should().Be($"Validation.{propertyName}");
        _ = error.Message.Should().Be($"{propertyName} has invalid format");
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.GetProperty().Should().Be(propertyName);
        _ = error.GetAttemptedValue().Should().Be(attemptedValue);
    }

    [Fact]
    public void OutOfRange_Should_Create_Range_Validation_Error()
    {
        // Arrange
        const string propertyName = "Age";
        const int attemptedValue = -5;

        // Act
        Error error = Error.OutOfRange(propertyName, attemptedValue);

        // Assert
        _ = error.Code.Should().Be($"Validation.{propertyName}");
        _ = error.Message.Should().Be($"{propertyName} is out of valid range");
        _ = error.Type.Should().Be(ErrorType.Validation);
        _ = error.GetProperty().Should().Be(propertyName);
        _ = error.GetAttemptedValue().Should().Be(attemptedValue);
    }

    [Fact]
    public void BusinessRule_Should_Create_Business_Rule_Error()
    {
        // Arrange
        const string rule = "UserMustBeActive";
        const string message = "User must be active to perform this action";
        object context = new { UserId = 123, Status = "Inactive" };

        // Act
        Error error = Error.BusinessRule(rule, message, context);

        // Assert
        _ = error.Code.Should().Be($"BusinessRule.{rule}");
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Problem);
        _ = error.Metadata.Should().NotBeNull();
        _ = error.Metadata!["rule"].Should().Be(rule);
        _ = error.Metadata["context"].Should().Be(context);
    }

    [Fact]
    public void BusinessRule_Without_Context_Should_Create_Error_With_Rule_Only()
    {
        // Arrange
        const string rule = "InsufficientFunds";
        const string message = "Insufficient funds for transaction";

        // Act
        Error error = Error.BusinessRule(rule, message);

        // Assert
        _ = error.Code.Should().Be($"BusinessRule.{rule}");
        _ = error.Message.Should().Be(message);
        _ = error.Type.Should().Be(ErrorType.Problem);
        _ = error.Metadata.Should().NotBeNull();
        _ = error.Metadata!["rule"].Should().Be(rule);
        _ = error.Metadata.Should().NotContainKey("context");
    }

    [Fact]
    public void GetProperty_Should_Return_Property_Name_When_Present()
    {
        // Arrange
        const string propertyName = "Email";
        Error error = Error.ValidationFailure(propertyName, "Email is invalid");

        // Act
        string? property = error.GetProperty();

        // Assert
        _ = property.Should().Be(propertyName);
    }

    [Fact]
    public void GetProperty_Should_Return_Null_When_Not_Present()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");

        // Act
        string? property = error.GetProperty();

        // Assert
        _ = property.Should().BeNull();
    }

    [Fact]
    public void GetAttemptedValue_Should_Return_Value_When_Present()
    {
        // Arrange
        const string attemptedValue = "invalid-email";
        Error error = Error.InvalidFormat("Email", attemptedValue);

        // Act
        object? value = error.GetAttemptedValue();

        // Assert
        _ = value.Should().Be(attemptedValue);
    }

    [Fact]
    public void GetAttemptedValue_Should_Return_Null_When_Not_Present()
    {
        // Arrange
        Error error = Error.RequiredField("Email");

        // Act
        object? value = error.GetAttemptedValue();

        // Assert
        _ = value.Should().BeNull();
    }

    [Fact]
    public void Equality_Should_Work_Correctly_For_Records()
    {
        // Arrange
        Error error1 = Error.Failure("Test", "Message");
        Error error2 = Error.Failure("Test", "Message");
        Error error3 = Error.Failure("Different", "Message");

        // Act & Assert
        _ = error1.Should().Be(error2);
        _ = error1.Should().NotBe(error3);
        _ = (error1 == error2).Should().BeTrue();
        _ = (error1 == error3).Should().BeFalse();
    }
}