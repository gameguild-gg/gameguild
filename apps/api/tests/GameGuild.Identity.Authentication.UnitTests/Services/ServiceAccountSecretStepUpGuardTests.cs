using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ServiceAccountSecretStepUpGuardTests
{
    private const string Receipt = "synthetic-service-rotation-receipt";

    [Fact]
    public async Task DirectRotationWithoutFreshReceiptCannotChangeAccountSecret()
    {
        var f = new Fixture();
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId), default);
        Assert.True(result.IsFailure);
        Assert.Equal("StepUp.Required", result.Error.Code);
        f.AssertNoRotation();
    }

    [Fact]
    public async Task ExactReceiptAllowsOneRotationAndReplayIsDenied()
    {
        var f = new Fixture();
        var challenge = f.SeedReceipt();
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default);
        Assert.True(result.IsSuccess);
        Assert.Equal("synthetic-new-secret", result.Value);
        Assert.NotNull(challenge.ConsumedAt);
        var replay = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default);
        Assert.True(replay.IsFailure);
        f.Service.Verify(x => x.RotateSecretAsync(f.AccountId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("Member", false, false, false)]
    [InlineData("Admin", false, false, true)]
    [InlineData("Admin", true, false, false)]
    [InlineData("Admin", false, true, false)]
    public async Task ActorAndAccountAuthorityCannotBeSubstituted(string role, bool otherTenant, bool global, bool anonymous)
    {
        var f = new Fixture(role, otherTenant, global, anonymous);
        var row = f.SeedReceipt();
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default);
        Assert.True(result.IsFailure);
        Assert.Null(row.ConsumedAt);
        f.AssertNoRotation();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SystemAdministratorCanRotateExplicitCrossTenantOrGlobalTarget(bool global)
    {
        var f = new Fixture("SystemAdmin", otherTenant: true, global: global);
        f.SeedReceipt();
        Assert.True((await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default)).IsSuccess);
        f.Service.Verify(x => x.RotateSecretAsync(f.AccountId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("target")]
    [InlineData("actor")]
    [InlineData("tenant")]
    [InlineData("session")]
    [InlineData("operation")]
    [InlineData("expired")]
    [InlineData("unverified")]
    [InlineData("receipt")]
    public async Task ReceiptCannotAuthorizeAnotherTargetContextOrExpiredProof(string change)
    {
        var f = new Fixture();
        var row = f.SeedReceipt();
        var target = f.AccountId;
        var receipt = Receipt;
        switch (change)
        {
            case "target": target = Guid.NewGuid(); break;
            case "actor": row.ActorId = Guid.NewGuid(); break;
            case "tenant": row.TenantId = Guid.NewGuid(); break;
            case "session": row.SessionId = Guid.NewGuid(); break;
            case "operation": row.OperationType = "create-api-key"; break;
            case "expired": row.ExpiresAt = StepUpReceiptServiceTestHarness.Now; break;
            case "unverified": row.VerifiedAt = null; break;
            case "receipt": receipt = "another-receipt"; break;
            default: throw new ArgumentOutOfRangeException(nameof(change), change, "Unknown receipt mismatch test case");
        }
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(target, receipt), default);
        Assert.True(result.IsFailure);
        Assert.Null(row.ConsumedAt);
        f.AssertNoRotation();
    }

    [Theory]
    [InlineData("session")]
    [InlineData("tenant")]
    [InlineData("malformed-session")]
    public async Task MissingBoundContextCannotRotateOrSpendReceipt(string missing)
    {
        var f = new Fixture();
        var row = f.SeedReceipt();
        var actor = ActorContextBuilder.ForUser(f.ActorId);
        if (missing != "tenant")
        {
            actor.WithTenantId(f.TenantId);
        }
        if (missing != "session")
        {
            actor.WithAttribute("session_id", missing == "malformed-session" ? "invalid" : f.SessionId.ToString());
        }
        f.Accessor.SetActorContext(actor.Build() with { Roles = new HashSet<string> { "Admin" } });
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default);
        Assert.Equal("Auth.Required", result.Error.Code);
        Assert.Null(row.ConsumedAt);
        f.AssertNoRotation();
    }

    [Fact]
    public async Task MissingAccountDoesNotConsumeReceipt()
    {
        var f = new Fixture();
        var row = f.SeedReceipt();
        f.Service.Setup(x => x.GetByIdAsync(f.AccountId, It.IsAny<CancellationToken>())).ReturnsAsync((ServiceAccount?)null);
        var result = await f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default);
        Assert.Equal("ServiceAccount.NotFound", result.Error.Code);
        Assert.Null(row.ConsumedAt);
        f.AssertNoRotation();
    }

    [Fact]
    public async Task ReceiptStoreFailureIsNotMappedToAnAccountNotFoundResult()
    {
        var f = new Fixture();
        var receipts = new Mock<IStepUpReceiptService>();
        receipts.Setup(x => x.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), Receipt, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("receipt storage unavailable"));
        var handler = new RotateServiceAccountSecretHandler(f.Service.Object, f.Accessor, receipts.Object);
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default));
        f.AssertNoRotation();
    }

    [Fact]
    public async Task RotationStorageFailurePropagatesToTheTransactionOwner()
    {
        var f = new Fixture();
        f.SeedReceipt();
        f.Service.Setup(x => x.RotateSecretAsync(f.AccountId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("rotation storage unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Handler.Handle(new RotateServiceAccountSecretCommand(f.AccountId, Receipt), default));
    }

    private sealed class Fixture
    {
        internal Guid ActorId { get; } = Guid.NewGuid();
        internal Guid TenantId { get; } = Guid.NewGuid();
        internal Guid SessionId { get; } = Guid.NewGuid();
        internal Guid AccountId { get; } = Guid.NewGuid();
        internal ActorContextAccessor Accessor { get; } = new();
        internal RecordingStepUpChallengeStore Store { get; } = new();
        internal Mock<IServiceAccountService> Service { get; } = new();
        internal RotateServiceAccountSecretHandler Handler { get; }

        internal Fixture(string role = "Admin", bool otherTenant = false, bool global = false, bool anonymous = false)
        {
            var actor = ActorContextBuilder.ForUser(ActorId).WithTenantId(TenantId)
                .WithAttribute("session_id", SessionId.ToString()).Build() with
            { Roles = new HashSet<string> { role } };
            Accessor.SetActorContext(anonymous ? ActorContext.Anonymous : actor);
            Service.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => new ServiceAccount
                { Id = id, TenantId = global ? null : otherTenant ? Guid.NewGuid() : TenantId });
            Service.Setup(x => x.RotateSecretAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("synthetic-new-secret");
            Handler = new RotateServiceAccountSecretHandler(Service.Object, Accessor,
                StepUpReceiptServiceTestHarness.CreateService(Accessor, Store));
        }

        internal StepUpChallenge SeedReceipt()
        {
            var id = AccountId.ToString("D");
            var row = new StepUpChallenge
            {
                Id = Guid.NewGuid(), ActorId = ActorId, TenantId = TenantId, SessionId = SessionId,
                OperationType = "rotate-service-account-secret", TargetReference = "service-account/" + id,
                PayloadHash = StepUpReceiptServiceTestHarness.Sha256(id),
                ReceiptHash = StepUpReceiptServiceTestHarness.Sha256(Receipt),
                CreatedAt = StepUpReceiptServiceTestHarness.Now.AddMinutes(-1),
                VerifiedAt = StepUpReceiptServiceTestHarness.Now, VerificationMethod = MfaMethod.Totp,
                ExpiresAt = StepUpReceiptServiceTestHarness.Now.AddMinutes(4)
            };
            Store.Challenges.Add(row);
            return row;
        }

        internal void AssertNoRotation() => Service.Verify(x => x.RotateSecretAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
