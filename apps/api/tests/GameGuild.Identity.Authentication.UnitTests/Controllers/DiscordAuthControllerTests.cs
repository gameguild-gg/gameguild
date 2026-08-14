using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public class DiscordAuthControllerTests
{
    [Fact]
    public async Task DiscordAuthorize_ShouldReturnOk_WithAuthUrlAndState()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<DiscordSignInCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DiscordSignInResponse
            {
                AuthUrl = "https://discord.com/oauth2/authorize?client_id=x",
                State = "state-abc"
            });

        var controller = new AuthController(sender.Object);

        var result = await controller.DiscordAuthorize(
            new DiscordAuthorizeRequest { RedirectUri = "https://web.example.com/api/auth/callback/discord" },
            CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<DiscordSignInResponse>().Subject;
        response.State.Should().Be("state-abc");
    }

    [Fact]
    public async Task DiscordAuthorize_ShouldReturn503ProblemDetails_WhenDiscordNotConfigured()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<DiscordSignInCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("OAuth client ID not configured for provider: discord"));

        var controller = new AuthController(sender.Object);

        var result = await controller.DiscordAuthorize(
            new DiscordAuthorizeRequest { RedirectUri = "https://web.example.com/api/auth/callback/discord" },
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(503);
        problem.Title.Should().Be("Discord OAuth is not configured");
    }

    [Fact]
    public async Task DiscordCallback_ShouldReturn401ProblemDetails_WhenSenderThrowsUnauthorizedAccessException()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<DiscordCallbackCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("Discord account has no email"));

        var controller = new AuthController(sender.Object);

        var result = await controller.DiscordCallback(
            new DiscordCallbackRequestDto
            {
                Code = "discord-code",
                State = "state-123",
                RedirectUri = "https://web.example.com/api/auth/callback/discord"
            },
            CancellationToken.None);

        var unauthorized = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
        var problem = unauthorized.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(401);
        problem.Detail.Should().Be("Discord account has no email");
    }

    [Fact]
    public async Task DiscordCallback_ShouldReturn503ProblemDetails_WhenDiscordNotConfigured()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<DiscordCallbackCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Discord OAuth client ID or client secret not configured"));

        var controller = new AuthController(sender.Object);

        var result = await controller.DiscordCallback(
            new DiscordCallbackRequestDto
            {
                Code = "discord-code",
                State = "state-123",
                RedirectUri = "https://web.example.com/api/auth/callback/discord"
            },
            CancellationToken.None);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(503);
        var problem = objectResult.Value.Should().BeOfType<ProblemDetails>().Subject;
        problem.Status.Should().Be(503);
        problem.Title.Should().Be("Discord OAuth is not configured");
    }
}
