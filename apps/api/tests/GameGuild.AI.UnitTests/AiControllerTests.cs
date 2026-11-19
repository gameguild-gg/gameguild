using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using GameGuild;
using GameGuild.Resources;
using Moq;
using Xunit;

namespace GameGuild.AI.UnitTests;

public sealed class AiControllerTests
{
    [Fact]
    public void TypedGenerationEndpoints_ShouldExposeContractAliases()
    {
        typeof(AiController).GetMethod(nameof(AiController.GenerateEmail))!
            .GetCustomAttributes(typeof(HttpPostAttribute), false)
            .Cast<HttpPostAttribute>()
            .Select(attribute => attribute.Template)
            .Should()
            .Contain(["generate-content/email", "email"]);

        typeof(AiController).GetMethod(nameof(AiController.GenerateReport))!
            .GetCustomAttributes(typeof(HttpPostAttribute), false)
            .Cast<HttpPostAttribute>()
            .Select(attribute => attribute.Template)
            .Should()
            .Contain(["generate-content/report", "report"]);
    }

    [Fact]
    public async Task GenerateEmail_ShouldRouteToEmailPromptWithDefaultTokenLimit()
    {
        var orchestrator = new Mock<IAiOrchestrator>();
        AiGenerateRequest? capturedRequest = null;
        var expected = new AiCompletionResponse(
            "primary",
            "OpenAi",
            "gpt-4.1-mini",
            "Email body",
            "stop",
            new AiUsageDto(10, 8, 18));

        orchestrator
            .Setup(service => service.GenerateAsync(It.IsAny<AiGenerateRequest>(), It.IsAny<CancellationToken>()))
            .Callback<AiGenerateRequest, CancellationToken>((request, _) => capturedRequest = request)
            .ReturnsAsync(Result.Success(expected));

        var controller = CreateController(orchestrator.Object);

        var result = await controller.GenerateEmail(new AiGeneratedContentDraftRequest(
            "Tour follow-up",
            "Lead toured unit 12B and asked about lease terms.",
            "prospective renter",
            "warm",
            null,
            null,
            null), CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().Be(expected);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.SystemPrompt.Should().Contain("business emails");
        capturedRequest.Prompt.Should().Contain("Create an email.");
        capturedRequest.Prompt.Should().Contain("Tour follow-up");
        capturedRequest.MaxTokens.Should().Be(700);
        capturedRequest.Temperature.Should().Be(0.4);
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnBadRequest_WhenContextIsMissing()
    {
        var controller = CreateController();

        var result = await controller.GenerateReport(new AiGeneratedContentDraftRequest(
            "Monthly report",
            " ",
            null,
            null,
            null,
            null,
            null), CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Status_ShouldReportBackendCredentialPresenceWithoutReturningSecrets()
    {
        var controller = CreateController(aiOptions: new AiOptions
        {
            Enabled = true,
            DefaultBackend = "primary",
            AllowTenantOverrides = true,
            Backends = new Dictionary<string, AiBackendOptions>
            {
                ["primary"] = new()
                {
                    Adapter = AiAdapterKeys.OpenAi,
                    ApiKey = "sk-secret",
                    DefaultModel = "gpt-4.1-mini",
                },
            }
        });

        var result = controller.Status();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var payload = ok.Value.Should().BeOfType<AiStatusResponse>().Subject;
        var primary = payload.Backends.Single(backend => backend.Backend == "primary");
        primary.Configured.Should().BeTrue();
        primary.CredentialsConfigured.Should().BeTrue();
        ok.Value!.ToString().Should().NotContain("sk-secret");
    }

    private static AiController CreateController(
        IAiOrchestrator? orchestrator = null,
        AiOptions? aiOptions = null)
    {
        var requestContextAccessor = new Mock<IRequestContextAccessor>();
        requestContextAccessor.SetupGet(accessor => accessor.CurrentTenantId).Returns(Guid.NewGuid());
        requestContextAccessor.SetupGet(accessor => accessor.CurrentUserId).Returns(Guid.NewGuid());

        var options = aiOptions ?? new AiOptions { Enabled = true };
        var adapter = new Mock<IAiAdapter>();
        adapter.SetupGet(value => value.Key).Returns(AiAdapterKeys.OpenAi);
        adapter.SetupGet(value => value.Descriptor).Returns(new AiAdapterDescriptor(
            AiAdapterKeys.OpenAi,
            RequiresExplicitBaseUrl: false,
            AiCredentialPolicy.Required,
            "https://api.openai.com",
            SupportsStreaming: true));
        var catalog = new AiBackendCatalog(
            Options.Create(options),
            new AiAdapterRegistry([adapter.Object]));

        return new AiController(
            new CommandHandlerSender(orchestrator ?? Mock.Of<IAiOrchestrator>(), Mock.Of<IAiPromptTemplateService>()),
            Mock.Of<IAiConversationHistoryReader>(),
            requestContextAccessor.Object,
            Mock.Of<IResourceQuotaReader>(),
            catalog,
            Options.Create(options));
    }
}
