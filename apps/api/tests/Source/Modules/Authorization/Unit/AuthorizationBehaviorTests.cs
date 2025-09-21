using System.Security.Claims;
using GameGuild;
using GameGuild.Core.Domain.Identity;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Modules.Authorization.Unit;

/// <summary>
/// Unit tests for AuthorizationBehavior pipeline behavior
/// </summary>
public class AuthorizationBehaviorTests {
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<AuthorizationBehavior<TestAuthorizedRequest, Result<string>>>> _mockLogger;
    private readonly AuthorizationBehavior<TestAuthorizedRequest, Result<string>> _behavior;
    private readonly Mock<HttpContext> _mockHttpContext;
    private readonly ClaimsPrincipal _authenticatedUser;
    private readonly ClaimsPrincipal _unauthenticatedUser;

    public AuthorizationBehaviorTests() {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<AuthorizationBehavior<TestAuthorizedRequest, Result<string>>>>();
        _behavior = new AuthorizationBehavior<TestAuthorizedRequest, Result<string>>(_mockHttpContextAccessor.Object, _mockLogger.Object);
        _mockHttpContext = new Mock<HttpContext>();

        // Setup authenticated user with roles and permissions
        _authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(new[] {
      new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
      new Claim(ClaimTypes.Email, "test@example.com"),
      new Claim(ClaimTypes.Role, "User"),
      new Claim(ClaimTypes.Role, "Admin"),
      new Claim("permission", "read:users"),
      new Claim("permission", "write:users")
    }, "test"));

        // Setup unauthenticated user
        _unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_mockHttpContext.Object);
    }

    [Fact]
    public async Task Handle_NonAuthorizedRequest_ShouldPassThrough() {
        // Arrange
        var request = new TestNonAuthorizedRequest();
        var nextCalled = false;
        Task<Result<string>> Next() {
            nextCalled = true;
            return Task.FromResult(Result.Success("success"));
        }

        // Create a behavior for the non-authorized request type
        var nonAuthorizedBehavior = new AuthorizationBehavior<TestNonAuthorizedRequest, Result<string>>(_mockHttpContextAccessor.Object, new Mock<ILogger<AuthorizationBehavior<TestNonAuthorizedRequest, Result<string>>>>().Object);

        // Act
        var result = await nonAuthorizedBehavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("success", result.Value);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized() {
        // Arrange
        var request = new TestAuthorizedRequest();
        _mockHttpContext.Setup(x => x.User).Returns(_unauthenticatedUser);

        Task<Result<string>> Next() => Task.FromResult(Result.Success("should not reach"));

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Unauthorized", result.Error.Code);
        Assert.Equal("Authentication is required", result.Error.Message);
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithRequiredRole_ShouldSucceed() {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredRoles = new[] { "Admin" } };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        var nextCalled = false;
        Task<Result<string>> Next() {
            nextCalled = true;
            return Task.FromResult(Result.Success("authorized"));
        }

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("authorized", result.Value);
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithoutRequiredRole_ShouldReturnForbidden() {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredRoles = new[] { "SuperAdmin" } };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        Task<Result<string>> Next() => Task.FromResult(Result.Success("should not reach"));

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error.Code);
        Assert.Equal("Insufficient permissions", result.Error.Message);
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithRequiredPermission_ShouldSucceed() {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredPermissions = new[] { "read:users" } };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        var nextCalled = false;
        Task<Result<string>> Next() {
            nextCalled = true;
            return Task.FromResult(Result.Success("authorized"));
        }

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("authorized", result.Value);
    }

    [Fact]
    public async Task Handle_AuthenticatedUserWithoutRequiredPermission_ShouldReturnForbidden() {
        // Arrange
        var request = new TestAuthorizedRequest { RequiredPermissions = new[] { "delete:users" } };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        Task<Result<string>> Next() => Task.FromResult(Result.Success("should not reach"));

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error.Code);
        Assert.Equal("Insufficient permissions", result.Error.Message);
    }

    [Fact]
    public async Task Handle_CustomAuthorizationFails_ShouldReturnForbidden() {
        // Arrange
        var request = new TestAuthorizedRequest {
            CustomAuthorizationResult = false,
            RequiredRoles = new[] { "Admin" } // This would pass, but custom auth fails
        };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        Task<Result<string>> Next() => Task.FromResult(Result.Success("should not reach"));

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Authorization.Forbidden", result.Error.Code);
        Assert.Equal("Insufficient permissions", result.Error.Message);
    }

    [Fact]
    public async Task Handle_AllAuthorizationChecksPass_ShouldSucceed() {
        // Arrange
        var request = new TestAuthorizedRequest {
            RequiredRoles = new[] { "Admin" },
            RequiredPermissions = new[] { "read:users" },
            CustomAuthorizationResult = true
        };
        _mockHttpContext.Setup(x => x.User).Returns(_authenticatedUser);

        var nextCalled = false;
        Task<Result<string>> Next() {
            nextCalled = true;
            return Task.FromResult(Result.Success("fully authorized"));
        }

        // Act
        var result = await _behavior.Handle(request, Next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.True(result.IsSuccess);
        Assert.Equal("fully authorized", result.Value);
    }

    // Test helper classes
    public class TestNonAuthorizedRequest : IBaseRequest {
    }

    public class TestAuthorizedRequest : IAuthorizedRequest, IBaseRequest {
        public string[]? RequiredRoles { get; set; }
        public string[]? RequiredPermissions { get; set; }
        public bool CustomAuthorizationResult { get; set; } = true;

        public Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, CancellationToken cancellationToken) {
            return Task.FromResult(CustomAuthorizationResult);
        }
    }
}
