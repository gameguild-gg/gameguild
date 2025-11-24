using System.Net;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.SharedKernel.Unit.Middlewares;

/// <summary>
/// Unit tests for the ExceptionHandlingMiddleware
/// </summary>
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
    private readonly ExceptionHandlingMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public ExceptionHandlingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _middleware = new ExceptionHandlingMiddleware(_mockNext.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_Should_Call_Next_Middleware_When_No_Exception()
    {
        // Arrange
        var nextCalled = false;
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback(() => nextCalled = true)
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        nextCalled.Should().BeTrue();
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Log_When_No_Exception()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_Should_Catch_Exception_And_Log_Error()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("unhandled exception")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_Response_ContentType_To_ApplicationJson()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.ContentType.Should().Be("application/json");
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_StatusCode_500_On_Exception()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_Should_Write_Json_Error_Response()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().NotBeNullOrEmpty();
        responseBody.Should().Contain("Test error message");
        responseBody.Should().Contain("statusCode");
        responseBody.Should().Contain("500");
    }

    [Fact]
    public async Task InvokeAsync_Should_Include_Exception_Message_In_Response()
    {
        // Arrange
        var exceptionMessage = "Custom error message for testing";
        var exception = new InvalidOperationException(exceptionMessage);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        responseBody.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task InvokeAsync_Should_Include_Timestamp_In_Response()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);
        var beforeTimestamp = DateTime.UtcNow;

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var afterTimestamp = DateTime.UtcNow;
        
        responseBody.Should().Contain("timestamp");
        
        // Parse and verify timestamp is reasonable
        var jsonDoc = JsonDocument.Parse(responseBody);
        var timestampString = jsonDoc.RootElement.GetProperty("timestamp").GetString();
        var timestamp = DateTime.Parse(timestampString!, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        timestamp.Should().BeOnOrAfter(beforeTimestamp.AddSeconds(-1));
        timestamp.Should().BeOnOrBefore(afterTimestamp.AddSeconds(1));
    }

    [Fact]
    public async Task InvokeAsync_Should_Include_StatusCode_In_Response()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        var jsonDoc = JsonDocument.Parse(responseBody);
        var statusCode = jsonDoc.RootElement.GetProperty("statusCode").GetInt32();
        statusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Multiple_Different_Exception_Types()
    {
        // Arrange & Act & Assert - ArgumentException
        var argException = new ArgumentException("Argument error");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(argException);
        var context1 = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        await _middleware.InvokeAsync(context1);
        context1.Response.StatusCode.Should().Be(500);

        // Arrange & Act & Assert - NullReferenceException
        var nullException = new NullReferenceException("Null reference error");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(nullException);
        var context2 = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        await _middleware.InvokeAsync(context2);
        context2.Response.StatusCode.Should().Be(500);

        // Arrange & Act & Assert - InvalidOperationException
        var invalidOpException = new InvalidOperationException("Invalid operation error");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).ThrowsAsync(invalidOpException);
        var context3 = new DefaultHttpContext { Response = { Body = new MemoryStream() } };
        await _middleware.InvokeAsync(context3);
        context3.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Constructor_Should_Initialize_Without_Throwing()
    {
        // Act
        var middleware = new ExceptionHandlingMiddleware(_mockNext.Object, _mockLogger.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Throw_Exception_To_Caller()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Exception_With_Null_Message()
    {
        // Arrange
        var exception = new InvalidOperationException();
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext);

        // Assert
        await act.Should().NotThrowAsync();
        _httpContext.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_Should_Return_Valid_Json()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(_httpContext.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        
        var act = () => JsonDocument.Parse(responseBody);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task InvokeAsync_Should_Log_Exception_Details()
    {
        // Arrange
        var exceptionMessage = "Detailed exception message";
        var exception = new InvalidOperationException(exceptionMessage);
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.Is<Exception>(ex => ex.Message == exceptionMessage),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
