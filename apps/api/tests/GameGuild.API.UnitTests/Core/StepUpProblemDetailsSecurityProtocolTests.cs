using GameGuild.API;
using GameGuild.Configuration.ApplicationLayer;
using GameGuild.API.Core.Filters;
using GameGuild.Identity.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using HostProblemDetailsOptions = GameGuild.Configuration.PresentationLayer.ProblemDetails.ProblemDetailsOptions;

namespace GameGuild.API.UnitTests.Core;

public sealed class StepUpProblemDetailsSecurityProtocolTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Actual423ResponderSurvivesTheHostProblemDetailsFilter(bool configurationLookupFails)
    {
        var configurations = new Mock<IUserMfaConfigurationRepository>();
        var credentials = new Mock<IWebAuthnCredentialRepository>();
        var lookup = configurations.Setup(repository => repository.GetByUserIdAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()));
        if (configurationLookupFails)
        {
            lookup.ThrowsAsync(new InvalidOperationException("Synthetic MFA configuration failure"));
        }
        else
        {
            lookup.ReturnsAsync(new UserMfaConfiguration { IsEnabled = true, IsSetupComplete = true, TotpSecretKey = "synthetic-enrollment" });
        }
        var responder = new StepUpRequiredResponder(configurations.Object, credentials.Object,
            new MfaOptions { Enabled = true }, NullLogger<StepUpRequiredResponder>.Instance);
        var payload = await responder.BuildAsync(Guid.NewGuid(), "trust-device", "current-session",
            "Fresh verification is required.");
        Assert.Equal(StatusCodes.Status423Locked, payload.Status);
        Assert.True(payload.StepUpRequired);
        string[] expectedMethods = configurationLookupFails ? [] : ["Totp"];
        Assert.Equal(expectedMethods, payload.AvailableMethods);

        await AssertHostStatusAsync(payload, StatusCodes.Status423Locked);
    }

    [Fact]
    public async Task ExistingOperationBound403PayloadRemainsForbiddenThroughTheSameFilter()
    {
        // Other callers deliberately use this DTO for a persisted operation-bound 403 demand.
        var payload = new StepUpAuthenticationRequiredResponse();
        Assert.Equal(StatusCodes.Status403Forbidden, payload.Status);
        await AssertHostStatusAsync(payload, StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void TrustEndpointDeclaresTheActualStructured423Response()
    {
        var action = typeof(TrustedDevicesController).GetMethod(nameof(TrustedDevicesController.TrustCurrentDevice));
        Assert.NotNull(action);
        var metadata = action.GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: true)
            .Cast<ProducesResponseTypeAttribute>();
        Assert.Contains(metadata, value => value.StatusCode == StatusCodes.Status423Locked &&
            value.Type == typeof(StepUpAuthenticationRequiredResponse));
    }


    [Theory]
    [InlineData("disabled-option", "")]
    [InlineData("disabled-enrollment", "")]
    [InlineData("incomplete-enrollment", "")]
    [InlineData("sms-email-only", "")]
    [InlineData("empty-backup-set", "")]
    [InlineData("spent-versioned-backup-set", "")]
    [InlineData("remaining-versioned-backup-code", "BackupCode")]
    [InlineData("remaining-legacy-backup-code", "BackupCode")]
    [InlineData("enrolled-totp", "Totp")]
    [InlineData("active-webauthn", "WebAuthn")]
    public async Task AdvertisedFactorsFollowTheStrictStepUpVerifier(string boundary, string expectedMethod)
    {
        var configuration = new UserMfaConfiguration
        {
            IsEnabled = boundary != "disabled-enrollment",
            IsSetupComplete = boundary != "incomplete-enrollment",
            TotpSecretKey = boundary is "disabled-option" or "disabled-enrollment" or
                "incomplete-enrollment" or "enrolled-totp" ? "synthetic-enrollment" : null,
            SmsPhoneNumber = "+15555550123",
            EmailMfaAddress = "synthetic@example.test",
            // Persisted backup sets are versioned or comma-separated, never JSON arrays.
            BackupCodes = boundary switch
            {
                "spent-versioned-backup-set" => "set-v1$2$",
                "remaining-versioned-backup-code" => "set-v1$2$synthetic-remaining-hash",
                "remaining-legacy-backup-code" => "synthetic-legacy-hash",
                _ => string.Empty
            }
        };
        var configurations = new Mock<IUserMfaConfigurationRepository>();
        configurations.Setup(repository => repository.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);
        var credentials = new Mock<IWebAuthnCredentialRepository>();
        credentials.Setup(repository => repository.HasActiveCredentialsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(boundary == "active-webauthn");
        var responder = new StepUpRequiredResponder(configurations.Object, credentials.Object,
            new MfaOptions { Enabled = boundary != "disabled-option" }, NullLogger<StepUpRequiredResponder>.Instance);
        var payload = await responder.BuildAsync(Guid.NewGuid(), "trust-device", "current-session", "Fresh proof required.");
        string[] expected = expectedMethod.Length == 0 ? [] : [expectedMethod];
        Assert.Equal(expected, payload.AvailableMethods);
        Assert.Equal(StatusCodes.Status423Locked, payload.Status);
    }

    private static async Task AssertHostStatusAsync(StepUpAuthenticationRequiredResponse payload, int expected)
    {
        var services = new ServiceCollection();
        services.SetupProblemDetails(new ConfigurationBuilder().Build(), HostProblemDetailsOptions.CreateDefault());
        using var provider = services.BuildServiceProvider();
        var http = new DefaultHttpContext { RequestServices = provider, TraceIdentifier = "step-up-protocol-probe" };
        var result = new ObjectResult(payload) { StatusCode = expected };
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        var executing = new ResultExecutingContext(action, [], result, controller: new object());
        var executed = new ResultExecutedContext(action, [], result, controller: new object());
        await provider.GetRequiredService<ProblemDetailsResultFilter>()
            .OnResultExecutionAsync(executing, () => Task.FromResult(executed));

        Assert.Same(payload, result.Value);
        Assert.Equal(expected, result.StatusCode);
        Assert.Equal(expected, http.Response.StatusCode);
        Assert.Equal(expected, payload.Status);
    }
}
