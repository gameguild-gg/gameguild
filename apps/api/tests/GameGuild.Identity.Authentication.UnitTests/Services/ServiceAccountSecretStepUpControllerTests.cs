using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ServiceAccountSecretStepUpControllerTests
{
    [Fact]
    public async Task AuthorizedTargetWithoutReceiptGetsStructuredDemandAndNoRotation()
    {
        var f = new Fixture();
        var demand = new StepUpAuthenticationRequiredResponse { StepUpRequired = true, ChallengeId = Guid.NewGuid() };
        f.Requirements.Setup(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>())).ReturnsAsync(demand);
        var result = Assert.IsType<ObjectResult>(await f.Controller.RotateSecret(f.Id, null, default));
        Assert.Equal(403, result.StatusCode);
        Assert.Same(demand, result.Value);
        f.Requirements.Verify(x => x.CreateAsync(It.Is<StepUpOperationBinding>(b => b.OperationType == "rotate-service-account-secret" &&
            b.TargetReference == "service-account/" + f.Id.ToString("D")), It.IsAny<CancellationToken>()), Times.Once);
        f.NoRotation();
    }

    [Theory]
    [InlineData("missing-account")]
    [InlineData("non-admin")]
    [InlineData("missing-session")]
    public async Task UnavailableTargetOrAuthorityCannotCreateDemand(string invalid)
    {
        var f = new Fixture(invalid);
        var result = await f.Controller.RotateSecret(f.Id, null, default);
        if (invalid == "missing-account")
        {
            Assert.IsType<NotFoundResult>(result);
        }
        if (invalid == "non-admin")
        {
            Assert.IsType<ForbidResult>(result);
        }
        if (invalid == "missing-session")
        {
            Assert.IsType<UnauthorizedResult>(result);
        }
        f.Requirements.Verify(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()), Times.Never);
        f.NoRotation();
    }

    [Fact]
    public async Task ChallengeStorageFailurePropagatesInsteadOfReturningNotFound()
    {
        var f = new Fixture();
        f.Requirements.Setup(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("challenge storage unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Controller.RotateSecret(f.Id, null, default));
        f.NoRotation();
    }

    [Fact]
    public async Task ReceiptStorageFailurePropagatesInsteadOfReturningNotFound()
    {
        var f = new Fixture();
        f.Receipts.Setup(x => x.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), "supplied-receipt", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("receipt storage unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Controller.RotateSecret(f.Id, new("supplied-receipt"), default));
        f.NoRotation();
    }

    [Fact]
    public async Task RejectedReceiptCannotCreateAnotherDemandOrRotateSecret()
    {
        var f = new Fixture();
        f.Receipts.Setup(x => x.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), "rejected-receipt", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StepUpReceiptInvalidException("receipt mismatch"));
        Assert.IsType<BadRequestObjectResult>(await f.Controller.RotateSecret(f.Id, new("rejected-receipt"), default));
        f.Requirements.Verify(x => x.CreateAsync(It.IsAny<StepUpOperationBinding>(), It.IsAny<CancellationToken>()), Times.Never);
        f.NoRotation();
    }

    private sealed class Fixture
    {
        internal Guid Id { get; } = Guid.NewGuid();
        internal Mock<IServiceAccountService> Service { get; } = new();
        internal Mock<IStepUpReceiptService> Receipts { get; } = new();
        internal Mock<IStepUpRequirementService> Requirements { get; } = new();
        internal ServiceAccountOperationsController Controller { get; }

        internal Fixture(string? invalid = null)
        {
            var tenant = Guid.NewGuid();
            var actor = ActorContextBuilder.ForUser(Guid.NewGuid()).WithTenantId(tenant);
            if (invalid != "missing-session")
            {
                actor.WithAttribute("session_id", Guid.NewGuid().ToString());
            }
            var accessor = new ActorContextAccessor();
            accessor.SetActorContext(actor.Build() with { Roles = new HashSet<string> { invalid == "non-admin" ? "Member" : "Admin" } });
            Service.Setup(x => x.GetByIdAsync(Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalid == "missing-account" ? null : new ServiceAccount { Id = Id, TenantId = tenant });
            var handler = new RotateServiceAccountSecretHandler(Service.Object, accessor, Receipts.Object);
            var sender = new Mock<ISender>();
            sender.Setup(x => x.Send(It.IsAny<RotateServiceAccountSecretCommand>(), It.IsAny<CancellationToken>()))
                .Returns((RotateServiceAccountSecretCommand command, CancellationToken ct) => handler.Handle(command, ct));
            Controller = new ServiceAccountOperationsController(Service.Object, sender.Object, Requirements.Object);
        }

        internal void NoRotation() => Service.Verify(x => x.RotateSecretAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
