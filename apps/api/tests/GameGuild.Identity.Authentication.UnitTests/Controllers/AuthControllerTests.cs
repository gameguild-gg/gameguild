using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task LocalSignUp_ShouldReturnCreatedBody_WhenSenderSucceeds()
    {
        var response = new SignInResponse
        {
            Success = true,
            Message = "Sign-up successful",
            AccessToken = "access-token",
            RefreshToken = "refresh-token",
            UserId = Guid.NewGuid(),
            Email = "new@example.com",
            SessionId = Guid.NewGuid()
        };
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<LocalSignUpCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var controller = new AuthController(sender.Object);

        var result = await controller.LocalSignUp(new LocalSignUpRequest
        {
            Email = "new@example.com",
            Password = "Password123!",
            Username = "newuser"
        }, CancellationToken.None);

        result.Should().NotBeOfType<CreatedAtActionResult>();
        var created = result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        created.Value.Should().BeSameAs(response);
    }

    [Fact]
    public async Task RefreshToken_ShouldReturnUnauthorized_WhenSenderThrowsUnauthorizedAccessException()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        var controller = new AuthController(sender.Object);

        var result = await controller.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        }, CancellationToken.None);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problem = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(401);
        problem.Detail.Should().Be("Invalid refresh token");
    }

    [Fact]
    public async Task LocalSignIn_ShouldReturnUnauthorized_WhenSenderThrowsUnauthorizedAccessException()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<LocalSignInCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        var controller = new AuthController(sender.Object);

        var result = await controller.LocalSignIn(new LocalSignInRequest
        {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        }, CancellationToken.None);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problem = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(401);
        problem.Detail.Should().Be("Invalid credentials");
    }

    [Fact]
    public async Task LocalSignUp_ShouldReturnConflict_WhenUserAlreadyExists()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<LocalSignUpCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        var controller = new AuthController(sender.Object);

        var result = await controller.LocalSignUp(new LocalSignUpRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            Username = "existing"
        }, CancellationToken.None);

        var conflict = result.Should().BeOfType<ConflictObjectResult>().Subject;
        var problem = conflict.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(409);
        problem.Detail.Should().Be("User already exists");
    }

    [Fact]
    public async Task ChangePassword_ShouldUseNameIdentifierClaim_WhenSubjectClaimWasMapped()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(
                It.Is<ChangePasswordCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordChangeResult { Success = true, Message = "Password changed" });

        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "unit-test"))
                }
            }
        };

        var result = await controller.ChangePassword(new PasswordChangeRequest
        {
            CurrentPassword = "Old!Pass123",
            NewPassword = "New!Pass123",
            ConfirmPassword = "New!Pass123"
        }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<PasswordChangeResult>().Which.Success.Should().BeTrue();
        sender.Verify(s => s.Send(It.IsAny<ChangePasswordCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
