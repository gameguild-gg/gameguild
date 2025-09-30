using System.Diagnostics;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Behaviors;

/// <summary>
/// Unit tests for LoggingBehavior
/// </summary>
public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>> _mockLogger;
    private readonly Mock<RequestHandlerDelegateBase<Result<string>>> _mockNext;

    public LoggingBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, Result<string>>>>();
        _mockNext = new Mock<RequestHandlerDelegateBase<Result<string>>>();
    }

    // Test request for logging
    public class TestRequest : IBaseRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_Should_Log_Request_Processing_Start()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Processing") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Successful_Result_Completion()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> successResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(successResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully completed") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Failed_Result_With_Warning()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Error error = Error.Failure("Test.Error", "Test error message");
        Result<string> failureResult = Result.Failure<string>(error);

        _mockNext.Setup(x => x()).ReturnsAsync(failureResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed") &&
                                              v.ToString()!.Contains("with error") &&
                                              v.ToString()!.Contains("TestRequest") &&
                                              v.ToString()!.Contains("Test.Error") &&
                                              v.ToString()!.Contains("Test error message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Exception_With_Error_Level()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        InvalidOperationException exception = new("Test exception");

        _mockNext.Setup(x => x()).ThrowsAsync(exception);

        // Act & Assert
        Func<Task> act = async () => await behavior.Handle(request, _mockNext.Object, CancellationToken.None);
        _ = await act.Should().ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing") &&
                                              v.ToString()!.Contains("TestRequest")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Call_Next_Handler()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedResult);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Performance_Warning_For_Slow_Requests()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        // Setup slow response (simulate delay)
        _mockNext.Setup(x => x()).Returns(async () =>
        {
            await Task.Delay(1100); // More than 1000ms threshold
            return expectedResult;
        });

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow request detected") &&
                                              v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Not_Log_Performance_Warning_For_Fast_Requests()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Slow request detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Include_Request_Id_In_All_Logs()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert - All log calls should include RequestId
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RequestId:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(2)); // At least start and completion logs
    }
}

/// <summary>
/// Unit tests for LoggingBehavior with non-Result responses
/// </summary>
public class LoggingBehaviorNonResultTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, string>>> _mockLogger;
    private readonly Mock<RequestHandlerDelegateBase<string>> _mockNext;

    public LoggingBehaviorNonResultTests()
    {
        _mockLogger = new Mock<ILogger<LoggingBehavior<TestRequest, string>>>();
        _mockNext = new Mock<RequestHandlerDelegateBase<string>>();
    }

    public class TestRequest : IBaseRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_Should_Log_Non_Result_Response_Success()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        LoggingBehavior<TestRequest, string> behavior = new(_mockLogger.Object);
        const string expectedResponse = "Success";

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResponse);

        // Act
        string result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedResponse);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully completed") && v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}