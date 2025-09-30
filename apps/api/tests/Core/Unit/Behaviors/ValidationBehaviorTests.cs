using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Behaviors;

/// <summary>
/// Unit tests for ValidationBehavior
/// </summary>
public class ValidationBehaviorTests
{
    private readonly Mock<ILogger<ValidationBehavior<TestRequest, Result<string>>>> _mockLogger;
    private readonly Mock<RequestHandlerDelegateBase<Result<string>>> _mockNext;

    public ValidationBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<ValidationBehavior<TestRequest, Result<string>>>>();
        _mockNext = new Mock<RequestHandlerDelegateBase<Result<string>>>();
    }

    // Test request for validation
    public class TestRequest : IBaseRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Test validator
    public class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            _ = RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            _ = RuleFor(x => x.Email).EmailAddress().WithMessage("Email must be valid");
        }
    }

    [Fact]
    public async Task Handle_Should_Proceed_When_No_Validators()
    {
        // Arrange
        TestRequest request = new() { Name = "Test", Email = "test@example.com" };
        IEnumerable<IValidator<TestRequest>> validators = [];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedResult);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Proceed_When_Validation_Passes()
    {
        // Arrange
        TestRequest request = new() { Name = "Test", Email = "test@example.com" };
        IEnumerable<IValidator<TestRequest>> validators = [new TestRequestValidator()];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedResult);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Return_Validation_Failure_When_Validation_Fails()
    {
        // Arrange
        TestRequest request = new() { Name = "", Email = "invalid-email" };
        IEnumerable<IValidator<TestRequest>> validators = [new TestRequestValidator()];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Type.Should().Be(ErrorType.Validation);
        _mockNext.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Use_First_Validation_Error_For_Result()
    {
        // Arrange
        TestRequest request = new() { Name = "", Email = "invalid-email" };
        IEnumerable<IValidator<TestRequest>> validators = [new TestRequestValidator()];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().StartWith("Validation.");
        _ = result.Error.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_Should_Throw_When_Next_Is_Null()
    {
        // Arrange
        TestRequest request = new();
        IEnumerable<IValidator<TestRequest>> validators = [];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);

        // Act & Assert
        Func<Task> act = async () => await behavior.Handle(request, null!, CancellationToken.None);
        _ = await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Handle_Should_Log_Debug_When_No_Validators()
    {
        // Arrange
        TestRequest request = new();
        IEnumerable<IValidator<TestRequest>> validators = [];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No validators found")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Debug_When_Validation_Passes()
    {
        // Arrange
        TestRequest request = new() { Name = "Test", Email = "test@example.com" };
        IEnumerable<IValidator<TestRequest>> validators = [new TestRequestValidator()];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation passed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Warning_When_Validation_Fails()
    {
        // Arrange
        TestRequest request = new() { Name = "", Email = "invalid-email" };
        IEnumerable<IValidator<TestRequest>> validators = [new TestRequestValidator()];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Validation failed")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Run_Multiple_Validators()
    {
        // Arrange
        TestRequest request = new() { Name = "", Email = "invalid-email" };

        Mock<IValidator<TestRequest>> validator1 = new();
        Mock<IValidator<TestRequest>> validator2 = new();

        ValidationFailure failure1 = new("Name", "Name is required");
        ValidationFailure failure2 = new("Email", "Email is invalid");

        _ = validator1.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new FluentValidation.Results.ValidationResult([failure1]));

        _ = validator2.Setup(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new FluentValidation.Results.ValidationResult([failure2]));

        IEnumerable<IValidator<TestRequest>> validators = [validator1.Object, validator2.Object];
        ValidationBehavior<TestRequest, Result<string>> behavior = new(validators, _mockLogger.Object);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        validator1.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        validator2.Verify(x => x.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}