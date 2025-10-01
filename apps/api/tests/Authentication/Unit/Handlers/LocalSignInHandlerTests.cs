using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Authentication.Unit.Handlers;

/// <summary>
/// Unit tests for the LocalSignInHandler
/// Tests the handling of local sign-in commands
/// </summary>
public class LocalSignInHandlerTests
{
    private readonly Mock<IAuthService> _mockAuthService;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<LocalSignInHandler>> _mockLogger;
    private readonly LocalSignInHandler _handler;

    public LocalSignInHandlerTests()
    {
        _mockAuthService = new Mock<IAuthService>();
        _mockMediator = new Mock<IMediator>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<LocalSignInHandler>>();

        _handler = new LocalSignInHandler(
            _mockAuthService.Object,
            _mockMediator.Object,
            _mockHttpContextAccessor.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenSignInIsSuccessful()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "password123",
            TenantId = Guid.NewGuid()
        };

        var signInResponse = new SignInResponse
        {
            User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com" },
            AccessToken = "jwt-token",
            RefreshToken = "refresh-token"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Test User Agent";

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ReturnsAsync(signInResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(signInResponse);

        _mockAuthService.Verify(x => x.LocalSignInAsync(It.Is<LocalSignInRequest>(r =>
            r.Email == command.Email &&
            r.Password == command.Password &&
            r.TenantId == command.TenantId)), Times.Once);

        _mockMediator.Verify(x => x.Publish(It.IsAny<UserSignedInEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenUnauthorizedAccessExceptionIsThrown()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "wrongpassword",
            TenantId = null
        };

        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.InvalidCredentials");

        _mockMediator.Verify(x => x.Publish(It.IsAny<AuthenticationFailedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenGenericExceptionIsThrown()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "password123",
            TenantId = null
        };

        var httpContext = new DefaultHttpContext();
        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Authentication.SignInFailed");

        _mockMediator.Verify(x => x.Publish(It.IsAny<AuthenticationFailedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldHandleNullHttpContext()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var signInResponse = new SignInResponse
        {
            User = new UserDto { Id = Guid.NewGuid(), Email = "test@example.com" },
            AccessToken = "jwt-token",
            RefreshToken = "refresh-token"
        };

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ReturnsAsync(signInResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(signInResponse);
    }

    [Fact]
    public async Task Handle_ShouldPublishUserSignedInEvent_WithCorrectData()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "password123"
        };

        var userId = Guid.NewGuid();
        var signInResponse = new SignInResponse
        {
            User = new UserDto { Id = userId, Email = "test@example.com" },
            AccessToken = "jwt-token",
            RefreshToken = "refresh-token"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Test User Agent";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ReturnsAsync(signInResponse);

        UserSignedInEvent? publishedEvent = null;
        _mockMediator.Setup(x => x.Publish(It.IsAny<UserSignedInEvent>(), It.IsAny<CancellationToken>()))
                    .Callback<object, CancellationToken>((evt, _) => publishedEvent = evt as UserSignedInEvent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.UserId.Should().Be(userId);
        publishedEvent.Email.Should().Be("test@example.com");
        publishedEvent.SignInMethod.Should().Be("Local");
        publishedEvent.IpAddress.Should().Be("192.168.1.1");
        publishedEvent.UserAgent.Should().Be("Test User Agent");
    }

    [Fact]
    public async Task Handle_ShouldPublishAuthenticationFailedEvent_WhenExceptionOccurs()
    {
        // Arrange
        var command = new LocalSignInCommand
        {
            Email = "test@example.com",
            Password = "wrongpassword"
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.UserAgent = "Test User Agent";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        _mockAuthService.Setup(x => x.LocalSignInAsync(It.IsAny<LocalSignInRequest>()))
                       .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        AuthenticationFailedEvent? publishedEvent = null;
        _mockMediator.Setup(x => x.Publish(It.IsAny<AuthenticationFailedEvent>(), It.IsAny<CancellationToken>()))
                    .Callback<object, CancellationToken>((evt, _) => publishedEvent = evt as AuthenticationFailedEvent);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        publishedEvent.Should().NotBeNull();
        publishedEvent!.Email.Should().Be("test@example.com");
        publishedEvent.Reason.Should().Be("Invalid credentials");
        publishedEvent.IpAddress.Should().Be("192.168.1.1");
        publishedEvent.UserAgent.Should().Be("Test User Agent");
    }
}