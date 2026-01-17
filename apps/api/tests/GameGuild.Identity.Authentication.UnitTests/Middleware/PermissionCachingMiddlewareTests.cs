using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Middleware;

/// <summary>
/// Unit tests for the PermissionCachingMiddleware
/// </summary>
public class PermissionCachingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<PermissionCachingMiddleware>> _mockLogger;
    private readonly PermissionCachingMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public PermissionCachingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<PermissionCachingMiddleware>>();
        _middleware = new PermissionCachingMiddleware(_mockNext.Object, _mockLogger.Object);
        _httpContext = new DefaultHttpContext();
    }

    [Fact]
    public async Task InvokeAsync_Should_Add_Permission_Cache_Header()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Permission-Cache");
        _httpContext.Response.Headers["X-Permission-Cache"].ToString().Should().Be("enabled");
    }

    [Fact]
    public async Task InvokeAsync_Should_Call_Next_Middleware()
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
    public async Task InvokeAsync_Should_Add_Header_Before_Calling_Next()
    {
        // Arrange
        var headerAddedBeforeNext = false;
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback((HttpContext ctx) =>
            {
                headerAddedBeforeNext = ctx.Response.Headers.ContainsKey("X-Permission-Cache");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        headerAddedBeforeNext.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Should_Propagate_Exception_From_Next_Middleware()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Fact]
    public async Task InvokeAsync_Should_Not_Throw_When_Adding_Header()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_Should_Complete_Successfully_On_Valid_Request()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers["X-Permission-Cache"].ToString().Should().Be("enabled");
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Work_With_Multiple_Requests()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);
        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();

        // Act
        await _middleware.InvokeAsync(context1);
        await _middleware.InvokeAsync(context2);

        // Assert
        context1.Response.Headers["X-Permission-Cache"].ToString().Should().Be("enabled");
        context2.Response.Headers["X-Permission-Cache"].ToString().Should().Be("enabled");
    }

    [Fact]
    public void Constructor_Should_Initialize_Without_Throwing()
    {
        // Act
        var middleware = new PermissionCachingMiddleware(_mockNext.Object, _mockLogger.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_Should_Preserve_Existing_Response_Headers()
    {
        // Arrange
        _httpContext.Response.Headers.Append("X-Custom-Header", "custom-value");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(_httpContext);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Custom-Header");
        _httpContext.Response.Headers["X-Custom-Header"].ToString().Should().Be("custom-value");
        _httpContext.Response.Headers.Should().ContainKey("X-Permission-Cache");
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Already_Started_Response()
    {
        // Arrange
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback((HttpContext ctx) => ctx.Response.StatusCode = 200)
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _middleware.InvokeAsync(_httpContext);

        // Assert
        await act.Should().NotThrowAsync();
        _httpContext.Response.Headers.Should().ContainKey("X-Permission-Cache");
    }
}
