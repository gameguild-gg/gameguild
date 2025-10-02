using System.Security;
using System.Text.Json;
using FluentAssertions;
using GameGuild.Core.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Core.Unit.Exceptions;

/// <summary>
/// Unit tests for GlobalExceptionHandler
/// </summary>
public class GlobalExceptionHandlerTests
{
    private readonly Mock<ILogger<GlobalExceptionHandler>> _mockLogger;
    private readonly GlobalExceptionHandler _handler;
    private readonly DefaultHttpContext _httpContext;

    public GlobalExceptionHandlerTests()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
        _handler = new GlobalExceptionHandler(_mockLogger.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_ValidationException()
    {
        // Arrange
        ValidationException exception = new("Validation failed");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_ArgumentException()
    {
        // Arrange
        ArgumentException exception = new("Invalid argument");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_NotFound_InvalidOperationException()
    {
        // Arrange
        InvalidOperationException exception = new("Resource not found");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_Conflict_InvalidOperationException_For_Concurrency()
    {
        // Arrange
        InvalidOperationException exception = new("Concurrency conflict detected");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_Conflict_InvalidOperationException_For_AlreadyExists()
    {
        // Arrange
        InvalidOperationException exception = new("Resource already exists");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_SecurityException()
    {
        // Arrange
        SecurityException exception = new("Access denied");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_UnauthorizedAccessException()
    {
        // Arrange
        UnauthorizedAccessException exception = new("Unauthorized access");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Handle_Generic_Exception()
    {
        // Arrange
        Exception exception = new("Generic error");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
        _ = _httpContext.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        _ = _httpContext.Response.ContentType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Log_Exception()
    {
        // Arrange
        Exception exception = new("Test exception");

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exception occurred")),
                It.Is<Exception>(ex => ex == exception),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task TryHandleAsync_Should_Write_ProblemDetails_To_Response()
    {
        // Arrange
        ValidationException exception = new("Validation failed");

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(_httpContext.Response.Body);
        string responseBody = await reader.ReadToEndAsync();

        _ = responseBody.Should().NotBeNullOrEmpty();

        // Parse JSON to verify structure
        JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;

        _ = root.GetProperty("title").GetString().Should().Be("Validation Error");
        _ = root.GetProperty("status").GetInt32().Should().Be(400);
        _ = root.GetProperty("detail").GetString().Should().Be("Validation failed");
    }

    [Fact]
    public async Task TryHandleAsync_Should_Include_Errors_For_ValidationException()
    {
        // Arrange
        string[] errors = ["Error 1", "Error 2"];
        ValidationException exception = new(errors);

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(_httpContext.Response.Body);
        string responseBody = await reader.ReadToEndAsync();

        JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;

        _ = root.TryGetProperty("errors", out JsonElement errorsElement).Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_Should_Always_Return_True()
    {
        // Arrange
        Exception exception = new("Any exception");

        // Act
        bool result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _ = result.Should().BeTrue();
    }

    [Fact]
    public async Task TryHandleAsync_Should_Set_Correct_ProblemDetails_Type()
    {
        // Arrange
        ArgumentException exception = new("Bad request");

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(_httpContext.Response.Body);
        string responseBody = await reader.ReadToEndAsync();

        JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;

        _ = root.GetProperty("type").GetString().Should().Be("https://tools.ietf.org/html/rfc7231#section-6.5.1");
    }
}