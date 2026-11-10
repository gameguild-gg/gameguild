using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Services;

public sealed class ApiKeyCreationStepUpGuardTests
{
    private const string Receipt = "receipt-for-api-key-1191";
    private const string Payload = "{\"Name\":\"issue-1191-key\",\"Scopes\":[\"reports:read\"],\"ExpiresAt\":null,\"IpWhitelist\":null}";

    [Fact]
    public async Task AuthenticatedCallerCannotMintApiKeyWithoutFreshVerificationReceipt()
    {
        var fixture = new Fixture();
        var result = await fixture.Handler.Handle(Intent() with { StepUpReceipt = null }, default);
        Assert.False(result.IsSuccess);
        fixture.AssertNoKey();
    }

    [Fact]
    public async Task ReceiptForExactIntentMintsOneKeyAndCannotBeReused()
    {
        var fixture = new Fixture();
        fixture.SeedReceipt();
        var result = await fixture.Handler.Handle(Intent(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.ActorId, fixture.Created!.UserId);
        Assert.Equal(fixture.TenantId, fixture.Created.TenantId);
        Assert.Equal("issue-1191-key", result.Value.Name);
        Assert.Equal(new[] { "reports:read" }, result.Value.Scopes);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.ApiKey));
        Assert.NotNull(fixture.Store.Challenges.Single().ConsumedAt);
        var replay = await fixture.Handler.Handle(Intent(), default);
        Assert.False(replay.IsSuccess);
        fixture.Repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("receipt")]
    [InlineData("actor")]
    [InlineData("tenant")]
    [InlineData("session")]
    [InlineData("operation")]
    [InlineData("target")]
    [InlineData("expired")]
    [InlineData("unverified")]
    [InlineData("name")]
    [InlineData("scopes")]
    [InlineData("expiry")]
    [InlineData("ip")]
    public async Task ReceiptCannotAuthorizeAnotherContextOrIntent(string change)
    {
        var f = new Fixture();
        var row = f.SeedReceipt();
        var intent = Intent();
        switch (change)
        {
            case "receipt": intent = intent with { StepUpReceipt = "another-receipt" }; break;
            case "actor": row.ActorId = Guid.NewGuid(); break;
            case "tenant": row.TenantId = Guid.NewGuid(); break;
            case "session": row.SessionId = Guid.NewGuid(); break;
            case "operation": row.OperationType = "trust-device"; break;
            case "target": row.TargetReference = "another-user"; break;
            case "expired": row.ExpiresAt = StepUpReceiptServiceTestHarness.Now; break;
            case "unverified": row.VerifiedAt = null; break;
            case "name": intent = intent with { Name = "changed-key" }; break;
            case "scopes": intent = intent with { Scopes = ["reports:write"] }; break;
            case "expiry": intent = intent with { ExpiresAt = DateTime.UtcNow.AddDays(1) }; break;
            case "ip": intent = intent with { IpWhitelist = "192.0.2.1" }; break;
        }
        var result = await f.Handler.Handle(intent, default);
        Assert.False(result.IsSuccess);
        f.AssertNoKey();
        Assert.Null(row.ConsumedAt);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("empty-scopes")]
    [InlineData("null-scopes")]
    [InlineData("expired")]
    public async Task InvalidIntentDoesNotConsumeReceipt(string invalid)
    {
        var f = new Fixture();
        var row = f.SeedReceipt();
        var intent = invalid switch
        {
            "name" => Intent() with { Name = "" },
            "empty-scopes" => Intent() with { Scopes = [] },
            "null-scopes" => Intent() with { Scopes = null! },
            _ => Intent() with { ExpiresAt = DateTime.UtcNow.AddDays(-1) }
        };
        var result = await f.Handler.Handle(intent, default);
        Assert.False(result.IsSuccess);
        f.AssertNoKey();
        Assert.Null(row.ConsumedAt);
    }

    [Fact]
    public async Task ScopeArrayCannotChangeBetweenVerificationAndPersistence()
    {
        var f = new Fixture();
        var intent = Intent();
        var receipts = new Mock<IStepUpReceiptService>();
        receipts.Setup(x => x.ConsumeAsync(It.IsAny<StepUpOperationBinding>(), Receipt, It.IsAny<CancellationToken>()))
            .Callback(() => intent.Scopes[0] = "reports:write")
            .Returns(Task.CompletedTask);
        var handler = new CreateApiKeyHandler(f.Repository.Object, f.Accessor, receipts.Object,
            NullLogger<CreateApiKeyHandler>.Instance, f.Audit.Object);
        var result = await handler.Handle(intent, default);
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "reports:read" }, result.Value.Scopes);
        Assert.Equal(new[] { "reports:read" }, f.Created!.GetScopes());
        receipts.Verify(x => x.ConsumeAsync(
            It.Is<StepUpOperationBinding>(b => b.OperationType == "create-api-key" &&
                b.TargetReference == "current-user" && b.PayloadHash == StepUpReceiptServiceTestHarness.Sha256(Payload)),
            Receipt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistenceFailureIsPropagatedWithoutSuccessAudit()
    {
        var f = new Fixture();
        f.SeedReceipt();
        f.Repository.Setup(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Handler.Handle(Intent(), default));
        f.Audit.Verify(x => x.RecordAsync(It.Is<ApiKeyAuditEvent>(e => e.Success), It.IsAny<CancellationToken>()), Times.Never);
        // Transactional receipt rollback is exercised by the relational command-pipeline tests.
    }

    private static CreateApiKeyCommand Intent() => new()
    {
        Name = "issue-1191-key", Scopes = ["reports:read"], StepUpReceipt = Receipt
    };

    private sealed class Fixture
    {
        internal Guid ActorId { get; } = Guid.NewGuid();
        internal Guid TenantId { get; } = Guid.NewGuid();
        private Guid SessionId { get; } = Guid.NewGuid();
        internal ActorContextAccessor Accessor { get; } = new();
        internal RecordingStepUpChallengeStore Store { get; } = new();
        internal Mock<IApiKeyRepository> Repository { get; } = new();
        internal Mock<IApiKeyAuditEventSink> Audit { get; } = new();
        internal CreateApiKeyHandler Handler { get; }
        internal ApiKey? Created { get; private set; }

        internal Fixture()
        {
            Accessor.SetActorContext(ActorContextBuilder.ForUser(ActorId).WithTenantId(TenantId)
                .WithAttribute("session_id", SessionId.ToString()).Build());
            Repository.Setup(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()))
                .Callback<ApiKey, CancellationToken>((key, _) => Created = key)
                .ReturnsAsync((ApiKey key, CancellationToken _) => key);
            Handler = new CreateApiKeyHandler(Repository.Object, Accessor,
                StepUpReceiptServiceTestHarness.CreateService(Accessor, Store),
                NullLogger<CreateApiKeyHandler>.Instance, Audit.Object);
        }

        internal StepUpChallenge SeedReceipt()
        {
            var row = new StepUpChallenge
            {
                Id = Guid.NewGuid(), TenantId = TenantId, ActorId = ActorId, SessionId = SessionId,
                OperationType = "create-api-key", TargetReference = "current-user",
                PayloadHash = StepUpReceiptServiceTestHarness.Sha256(Payload),
                ReceiptHash = StepUpReceiptServiceTestHarness.Sha256(Receipt),
                CreatedAt = StepUpReceiptServiceTestHarness.Now.AddMinutes(-1),
                VerifiedAt = StepUpReceiptServiceTestHarness.Now, VerificationMethod = MfaMethod.Totp,
                ExpiresAt = StepUpReceiptServiceTestHarness.Now.AddMinutes(4)
            };
            Store.Challenges.Add(row);
            return row;
        }

        internal void AssertNoKey()
        {
            Repository.Verify(x => x.AddAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
            Audit.Verify(x => x.RecordAsync(It.Is<ApiKeyAuditEvent>(e => e.ActionType == ApiKeyAuditActions.Created && e.Success),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
