using System.Security.Claims;
using FluentAssertions;
using GameGuild;
using GameGuild.CQRS;
using GameGuild.Modules.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authorization.Unit.Handlers;

/// <summary>
/// Unit tests for the AuthorizationBehavior
/// Tests authorization pipeline behavior for secured commands and queries
/// </summary>
public class AuthorizationBehaviorTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<AuthorizationBehavior<TestAuthorizedRequest, string>>> _mockLogger;
    private readonly AuthorizationBehavior<TestAuthorizedRequest, string> _behavior;

    public AuthorizationBehaviorTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<AuthorizationBehavior<TestAuthorizedRequest, string>>>();
        _behavior = new AuthorizationBehavior<TestAuthorizedRequest, string>(_mockHttpContextAccessor.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ShouldContinuePipeline_WhenRequestIsNotAuthorized()
    {
        // Arrange
        var request = new TestUnauthorizedRequest();
        var expectedResponse = "success";
        var behavior = new AuthorizationBehavior<TestUnauthorizedRequest, string>(_mockHttpContextAccessor.Object,
            Mock.Of<ILogger<AuthorizationBehavior<TestUnauthorizedRequest, string>>>());

        // Act
        var result = await behavior.Handle(request, () => Task.FromResult(expectedResponse), CancellationToken.None);

        // Assert
        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var request = new TestAuthorizedRequest();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(); // Not authenticated
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        result.Should().BeNull(); // Unauthorized response
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUserLacksRequiredRole()
    {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredRoles = new[] { "Admin" } };
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        result.Should().BeNull(); // Forbidden response
    }

    [Fact]
    public async Task Handle_ShouldContinuePipeline_WhenUserHasRequiredRole()
    {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredRoles = new[] { "Admin" } };
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenUserLacksRequiredPermission()
    {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredPermissions = new[] { "read:users" } };
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("permission", "write:users")
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        result.Should().BeNull(); // Forbidden response
    }

    [Fact]
    public async Task Handle_ShouldContinuePipeline_WhenUserHasRequiredPermission()
    {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredPermissions = new[] { "read:users" } };
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("permission", "read:users")
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var result = await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task Handle_ShouldLogWarning_WhenUnauthorizedAccessAttempted()
    {
        // Arrange
        var request = new TestAuthorizedRequest();
        var httpContext = new DefaultHttpContext();
        var identity = new ClaimsIdentity(); // Not authenticated
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unauthorized access attempt")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogWarning_WhenInsufficientPermissions()
    {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredRoles = new[] { "Admin" } };
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "test");
        httpContext.User = new ClaimsPrincipal(identity);

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        await _behavior.Handle(request, () => Task.FromResult("success"), CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Insufficient permissions")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}

/// <summary>
/// Test request that implements IAuthorizedRequest
/// </summary>
public class TestAuthorizedRequest : IBaseRequest, IAuthorizedRequest
{
    public string[]? RequiredRoles { get; set; }
    public string[]? RequiredPermissions { get; set; }
}

/// <summary>
/// Test request that does not implement IAuthorizedRequest
/// </summary>
public class TestUnauthorizedRequest : IBaseRequest
{
}