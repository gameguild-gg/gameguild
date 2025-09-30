using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Results;

/// <summary>
/// Unit tests for Result class
/// </summary>
public class ResultTests
{
    [Fact]
    public void Success_Should_Create_Successful_Result()
    {
        // Act
        Result result = Result.Success();

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.IsFailure.Should().BeFalse();
        _ = result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Success_Generic_Should_Create_Successful_Result_With_Value()
    {
        // Arrange
        const string value = "test value";

        // Act
        Result<string> result = Result.Success(value);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.IsFailure.Should().BeFalse();
        _ = result.Error.Should().Be(Error.None);
        _ = result.Value.Should().Be(value);
    }

    [Fact]
    public void Failure_Should_Create_Failed_Result()
    {
        // Arrange
        Error error = Error.Failure("Test.Error", "Test error message");

        // Act
        Result result = Result.Failure(error);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Should().Be(error);
    }

    [Fact]
    public void Failure_Generic_Should_Create_Failed_Result_With_Default_Value()
    {
        // Arrange
        Error error = Error.Failure("Test.Error", "Test error message");

        // Act
        Result<string> result = Result.Failure<string>(error);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Should().Be(error);
    }

    [Fact]
    public void ValidationFailure_Should_Create_Validation_Error_Result()
    {
        // Arrange
        const string propertyName = "Email";
        const string message = "Email is required";
        const string attemptedValue = "";

        // Act
        Result result = Result.ValidationFailure(propertyName, message, attemptedValue);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Type.Should().Be(ErrorType.Validation);
        _ = result.Error.Code.Should().Be($"Validation.{propertyName}");
        _ = result.Error.Message.Should().Be(message);
    }

    [Fact]
    public void ValidationFailure_Generic_Should_Create_Validation_Error_Result()
    {
        // Arrange
        const string propertyName = "Email";
        const string message = "Email is required";

        // Act
        Result<string> result = Result.ValidationFailure<string>(propertyName, message);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Type.Should().Be(ErrorType.Validation);
        _ = result.Error.Code.Should().Be($"Validation.{propertyName}");
        _ = result.Error.Message.Should().Be(message);
    }

    [Fact]
    public void BusinessRuleViolation_Should_Create_Business_Rule_Error()
    {
        // Arrange
        const string rule = "UserMustBeActive";
        const string message = "User must be active to perform this action";
        object context = new { UserId = 123 };

        // Act
        Result result = Result.BusinessRuleViolation(rule, message, context);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Type.Should().Be(ErrorType.Problem);
        _ = result.Error.Code.Should().Be($"BusinessRule.{rule}");
        _ = result.Error.Message.Should().Be(message);
    }

    [Fact]
    public void NotFound_Should_Create_NotFound_Error()
    {
        // Arrange
        const string resource = "User";
        const int identifier = 123;

        // Act
        Result result = Result.NotFound(resource, identifier);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Type.Should().Be(ErrorType.NotFound);
        _ = result.Error.Code.Should().Be($"{resource}.NotFound");
        _ = result.Error.Message.Should().Be($"{resource} not found: {identifier}");
    }

    [Fact]
    public void NotFound_Without_Identifier_Should_Create_NotFound_Error()
    {
        // Arrange
        const string resource = "User";

        // Act
        Result result = Result.NotFound(resource);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Type.Should().Be(ErrorType.NotFound);
        _ = result.Error.Code.Should().Be($"{resource}.NotFound");
        _ = result.Error.Message.Should().Be($"{resource} not found");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Success_With_Non_None_Error()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test");

        // Act & Assert
        Action act = () => new Result(true, error);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("error");
    }

    [Fact]
    public void Constructor_Should_Throw_When_Failure_With_None_Error()
    {
        // Act & Assert
        Action act = () => new Result(false, Error.None);
        _ = act.Should().Throw<ArgumentException>()
            .WithParameterName("error");
    }
}

/// <summary>
/// Unit tests for Result<T> class
/// </summary>
public class ResultGenericTests
{
    [Fact]
    public void Value_Should_Return_Value_When_Success()
    {
        // Arrange
        const string expectedValue = "test value";
        Result<string> result = Result.Success(expectedValue);

        // Act
        string value = result.Value;

        // Assert
        _ = value.Should().Be(expectedValue);
    }

    [Fact]
    public void Value_Should_Throw_When_Failure()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");
        Result<string> result = Result.Failure<string>(error);

        // Act & Assert
        Action act = () => _ = result.Value;
        _ = act.Should().Throw<InvalidOperationException>()
            .WithMessage("The value of a failure result can't be accessed.");
    }

    [Fact]
    public void Implicit_Conversion_From_Value_Should_Create_Success_Result()
    {
        // Act
        Result<string> result = "test value";

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be("test value");
    }

    [Fact]
    public void Implicit_Conversion_From_Null_Value_Should_Create_Failure_Result()
    {
        // Act
        Result<string> result = (string?)null;

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void Implicit_Conversion_From_Error_Should_Create_Failure_Result()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");

        // Act
        Result<string> result = error;

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Should().Be(error);
    }

    [Fact]
    public void Map_Should_Transform_Value_When_Success()
    {
        // Arrange
        Result<int> result = Result.Success(42);

        // Act
        Result<string> mappedResult = result.Map(x => x.ToString());

        // Assert
        _ = mappedResult.IsSuccess.Should().BeTrue();
        _ = mappedResult.Value.Should().Be("42");
    }

    [Fact]
    public void Map_Should_Propagate_Error_When_Failure()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");
        Result<int> result = Result.Failure<int>(error);

        // Act
        Result<string> mappedResult = result.Map(x => x.ToString());

        // Assert
        _ = mappedResult.IsSuccess.Should().BeFalse();
        _ = mappedResult.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_Should_Chain_Operations_When_Success()
    {
        // Arrange
        Result<int> result = Result.Success(42);

        // Act
        Result<string> boundResult = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        _ = boundResult.IsSuccess.Should().BeTrue();
        _ = boundResult.Value.Should().Be("42");
    }

    [Fact]
    public void Bind_Should_Propagate_Error_When_Failure()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");
        Result<int> result = Result.Failure<int>(error);

        // Act
        Result<string> boundResult = result.Bind(x => Result.Success(x.ToString()));

        // Assert
        _ = boundResult.IsSuccess.Should().BeFalse();
        _ = boundResult.Error.Should().Be(error);
    }

    [Fact]
    public void Bind_Should_Return_Failure_From_Binder()
    {
        // Arrange
        Result<int> result = Result.Success(42);
        Error binderError = Error.Failure("Binder", "Binder error");

        // Act
        Result<string> boundResult = result.Bind(x => Result.Failure<string>(binderError));

        // Assert
        _ = boundResult.IsSuccess.Should().BeFalse();
        _ = boundResult.Error.Should().Be(binderError);
    }

    [Fact]
    public async Task BindAsync_Should_Chain_Async_Operations_When_Success()
    {
        // Arrange
        Result<int> result = Result.Success(42);

        // Act
        Result<string> boundResult = await result.BindAsync(x => Task.FromResult(Result.Success(x.ToString())));

        // Assert
        _ = boundResult.IsSuccess.Should().BeTrue();
        _ = boundResult.Value.Should().Be("42");
    }

    [Fact]
    public async Task BindAsync_Should_Propagate_Error_When_Failure()
    {
        // Arrange
        Error error = Error.Failure("Test", "Test error");
        Result<int> result = Result.Failure<int>(error);

        // Act
        Result<string> boundResult = await result.BindAsync(x => Task.FromResult(Result.Success(x.ToString())));

        // Assert
        _ = boundResult.IsSuccess.Should().BeFalse();
        _ = boundResult.Error.Should().Be(error);
    }
}