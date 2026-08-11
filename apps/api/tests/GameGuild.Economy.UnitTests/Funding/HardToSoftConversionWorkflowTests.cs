using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Funding;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using GameGuild.Economy.UnitTests.Persistence;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GameGuild.Economy.UnitTests.Funding;

public sealed class HardToSoftConversionWorkflowTests
{
    [Fact]
    public void RequestReceiptAndRejection_ExposeTheirExactValues()
    {
        var decisionId = Guid.NewGuid();
        var postingId = Guid.NewGuid();
        var feePostingId = Guid.NewGuid();
        var request = new SelfServiceHardToSoftConversionRequest(100, 3, decisionId, "conversion-key");
        var receipt = new SelfServiceHardToSoftConversionReceipt(postingId, feePostingId, 17, "journal-hash", true);
        var rejection = new EconomySelfServiceCommandRejectedException("rejected");

        request.PrincipalHardCoinUnits.Should().Be(100);
        request.FeeHardCoinUnits.Should().Be(3);
        request.RiskDecisionId.Should().Be(decisionId);
        request.IdempotencyKey.Should().Be("conversion-key");
        receipt.PrincipalPostingId.Should().Be(postingId);
        receipt.FeePostingId.Should().Be(feePostingId);
        receipt.JournalSequence.Should().Be(17);
        receipt.JournalHash.Should().Be("journal-hash");
        receipt.IsDuplicate.Should().BeTrue();
        rejection.Message.Should().Be("rejected");
    }

    [Fact]
    public void ParseRootIds_CanonicalizesValidIdsAndRejectsEveryMalformedShape()
    {
        var first = Guid.Parse("74000000-0000-0000-0000-000000000001");
        var second = Guid.Parse("74000000-0000-0000-0000-000000000002");

        PostgreSqlHardToSoftConversionWorkflow.ParseRootIds($"[\"{second}\",\"{first}\"]")
            .Should().Equal(first, second);
        PostgreSqlHardToSoftConversionWorkflow.DeterministicGuid("principal", "conversion-key")
            .Should().Be(PostgreSqlHardToSoftConversionWorkflow.DeterministicGuid("principal", "conversion-key"));

        foreach (var sourceRoots in new[]
                 {
                     "",
                     "not-json",
                     "{}",
                     "[]",
                     "[\"not-a-guid\"]",
                     $"[\"{Guid.Empty}\"]",
                     $"[\"{first}\",\"{first}\"]"
                 })
        {
            Action act = () => PostgreSqlHardToSoftConversionWorkflow.ParseRootIds(sourceRoots);
            act.Should().Throw<EconomySelfServiceCommandRejectedException>();
        }
    }

    [Fact]
    public async Task ConvertAsync_RejectsInvalidRequestsBeforeAnyPersistenceWork()
    {
        await using var context = CreateContext();
        var accessor = new ActorContextAccessor();
        var enabled = new EnabledGate();
        var workflow = new PostgreSqlHardToSoftConversionWorkflow(context, accessor, enabled);
        var valid = Request();

        Func<Task> nullRequest = () => workflow.ConvertAsync(null!, CancellationToken.None);
        Func<Task> cancelled = () => workflow.ConvertAsync(valid, new CancellationToken(canceled: true));
        Func<Task> invalidPrincipal = () => workflow.ConvertAsync(valid with { PrincipalHardCoinUnits = 0 }, CancellationToken.None);
        Func<Task> invalidFee = () => workflow.ConvertAsync(valid with { FeeHardCoinUnits = -1 }, CancellationToken.None);
        Func<Task> missingDecision = () => workflow.ConvertAsync(valid with { RiskDecisionId = Guid.Empty }, CancellationToken.None);

        await nullRequest.Should().ThrowAsync<ArgumentNullException>();
        await cancelled.Should().ThrowAsync<OperationCanceledException>();
        await invalidPrincipal.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await invalidFee.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await missingDecision.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>();
        enabled.EnsuredCapabilities.Should().OnlyContain(capability => capability == EconomyValueMovementCapability.ConvertHardToSoft);
    }

    [Fact]
    public async Task ConvertAsync_RequiresTheCapabilityAndAnAuthenticatedTenantActor()
    {
        await using var context = CreateContext();
        var accessor = new ActorContextAccessor();
        var blocked = new EnabledGate(new EconomyValueMovementDisabledException("disabled"));
        var workflow = new PostgreSqlHardToSoftConversionWorkflow(context, accessor, blocked);

        Func<Task> disabled = () => workflow.ConvertAsync(Request(), CancellationToken.None);
        await disabled.Should().ThrowAsync<EconomyValueMovementDisabledException>();

        var enabled = new EnabledGate();
        workflow = new PostgreSqlHardToSoftConversionWorkflow(context, accessor, enabled);
        try
        {
            Func<Task> anonymous = () => workflow.ConvertAsync(Request(), CancellationToken.None);
            await anonymous.Should().ThrowAsync<UnauthorizedAccessException>();
        }
        finally
        {
            accessor.ClearActorContext();
        }
    }

    [Fact]
    public async Task ConvertAsync_RequiresAnActiveWalletAndARequestBoundRiskDecision()
    {
        await using var context = CreateContext();
        var accessor = SetActorContext(out var actorId, out var tenantId);
        var workflow = new PostgreSqlHardToSoftConversionWorkflow(context, accessor, new EnabledGate());
        var request = Request();

        try
        {
            Func<Task> missingWallet = () => workflow.ConvertAsync(request, CancellationToken.None);
            await missingWallet.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
                .WithMessage("*no active Economy wallet*");

            context.Set<EconomyWalletRow>().Add(new EconomyWalletRow
            {
                Id = Guid.NewGuid(),
                OwnerId = actorId,
                TenantId = tenantId,
                State = WalletLifecycleState.Active,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();

            Func<Task> missingDecision = () => workflow.ConvertAsync(request, CancellationToken.None);
            await missingDecision.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
                .WithMessage("*not bound to this idempotent conversion request*");

            context.Set<EconomyRiskDecisionRow>().Add(new EconomyRiskDecisionRow
            {
                Id = request.RiskDecisionId,
                IdempotencyKey = "other-key",
                SourceRoots = "[]"
            });
            await context.SaveChangesAsync();

            Func<Task> mismatchedDecision = () => workflow.ConvertAsync(request, CancellationToken.None);
            await mismatchedDecision.Should().ThrowAsync<EconomySelfServiceCommandRejectedException>()
                .WithMessage("*not bound to this idempotent conversion request*");
        }
        finally
        {
            accessor.ClearActorContext();
        }
    }

    [DockerFact]
    public async Task ConvertAsync_ComposesTheAuthorizedWriterCallForZeroFeeAndFeeConversions()
    {
        await using var database = await PostgreSqlHardToSoftConversionGatewayTests.CreateDatabaseAsync();
        await using var context = PostgreSqlHardToSoftConversionGatewayTests.CreateContext(database.GetConnectionString());
        await context.Database.MigrateAsync();
        await using var connection = new Npgsql.NpgsqlConnection(database.GetConnectionString());
        await connection.OpenAsync();

        var zeroFee = await ConvertWithPersistedWriterAsync(context, connection, 0);
        var withFee = await ConvertWithPersistedWriterAsync(context, connection, 1);

        zeroFee.FeePostingId.Should().BeNull();
        zeroFee.IsDuplicate.Should().BeFalse();
        zeroFee.JournalSequence.Should().BePositive();
        zeroFee.JournalHash.Should().NotBeNullOrWhiteSpace();
        withFee.FeePostingId.Should().NotBeNull();
        withFee.IsDuplicate.Should().BeFalse();
        withFee.JournalSequence.Should().BeGreaterThan(zeroFee.JournalSequence);
    }

    private static async Task<SelfServiceHardToSoftConversionReceipt> ConvertWithPersistedWriterAsync(
        ApplicationDbContext context,
        Npgsql.NpgsqlConnection connection,
        long feeHardCoinUnits)
    {
        var walletId = Guid.NewGuid();
        var rootId = Guid.NewGuid();
        var hardLotId = Guid.NewGuid();
        var capabilityId = Guid.NewGuid();
        var decisionId = Guid.NewGuid();
        var counterId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var key = $"workflow-conversion-{Guid.NewGuid():N}";
        var principalHardCoinUnits = 10L;
        var totalHardCoinUnits = principalHardCoinUnits + feeHardCoinUnits;
        var timestamp = DateTimeOffset.UtcNow;

        await PostgreSqlHardToSoftConversionGatewayTests.SeedAsync(
            connection,
            walletId,
            rootId,
            hardLotId,
            capabilityId,
            decisionId,
            counterId,
            actorId,
            tenantId,
            key,
            totalHardCoinUnits,
            key,
            timestamp);
        await PostgreSqlHardToSoftConversionGatewayTests.ReserveRiskCounterAsync(
            connection,
            decisionId,
            counterId,
            totalHardCoinUnits,
            timestamp);

        var accessor = SetActorContext(actorId, tenantId);
        try
        {
            var workflow = new PostgreSqlHardToSoftConversionWorkflow(context, accessor, new EnabledGate());
            return await workflow.ConvertAsync(
                new SelfServiceHardToSoftConversionRequest(
                    principalHardCoinUnits,
                    feeHardCoinUnits,
                    decisionId,
                    key),
                CancellationToken.None);
        }
        finally
        {
            accessor.ClearActorContext();
        }
    }

    private static ApplicationDbContext CreateContext() => new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static SelfServiceHardToSoftConversionRequest Request() => new(
        100,
        0,
        Guid.NewGuid(),
        $"conversion-{Guid.NewGuid():N}");

    private static ActorContextAccessor SetActorContext(out Guid actorId, out Guid tenantId)
    {
        actorId = Guid.NewGuid();
        tenantId = Guid.NewGuid();
        return SetActorContext(actorId, tenantId);
    }

    private static ActorContextAccessor SetActorContext(Guid actorId, Guid tenantId)
    {
        var accessor = new ActorContextAccessor();
        accessor.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>(),
            IsAuthenticated = true
        });
        return accessor;
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }

    private sealed class EnabledGate(Exception? exception = null) : IEconomyValueMovementDecisionGate
    {
        public List<EconomyValueMovementCapability> EnsuredCapabilities { get; } = [];

        public bool IsEnabled => exception is null;

        public bool IsCapabilityEnabled(EconomyValueMovementCapability capability) => exception is null;

        public void EnsureEnabled()
        {
            if (exception is not null)
                throw exception;
        }

        public void EnsureEnabled(EconomyValueMovementCapability capability)
        {
            EnsuredCapabilities.Add(capability);
            EnsureEnabled();
        }
    }
}
