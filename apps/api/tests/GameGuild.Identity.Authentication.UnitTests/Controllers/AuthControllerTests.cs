using System.Security.Claims;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task ChangePassword_ShouldUseMappedNameIdentifierClaim()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(
                It.Is<ChangePasswordCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PasswordChangeResult
            {
                Success = true,
                Message = "Password changed successfully"
            });

        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                        "Bearer"))
                }
            }
        };

        var result = await controller.ChangePassword(new PasswordChangeRequest
        {
            CurrentPassword = "Old!Password123",
            NewPassword = "New!Password123",
            ConfirmPassword = "New!Password123",
            RevokeOtherSessions = false
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        sender.VerifyAll();
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
}
