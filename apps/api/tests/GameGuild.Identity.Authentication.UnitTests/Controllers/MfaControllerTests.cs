using System.Security.Claims;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

public class MfaControllerTests
{
    private static MfaController CreateController(ISender sender, Guid userId, string email = "user@example.com")
    {
        return new MfaController(mfaService: null!, sender)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Email, email)
                    ],
                    "Bearer"))
                }
            }
        };
    }

    [Fact]
    public async Task InitiateTotpSetup_ShouldReturnBackupCodes_GeneratedDuringSetup()
    {
        // Issue #1195: setup generates and stores the initial backup-code set; the
        // response must surface it instead of an empty array.
        var userId = Guid.NewGuid();
        var generatedCodes = new[] { "CODE-1", "CODE-2", "CODE-3" };
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(
                It.Is<InitiateMfaSetupCommand>(command => command.UserId == userId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaSetupResult
            {
                Success = true,
                SecretKey = "JBSWY3DPEHPK3PXP",
                QrCodeUrl = "otpauth://totp/GameGuild",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10),
                BackupCodes = generatedCodes
            });

        var controller = CreateController(sender.Object, userId);

        var result = await controller.InitiateTotpSetup(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<MfaSetupResponse>().Subject;
        response.BackupCodes.Should().BeEquivalentTo(generatedCodes);
        response.SecretKey.Should().Be("JBSWY3DPEHPK3PXP");
    }

    [Fact]
    public async Task InitiateTotpSetup_ShouldReturnEmptyBackupCodes_WhenSetupResultHasNone()
    {
        var userId = Guid.NewGuid();
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<InitiateMfaSetupCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MfaSetupResult
            {
                Success = true,
                SecretKey = "JBSWY3DPEHPK3PXP",
                QrCodeUrl = "otpauth://totp/GameGuild",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
            });

        var controller = CreateController(sender.Object, userId);

        var result = await controller.InitiateTotpSetup(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<MfaSetupResponse>().Subject;
        response.BackupCodes.Should().NotBeNull();
        response.BackupCodes.Should().BeEmpty();
    }
}
