using System.Security.Claims;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Identity;

public class UserContextTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly DefaultHttpContext _httpContext;
    private readonly UserContext _userContext;

    public UserContextTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(_httpContext);
        _userContext = new UserContext(_mockHttpContextAccessor.Object);
    }

    [Fact]
    public void Constructor_Should_Throw_When_HttpContextAccessor_Is_Null()
    {
        var act = () => new UserContext(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void UserId_Should_Return_Null_When_No_User()
    {
        _httpContext.User = null!;
        var userId = _userContext.UserId;
        userId.Should().BeNull();
    }

    [Fact]
    public void UserId_Should_Extract_From_NameIdentifier_Claim()
    {
        var expectedUserId = Guid.NewGuid();
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, expectedUserId.ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var userId = _userContext.UserId;
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_Should_Extract_From_Sub_Claim()
    {
        var expectedUserId = Guid.NewGuid();
        var claims = new List<Claim> { new("sub", expectedUserId.ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var userId = _userContext.UserId;
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_Should_Extract_From_UserId_Claim()
    {
        var expectedUserId = Guid.NewGuid();
        var claims = new List<Claim> { new("userId", expectedUserId.ToString()) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var userId = _userContext.UserId;
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_Should_Prioritize_NameIdentifier_Over_Other_Claims()
    {
        var expectedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new("sub", otherUserId.ToString())
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var userId = _userContext.UserId;
        userId.Should().Be(expectedUserId);
    }

    [Fact]
    public void UserId_Should_Return_Null_For_Invalid_Guid()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "not-a-guid") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var userId = _userContext.UserId;
        userId.Should().BeNull();
    }

    [Fact]
    public void Email_Should_Extract_From_Email_Claim()
    {
        var expectedEmail = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, expectedEmail) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var email = _userContext.Email;
        email.Should().Be(expectedEmail);
    }

    [Fact]
    public void Name_Should_Extract_From_Name_Claim()
    {
        var expectedName = "John Doe";
        var claims = new List<Claim> { new(ClaimTypes.Name, expectedName) };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var name = _userContext.Name;
        name.Should().Be(expectedName);
    }

    [Fact]
    public void IsAuthenticated_Should_Return_True_When_User_Is_Authenticated()
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "John Doe") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var isAuthenticated = _userContext.IsAuthenticated;
        isAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void IsAuthenticated_Should_Return_False_When_User_Is_Not_Authenticated()
    {
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        var isAuthenticated = _userContext.IsAuthenticated;
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void Claims_Should_Return_All_User_Claims()
    {
        var expectedClaims = new List<Claim>
        {
            new(ClaimTypes.Name, "John Doe"),
            new(ClaimTypes.Email, "john@example.com"),
            new(ClaimTypes.Role, "Admin")
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(expectedClaims, "test"));
        var claims = _userContext.Claims.ToList();
        claims.Should().HaveCount(3);
    }

    [Fact]
    public void Roles_Should_Return_All_Role_Claims()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Role, "Admin"),
            new(ClaimTypes.Role, "User")
        };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var roles = _userContext.Roles.ToList();
        roles.Should().HaveCount(2);
        roles.Should().Contain("Admin");
        roles.Should().Contain("User");
    }

    [Fact]
    public void IsInRole_Should_Return_True_When_User_Has_Role()
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        _userContext.IsInRole("Admin").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_Should_Return_False_When_User_Does_Not_Have_Role()
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "User") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        _userContext.IsInRole("Admin").Should().BeFalse();
    }

    [Fact]
    public void IsInRole_Should_Be_Case_Insensitive()
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        _userContext.IsInRole("admin").Should().BeTrue();
        _userContext.IsInRole("ADMIN").Should().BeTrue();
    }

    [Fact]
    public void IsInRole_Should_Return_False_For_Empty_Role()
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Admin") };
        _httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        _userContext.IsInRole("").Should().BeFalse();
        _userContext.IsInRole("   ").Should().BeFalse();
    }
}
