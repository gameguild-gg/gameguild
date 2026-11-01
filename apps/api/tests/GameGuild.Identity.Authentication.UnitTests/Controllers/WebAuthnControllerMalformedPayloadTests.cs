using System.Security.Claims;
using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Controllers;

/// <summary>
///     Controller mapping for malformed WebAuthn payloads (issue #1130): the service
///     contract (pinned in <see cref="Services.WebAuthnMalformedAttestationTests" />) is
///     that any corrupted / empty / oversized attestation yields
///     <see cref="WebAuthnRegistrationResult.Success" /> == false with an error message.
///     These tests pin the second half of the contract: the controller answers that
///     failure result with HTTP 400 — never 500 — for every malformed-payload shape.
/// </summary>
public sealed class WebAuthnControllerMalformedPayloadTests
{
    private static readonly Guid UserId = Guid.NewGuid();

    private static WebAuthnController CreateController(ISender sender)
    {
        var controller = new WebAuthnController(Mock.Of<IWebAuthnService>(), sender);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", UserId.ToString())], "TestScheme"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
        return controller;
    }

    private static Mock<ISender> SenderReturning(WebAuthnRegistrationResult result) 
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<CompleteWebAuthnRegistrationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return sender;
    }

    private static CompleteWebAuthnRegistrationRequest BodyWith(string attestation) => new()
    {
        AttestationResponse = attestation,
        FriendlyName = "Malformed test key"
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{corrupted attestation")]
    [InlineData("{\"id\":123,\"rawId\":")]
    [InlineData("null")]
    [InlineData("{\"foo\":\"bar\"}")]
    public async Task CompleteRegistration_FailedServiceResult_IsMappedToBadRequest_Never500(string payload)
    {
        // The result below is exactly what WebAuthnRegistrationService.CompleteRegistrationAsync
        // returns for this payload shape (see WebAuthnMalformedAttestationTests).
        var sender = SenderReturning(new WebAuthnRegistrationResult
        {
            Success = false,
            Error = "Failed to complete WebAuthn registration"
        });

        var response = await CreateController(sender.Object).CompleteRegistration(BodyWith(payload));

        var badRequest = response.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        badRequest.Value.Should().BeOfType<WebAuthnRegistrationResult>()
            .Which.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CompleteAuthentication_FailedServiceResult_IsMappedToBadRequest_Never500()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<CompleteWebAuthnAuthenticationCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebAuthnAuthenticationResult { Success = false, Error = "Invalid assertion response" });

        var controller = CreateController(sender.Object);
        var request = new CompleteWebAuthnAuthenticationRequest { AssertionResponse = "{corrupted assertion" };

        var response = await controller.CompleteAuthentication(request);

        var badRequest = response.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task CompleteRegistration_UnauthenticatedCaller_Gets401WithoutInvokingTheHandler()
    {
        var sender = new Mock<ISender>(MockBehavior.Strict);
        var controller = new WebAuthnController(Mock.Of<IWebAuthnService>(), sender.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext() // no sub claim
        };

        var response = await controller.CompleteRegistration(BodyWith("{malformed"));

        response.Result.Should().BeOfType<UnauthorizedResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }
}
