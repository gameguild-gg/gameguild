using System.Diagnostics;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Behaviors;

/// <summary>
/// Unit tests for PerformanceBehavior
/// </summary>
public class PerformanceBehaviorTests
{
    private readonly Mock<ILogger<PerformanceBehavior<TestRequest, Result<string>>>> _mockLogger;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly Mock<RequestHandlerDelegateBase<Result<string>>> _mockNext;
    private readonly DateTime _fixedDateTime = new(2023, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    public PerformanceBehaviorTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceBehavior<TestRequest, Result<string>>>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockNext = new Mock<RequestHandlerDelegateBase<Result<string>>>();

        _mockDateTimeProvider.Setup(x => x.UtcNow).Returns(_fixedDateTime);
    }

    // Test request for performance monitoring
    public class TestRequest : IBaseRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task Handle_Should_Call_Next_Handler()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        Result<string> result = await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _ = result.Should().Be(expectedResult);
        _mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Performance_Metrics_For_Fast_Request()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestRequest") &&
                                              v.ToString()!.Contains("Performance")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Warning_For_Slow_Request()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        // Setup slow response (simulate delay > 1000ms)
        _mockNext.Setup(x => x()).Returns(async () =>
        {
            await Task.Delay(1100);
            return expectedResult;
        });

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestRequest") &&
                                              v.ToString()!.Contains("Performance")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Error_For_Critical_Request()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        // Setup very slow response (simulate delay > 5000ms)
        _mockNext.Setup(x => x()).Returns(async () =>
        {
            await Task.Delay(5100);
            return expectedResult;
        });

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestRequest") &&
                                              v.ToString()!.Contains("Performance")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Log_Performance_Metrics_When_Exception_Occurs()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        InvalidOperationException exception = new("Test exception");

        _mockNext.Setup(x => x()).ThrowsAsync(exception);

        // Act & Assert
        Func<Task> act = async () => await behavior.Handle(request, _mockNext.Object, CancellationToken.None);
        _ = await act.Should().ThrowAsync<InvalidOperationException>();

        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestRequest") &&
                                              v.ToString()!.Contains("Performance")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Track_Memory_Usage()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert - Verify that memory metrics are logged
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("KB")), // Memory usage in KB
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Use_DateTimeProvider_For_StartTime()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockDateTimeProvider.Verify(x => x.UtcNow, Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Include_Request_Name_In_Performance_Log()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        _mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TestRequest")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Should_Measure_Execution_Time_Accurately()
    {
        // Arrange
        TestRequest request = new() { Name = "Test" };
        PerformanceBehavior<TestRequest, Result<string>> behavior = new(_mockLogger.Object, _mockDateTimeProvider.Object);
        Result<string> expectedResult = Result.Success("Success");

        const int delayMs = 100;
        _mockNext.Setup(x => x()).Returns(async () =>
        {
            await Task.Delay(delayMs);
            return expectedResult;
        });

        // Act
        Stopwatch stopwatch = Stopwatch.StartNew();
        await behavior.Handle(request, _mockNext.Object, CancellationToken.None);
        stopwatch.Stop();

        // Assert - The behavior should have measured time close to our delay
        // We verify through logging that timing was captured
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("ms")), // Contains milliseconds
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}