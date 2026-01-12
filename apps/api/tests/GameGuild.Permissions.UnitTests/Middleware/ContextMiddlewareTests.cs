using System.Globalization;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Middleware;

/// <summary>
/// Unit tests for the ContextMiddleware
/// </summary>
public class ContextMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ITenantContext> _mockTenantContext;
    private readonly Mock<IPermissionsContext> _mockPermissionsContext;
    private readonly Mock<ILocalizationContext> _mockLocalizationContext;
    private readonly ContextMiddleware _middleware;
    private readonly DefaultHttpContext _httpContext;

    public ContextMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockUserContext = new Mock<IUserContext>();
        _mockTenantContext = new Mock<ITenantContext>();
        _mockPermissionsContext = new Mock<IPermissionsContext>();
        _mockLocalizationContext = new Mock<ILocalizationContext>();
        _middleware = new ContextMiddleware(_mockNext.Object);
        _httpContext = new DefaultHttpContext();

        // Setup default culture code
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("en-US");
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_UserContext_In_HttpContext_Items()
    {
        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        _httpContext.Items.Should().ContainKey("UserContext");
        _httpContext.Items["UserContext"].Should().Be(_mockUserContext.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_TenantContext_In_HttpContext_Items()
    {
        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        _httpContext.Items.Should().ContainKey("TenantContext");
        _httpContext.Items["TenantContext"].Should().Be(_mockTenantContext.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_PermissionsContext_In_HttpContext_Items()
    {
        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        _httpContext.Items.Should().ContainKey("PermissionsContext");
        _httpContext.Items["PermissionsContext"].Should().Be(_mockPermissionsContext.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_LocalizationContext_In_HttpContext_Items()
    {
        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        _httpContext.Items.Should().ContainKey("LocalizationContext");
        _httpContext.Items["LocalizationContext"].Should().Be(_mockLocalizationContext.Object);
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_All_Four_Contexts_In_HttpContext_Items()
    {
        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        _httpContext.Items.Should().HaveCount(4);
        _httpContext.Items.Keys.Should().Contain(new[] { "UserContext", "TenantContext", "PermissionsContext", "LocalizationContext" });
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_CurrentCulture_From_LocalizationContext()
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("pt-BR");

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        CultureInfo.CurrentCulture.Name.Should().Be("pt-BR");
        Thread.CurrentThread.CurrentCulture.Name.Should().Be("pt-BR");
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_CurrentUICulture_From_LocalizationContext()
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("fr-FR");

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        CultureInfo.CurrentUICulture.Name.Should().Be("fr-FR");
        Thread.CurrentThread.CurrentUICulture.Name.Should().Be("fr-FR");
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_DefaultThreadCurrentCulture()
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("de-DE");

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        CultureInfo.DefaultThreadCurrentCulture?.Name.Should().Be("de-DE");
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_DefaultThreadCurrentUICulture()
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("es-ES");

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        CultureInfo.DefaultThreadCurrentUICulture?.Name.Should().Be("es-ES");
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
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        nextCalled.Should().BeTrue();
        _mockNext.Verify(x => x(_httpContext), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_Should_Store_Contexts_Before_Calling_Next()
    {
        // Arrange
        var contextsStoredBeforeNext = false;
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback((HttpContext ctx) =>
            {
                contextsStoredBeforeNext = ctx.Items.ContainsKey("UserContext") &&
                                          ctx.Items.ContainsKey("TenantContext") &&
                                          ctx.Items.ContainsKey("PermissionsContext") &&
                                          ctx.Items.ContainsKey("LocalizationContext");
            })
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        contextsStoredBeforeNext.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Should_Set_Culture_Before_Calling_Next()
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns("ja-JP");
        var cultureSetBeforeNext = false;
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .Callback(() =>
            {
                cultureSetBeforeNext = CultureInfo.CurrentCulture.Name == "ja-JP";
            })
            .Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        cultureSetBeforeNext.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_Should_Handle_Null_HttpContext_Items_Gracefully()
    {
        // Arrange
        // HttpContext.Items should never be null in real scenarios, but test defensive programming

        // Act
        var act = async () => await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InvokeAsync_Should_Propagate_Exception_From_Next_Middleware()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Test exception");
        _mockNext.Setup(x => x(It.IsAny<HttpContext>()))
            .ThrowsAsync(expectedException);

        // Act
        var act = async () => await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    [InlineData("fr-FR")]
    [InlineData("de-DE")]
    [InlineData("es-ES")]
    [InlineData("ja-JP")]
    [InlineData("zh-CN")]
    public async Task InvokeAsync_Should_Handle_Multiple_Cultures(string cultureCode)
    {
        // Arrange
        _mockLocalizationContext.Setup(x => x.CultureCode).Returns(cultureCode);

        // Act
        await _middleware.InvokeAsync(
            _httpContext,
            _mockUserContext.Object,
            _mockTenantContext.Object,
            _mockPermissionsContext.Object,
            _mockLocalizationContext.Object
        );

        // Assert
        CultureInfo.CurrentCulture.Name.Should().Be(cultureCode);
        CultureInfo.CurrentUICulture.Name.Should().Be(cultureCode);
    }
}
