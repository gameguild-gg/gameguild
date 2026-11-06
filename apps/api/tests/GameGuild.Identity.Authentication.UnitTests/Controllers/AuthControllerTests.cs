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

    [Fact]
    public async Task RefreshToken_ShouldReturnGenericUnauthorized_ForCompletedContainmentDenial()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        sender.Setup(service => service.Send(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignInResponse { Success = false, Message = "internal denial context" });
        var controller = new AuthController(sender.Object);
        var result = Assert.IsType<UnauthorizedObjectResult>(await controller.RefreshToken(
            new RefreshTokenRequest { RefreshToken = Guid.NewGuid().ToString("N") }, CancellationToken.None));
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(401, problem.Status);
        Assert.Equal("Invalid refresh token", problem.Detail);
        Assert.DoesNotContain("internal denial context", problem.Detail, StringComparison.Ordinal);
        sender.VerifyAll();
    }

    [Fact]
    public async Task VerifyWeb3Signature_ShouldForwardSiweMessageNonceAndChainId()
    {
        var sender = new Mock<ISender>();
        VerifyWeb3SignatureCommand? capturedCommand = null;
        sender
            .Setup(service => service.Send(It.IsAny<VerifyWeb3SignatureCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<SignInResponse>, CancellationToken>((command, _) => capturedCommand = (VerifyWeb3SignatureCommand)command)
            .ReturnsAsync(new SignInResponse { Success = true, Message = "verified", Email = "wallet@example.com" });
        var controller = new AuthController(sender.Object);

        var result = await controller.VerifyWeb3Signature(new Web3VerifyRequest
        {
            WalletAddress = "0x1234567890abcdef1234567890abcdef12345678",
            Challenge = "full SIWE challenge message",
            Signature = "0xsigned-message",
            Nonce = "nonce-12345678",
            ChainId = "5"
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.Challenge.Should().Be("full SIWE challenge message");
        capturedCommand.Signature.Should().Be("0xsigned-message");
        capturedCommand.Nonce.Should().Be("nonce-12345678");
        capturedCommand.ChainId.Should().Be("5");
    }

    // ── Issue #1116: provider outage answers 503 and advertises the magic-link fallback ──

    [Fact]
    public async Task GoogleIdTokenSignIn_ShouldReturnServiceUnavailableWithMagicLinkFallback_WhenProviderIsDown()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<GoogleIdTokenSignInCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OAuthProviderUnavailableException(
                "GoogleIdToken",
                new HttpRequestException("connection refused")));

        var controller = new AuthController(sender.Object);

        var result = await controller.GoogleIdTokenSignIn(new GoogleIdTokenRequestDto
        {
            IdToken = "id-token"
        }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(503);
        problem.Extensions.Should().ContainKey("magicLinkFallback").WhoseValue.Should().Be(true);
        problem.Extensions.Should().ContainKey("errorCode").WhoseValue.Should().Be("oauth_provider_unavailable");
        problem.Extensions.Should().ContainKey("provider").WhoseValue.Should().Be("GoogleIdToken");
    }

    [Fact]
    public async Task GitHubCallback_ShouldReturnServiceUnavailableWithMagicLinkFallback_WhenProviderIsDown()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<GitHubCallbackCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OAuthProviderUnavailableException(
                "GitHub",
                new HttpRequestException("connection refused")));

        var controller = new AuthController(sender.Object);

        var result = await controller.GitHubCallback(new GitHubCallbackRequestDto
        {
            Code = "callback-code"
        }, CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Extensions.Should().ContainKey("magicLinkFallback").WhoseValue.Should().Be(true);
        problem.Extensions.Should().ContainKey("errorCode").WhoseValue.Should().Be("oauth_provider_unavailable");
        problem.Extensions.Should().ContainKey("provider").WhoseValue.Should().Be("GitHub");
    }

    [Fact]
    public async Task SignOut_ShouldMapClaimsAndBodyToLogoutCommand()
    {
        var userId = Guid.NewGuid();
        const string jti = "current-jti";
        const long expUnix = 1900000000;
        var expectedExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;
        LogoutCommand? capturedCommand = null;
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<LogoutResponse>, CancellationToken>((command, _) => capturedCommand = (LogoutCommand)command)
            .ReturnsAsync(new LogoutResponse { Success = true, Message = "Successfully logged out", SessionsInvalidated = 1 });

        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                            new Claim("jti", jti),
                            new Claim("exp", expUnix.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        ],
                        "Bearer"))
                }
            }
        };

        var result = await controller.SignOut(new SignOutRequest { LogoutEverywhere = true }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<LogoutResponse>().Subject;
        payload.Success.Should().BeTrue();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.UserId.Should().Be(userId);
        capturedCommand.CurrentTokenJti.Should().Be(jti);
        capturedCommand.CurrentTokenExpiresAt.Should().Be(expectedExpiresAt);
        capturedCommand.LogoutEverywhere.Should().BeTrue();
        sender.VerifyAll();
    }

    [Fact]
    public async Task SignOut_ShouldDefaultToCurrentTokenOnly_WhenBodyOmitted()
    {
        var userId = Guid.NewGuid();
        LogoutCommand? capturedCommand = null;
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<LogoutResponse>, CancellationToken>((command, _) => capturedCommand = (LogoutCommand)command)
            .ReturnsAsync(new LogoutResponse { Success = true, Message = "Successfully logged out", SessionsInvalidated = 1 });

        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        [new Claim("sub", userId.ToString())],
                        "Bearer"))
                }
            }
        };

        var result = await controller.SignOut(null, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        capturedCommand.Should().NotBeNull();
        capturedCommand!.LogoutEverywhere.Should().BeFalse();
        capturedCommand.CurrentTokenJti.Should().BeNull();
        capturedCommand.CurrentTokenExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task SignOut_ShouldReturnUnauthorized_WhenUserContextIsMissing()
    {
        var sender = new Mock<ISender>();
        var controller = new AuthController(sender.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity("Bearer"))
                }
            }
        };

        var result = await controller.SignOut(null, CancellationToken.None);

        result.Should().BeOfType<UnauthorizedObjectResult>();
        sender.Verify(service => service.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SignOut_ShouldReturnServerFailurePayload_WhenLogoutHandlerFails()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(service => service.Send(It.IsAny<LogoutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LogoutResponse { Success = false, Message = "An error occurred during logout", SessionsInvalidated = 0 });

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

        var result = await controller.SignOut(null, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<LogoutResponse>().Subject;
        payload.Success.Should().BeFalse();
        payload.SessionsInvalidated.Should().Be(0);
    }
}
