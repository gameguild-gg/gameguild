using System.Security.Claims;
using FluentAssertions;
using GameGuild.Source.Modules.Authorization.Identity;
using Xunit;

namespace GameGuild.Tests.Authorization.Unit.Services;

/// <summary>
/// Unit tests for the ClaimsPrincipalExtensions - testing actual existing methods
/// </summary>
public class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void GetUserId_ShouldReturnUserId_WhenUserIdClaimExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("user_id", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetUserId_ShouldReturnNull_WhenUserIdClaimDoesNotExist()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_ShouldReturnTenantId_WhenTenantIdClaimExists()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("tenant_id", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetTenantId_ShouldReturnNull_WhenTenantIdClaimDoesNotExist()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetEmail_ShouldReturnEmail_WhenEmailClaimExists()
    {
        // Arrange
        const string email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetEmail();

        // Assert
        result.Should().Be(email);
    }

    [Fact]
    public void GetEmail_ShouldReturnNull_WhenEmailClaimDoesNotExist()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetEmail();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetDisplayName_ShouldReturnDisplayName_WhenNameClaimExists()
    {
        // Arrange
        const string displayName = "John Doe";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, displayName)
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetDisplayName();

        // Assert
        result.Should().Be(displayName);
    }

    [Fact]
    public void GetDisplayName_ShouldReturnNull_WhenNameClaimDoesNotExist()
    {
        // Arrange
        var claims = new List<Claim>();
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetDisplayName();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_ShouldPrioritizeUserIdClaim_WhenMultipleClaimsExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var subId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("user_id", userId.ToString()),
            new("sub", subId.ToString()),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().Be(userId);
    }

    [Fact]
    public void GetTenantId_ShouldPrioritizeTenantIdClaim_WhenMultipleClaimsExist()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tidId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("tenant_id", tenantId.ToString()),
            new("tid", tidId.ToString()),
            new("http://schemas.microsoft.com/identity/claims/tenantid", Guid.NewGuid().ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().Be(tenantId);
    }

    [Fact]
    public void GetUserId_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTenantId_ShouldReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new("tenant_id", tenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims); // Not authenticated
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.GetTenantId();

        // Assert
        result.Should().BeNull();
    }
}