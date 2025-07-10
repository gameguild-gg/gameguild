using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MediatR;
using Moq;
using Xunit;
using FluentValidation;
using System.Security.Claims;

namespace GameGuild.Modules.Auth.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AuthController using CQRS pattern
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act & Assert
        Assert.NotNull(_controller);
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthController(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AuthController(_mediatorMock.Object, null!));
    }

    [Fact]
    public async Task LocalSignUp_ValidRequest_ReturnsCreatedResult()
    {
        // Arrange
        var request = new LocalSignUpRequestDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            Username = "testuser"
        };

        var expectedResponse = new SignInResponseDto
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<LocalSignUpCommand>(), default))
                    .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.LocalSignUp(request);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(expectedResponse, createdResult.Value);
        _mediatorMock.Verify(m => m.Send(It.IsAny<LocalSignUpCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task LocalSignUp_ValidationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new LocalSignUpRequestDto { Email = "invalid-email" };
        var validationException = new ValidationException("Validation failed");

        _mediatorMock.Setup(m => m.Send(It.IsAny<LocalSignUpCommand>(), default))
                    .ThrowsAsync(validationException);

        // Act
        var result = await _controller.LocalSignUp(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("Validation Error", problemDetails.Title);
    }

    [Fact]
    public async Task LocalSignIn_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new LocalSignInRequestDto
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var expectedResponse = new SignInResponseDto
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<LocalSignInCommand>(), default))
                    .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.LocalSignIn(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
        _mediatorMock.Verify(m => m.Send(It.IsAny<LocalSignInCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task LocalSignIn_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LocalSignInRequestDto
        {
            Email = "test@example.com",
            Password = "wrong-password"
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<LocalSignInCommand>(), default))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _controller.LocalSignIn(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problemDetails.Status);
        Assert.Equal("Authentication Failed", problemDetails.Title);
    }

    [Fact]
    public async Task RefreshToken_ValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = new RefreshTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };

        var expectedResponse = new SignInResponseDto
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            Expires = DateTime.UtcNow.AddHours(1)
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
                    .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedResponse, okResult.Value);
        _mediatorMock.Verify(m => m.Send(It.IsAny<RefreshTokenCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task RefreshToken_UnauthorizedAccessException_ReturnsUnauthorized()
    {
        // Arrange
        var request = new RefreshTokenRequestDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<RefreshTokenCommand>(), default))
                    .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problemDetails.Status);
        Assert.Equal("Token Refresh Failed", problemDetails.Title);
    }

    [Fact]
    public async Task RevokeToken_ValidRequest_ReturnsNoContent()
    {
        // Arrange
        var request = new RevokeTokenRequestDto
        {
            RefreshToken = "valid-refresh-token"
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeTokenCommand>(), default))
                    .ReturnsAsync(MediatR.Unit.Value);

        // Setup HttpContext for IP address
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mediatorMock.Verify(m => m.Send(It.IsAny<RevokeTokenCommand>(), default), Times.Once);
    }

    [Fact]
    public async Task RevokeToken_ValidationException_ReturnsBadRequest()
    {
        // Arrange
        var request = new RevokeTokenRequestDto
        {
            RefreshToken = ""
        };

        // Setup HttpContext for IP address
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<RevokeTokenCommand>(), default))
                    .ThrowsAsync(new ValidationException("Validation failed"));

        // Act
        var result = await _controller.RevokeToken(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ProblemDetails>(badRequestResult.Value);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("Validation Error", problemDetails.Title);
    }

    [Fact]
    public async Task GetProfile_ValidUser_ReturnsOkResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedProfile = new UserProfileDto
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser"
        };

        _mediatorMock.Setup(m => m.Send(It.IsAny<GetUserProfileQuery>(), default))
                    .ReturnsAsync(expectedProfile);

        // Setup user claims
        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(expectedProfile, okResult.Value);
        _mediatorMock.Verify(m => m.Send(It.IsAny<GetUserProfileQuery>(), default), Times.Once);
    }

    [Fact]
    public async Task GetProfile_InvalidUserClaims_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>(); // No user ID claim
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
        Assert.Equal(401, problemDetails.Status);
        Assert.Equal("Unauthorized", problemDetails.Title);
    }
}
