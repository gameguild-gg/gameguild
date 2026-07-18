using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Controllers;

public class BillingWebhooksControllerTests
{
    [Fact]
    public async Task HandleGooglePayWebhook_Should_Reject_Missing_Authorization()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Google-Cloud-Project-Id"] = "project"
        });

        var result = await controller.HandleGooglePayWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleGooglePayWebhook_Should_Reject_Missing_ProjectId()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token"
        });

        var result = await controller.HandleGooglePayWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleGooglePayWebhook_Should_Return_Ok_When_Processed()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessGooglePayWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookProcessingResult { Processed = true });

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Authorization"] = "Bearer token",
            ["Google-Cloud-Project-Id"] = "project"
        });

        var result = await controller.HandleGooglePayWebhook(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HandleApplePayWebhook_Should_Reject_Missing_MerchantId()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Apple-Pay-Signature"] = "sig"
        });

        var result = await controller.HandleApplePayWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleApplePayWebhook_Should_Return_Ok_When_Processed()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessApplePayWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebhookProcessingResult.Success("evt"));

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Apple-Pay-Merchant-Id"] = "merchant",
            ["Apple-Pay-Signature"] = "sig"
        });

        var result = await controller.HandleApplePayWebhook(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HandleStripeWebhook_Should_Reject_Missing_Signature()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>());

        var result = await controller.HandleStripeWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleStripeWebhook_Should_Return_BadRequest_When_Signature_Invalid()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessStripeWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidWebhookSignatureException("bad"));

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Stripe-Signature"] = "sig"
        });

        var result = await controller.HandleStripeWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleStripeWebhook_Should_Return_BadRequest_When_Verified_Payload_Is_Invalid()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessStripeWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidWebhookPayloadException("mismatch"));
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Stripe-Signature"] = "sig"
        });

        var result = await controller.HandleStripeWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandleStripeWebhook_Should_Return_500_When_Inbox_Or_Processing_Fails()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessStripeWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebhookProcessingResult.Failed("evt", "retry"));
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["Stripe-Signature"] = "sig"
        });

        var result = await controller.HandleStripeWebhook(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public async Task HandlePayPalWebhook_Should_Reject_Missing_Headers()
    {
        var sender = new Mock<ISender>();
        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>());

        var result = await controller.HandlePayPalWebhook(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task HandlePayPalWebhook_Should_Return_Ok_When_Processed()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessPayPalWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebhookProcessingResult.Success("evt"));

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["PayPal-Transmission-Id"] = "tx",
            ["PayPal-Transmission-Time"] = "time",
            ["PayPal-Transmission-Sig"] = "sig"
        });

        var result = await controller.HandlePayPalWebhook(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task HandlePayPalWebhook_Should_Return_500_When_Processing_Fails()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<ProcessPayPalWebhookCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(WebhookProcessingResult.Failed("evt", "oops"));

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>
        {
            ["PayPal-Transmission-Id"] = "tx",
            ["PayPal-Transmission-Time"] = "time",
            ["PayPal-Transmission-Sig"] = "sig"
        });

        var result = await controller.HandlePayPalWebhook(CancellationToken.None);

        result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task GetWebhookEvent_Should_Return_NotFound_When_Missing()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<GetWebhookEventQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingWebhookEventDto?)null);

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>());

        var result = await controller.GetWebhookEvent("evt", CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task RetryWebhookEvent_Should_Return_Ok_When_Success()
    {
        var sender = new Mock<ISender>();
        sender
            .Setup(s => s.Send(It.IsAny<RetryWebhookEventCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WebhookRetryResult { Success = true });

        var controller = CreateController(sender.Object, "{}", new Dictionary<string, string>());

        var result = await controller.RetryWebhookEvent("evt", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    private static BillingWebhooksController CreateController(ISender sender, string body, IDictionary<string, string> headers)
    {
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        foreach (var header in headers)
        {
            context.Request.Headers[header.Key] = header.Value;
        }

        return new BillingWebhooksController(sender, NullLogger<BillingWebhooksController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = context }
        };
    }
}
