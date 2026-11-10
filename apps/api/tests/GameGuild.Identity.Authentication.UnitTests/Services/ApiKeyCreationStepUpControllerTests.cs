using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ApiKeyCreationStepUpControllerTests
{
    [Fact]
    public async Task ValidUnverifiedIntentReturnsStructuredDemandWithoutCreatingKey()
    {
        var (controller, requirements, repository) = Controller();
        var demand = new StepUpAuthenticationRequiredResponse { StepUpRequired = true, ChallengeId = Guid.NewGuid() };
        requirements.Setup(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(demand);
        var result = Assert.IsType<ObjectResult>(await controller.CreateApiKey(Intent(), default));
        Assert.Equal(403, result.StatusCode);
        Assert.Same(demand, result.Value);
        requirements.Verify(x => x.CreateAsync(It.Is<StepUpOperationBinding>(b => b.OperationType == "create-api-key" && b.TargetReference == "current-user"),
            It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("scopes")]
    [InlineData("null-scopes")]
    [InlineData("expiry")]
    public async Task InvalidIntentCannotCreateAChallenge(string invalid)
    {
        var (controller, requirements, repository) = Controller();
        var intent = invalid switch
        {
            "name" => Intent() with { Name = "" },
            "scopes" => Intent() with { Scopes = [] },
            "null-scopes" => Intent() with { Scopes = null! },
            _ => Intent() with { ExpiresAt = DateTime.UtcNow.AddDays(-1) }
        };
        Assert.IsType<BadRequestObjectResult>(await controller.CreateApiKey(intent, default));
        requirements.Verify(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingSessionCannotCreateChallengeOrKey()
    {
        var (controller, requirements, repository) = Controller(session: false);
        Assert.IsType<UnauthorizedObjectResult>(await controller.CreateApiKey(Intent(), default));
        requirements.Verify(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequirementStorageFailurePropagatesWithoutIssuingKey()
    {
        var (controller, requirements, repository) = Controller();
        requirements.Setup(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("challenge persistence unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => controller.CreateApiKey(Intent(), default));
        repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static CreateApiKeyCommand Intent() => new() { Name = "step-up-key", Scopes = ["reports:read"] };

    private static (ApiKeyController Controller, Mock<IStepUpRequirementService> Requirements, Mock<IApiKeyRepository> Repository) Controller(bool session = true)
    {
        var actor = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(Guid.NewGuid());
        if (session) actor.WithAttribute("session_id", Guid.NewGuid().ToString());
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(actor.Build());
        var repository = new Mock<IApiKeyRepository>();
        var handler = new CreateApiKeyHandler(repository.Object, accessor, Mock.Of<IStepUpReceiptService>(),
            NullLogger<CreateApiKeyHandler>.Instance);
        var sender = new Mock<ISender>();
        sender.Setup(x => x.Send(It.IsAny<CreateApiKeyCommand>(), It.IsAny<CancellationToken>()))
            .Returns((CreateApiKeyCommand command, CancellationToken ct) => handler.Handle(command, ct));
        var requirements = new Mock<IStepUpRequirementService>();
        return (new ApiKeyController(sender.Object, requirements.Object), requirements, repository);
    }
}
