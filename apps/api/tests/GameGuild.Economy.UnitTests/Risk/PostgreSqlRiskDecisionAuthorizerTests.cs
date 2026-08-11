using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.PostgreSql;

namespace GameGuild.Economy.UnitTests.Risk;

public sealed class PostgreSqlRiskDecisionAuthorizerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 10, 20, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task AllowsOnlyTheExactDurableAllowDecision()
    {
        await using var database = await CreateDatabaseAsync();
        var context = CreateContext(database.GetConnectionString());
        await using (context)
        {
            await context.Database.MigrateAsync();
            var operation = CreateOperation();
            var decision = CreateDecision(operation);
            await PersistAsync(context, operation, decision);

            var authorization = new PostgreSqlRiskDecisionAuthorizer(context)
                .AuthorizeValueMovement(decision, operation, Now);

            authorization.DecisionId.Should().Be(decision.Id);
            authorization.OperationFingerprint.Should().Be(operation.Fingerprint());
            authorization.IdempotencyKey.Should().Be(operation.IdempotencyKey);
        }
    }

    [DockerFact]
    public async Task RejectsMissingOrDifferentDurableDecision()
    {
        await using var database = await CreateDatabaseAsync();
        var context = CreateContext(database.GetConnectionString());
        await using (context)
        {
            await context.Database.MigrateAsync();
            var operation = CreateOperation();
            var decision = CreateDecision(operation);
            await PersistAsync(context, operation, decision);
            var authorizer = new PostgreSqlRiskDecisionAuthorizer(context);

            FluentActions.Invoking(() => authorizer.AuthorizeValueMovement(
                    decision with { Id = Guid.NewGuid() }, operation, Now))
                .Should().Throw<RiskDecisionBindingException>();
            FluentActions.Invoking(() => authorizer.AuthorizeValueMovement(
                    decision with { IssuedAt = decision.IssuedAt.AddSeconds(1) }, operation, Now))
                .Should().Throw<RiskDecisionBindingException>();
            FluentActions.Invoking(() => authorizer.AuthorizeValueMovement(
                    decision with { ExpiresAt = decision.ExpiresAt.AddSeconds(1) }, operation, Now))
                .Should().Throw<RiskDecisionBindingException>();
            FluentActions.Invoking(() => authorizer.AuthorizeValueMovement(
                    decision with { Outcome = RiskOutcome.Deny }, operation, Now))
                .Should().Throw<RiskDecisionBindingException>();
        }
    }

    [DockerFact]
    public async Task RejectsAStoredDenyDecisionAfterVerifyingItsBinding()
    {
        await using var database = await CreateDatabaseAsync();
        var context = CreateContext(database.GetConnectionString());
        await using (context)
        {
            await context.Database.MigrateAsync();
            var operation = CreateOperation();
            var decision = CreateDecision(operation) with { Outcome = RiskOutcome.Deny };
            await PersistAsync(context, operation, decision);

            FluentActions.Invoking(() => new PostgreSqlRiskDecisionAuthorizer(context)
                    .AuthorizeValueMovement(decision, operation, Now))
                .Should().Throw<RiskAuthorizationDeniedException>();
        }
    }

    private static async Task<PostgreSqlContainer> CreateDatabaseAsync()
    {
        var database = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("economy_risk_authorizer")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
        await database.StartAsync();
        return database;
    }

    private static ApplicationDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);

    private static ProtectedOperationContext CreateOperation()
    {
        return new ProtectedOperationContext(
            new IdempotencyKey("persisted-risk-decision"),
            Guid.NewGuid(),
            PostingTemplateKind.Spend,
            WalletId.New(),
            WalletId.New(),
            new CoinAmount(CurrencyCode.HardCoin, 100),
            [new RiskCurrencyLeg(CurrencyCode.HardCoin, 100)],
            [SourceStampId.New()],
            "provider-reference-hash",
            new PolicyVersion(1),
            new ReserveVersion(1),
            1,
            1,
            1,
            "entity-graph-hash");
    }

    private static RiskDecisionSnapshot CreateDecision(ProtectedOperationContext operation) =>
        RiskDecisionSnapshot.Create(
            Guid.NewGuid(),
            RiskOutcome.Allow,
            operation,
            Now.AddMinutes(-1),
            Now.AddMinutes(5),
            [RiskReasonCode.WithinLimits]);

    private static async Task PersistAsync(
        ApplicationDbContext context,
        ProtectedOperationContext operation,
        RiskDecisionSnapshot decision)
    {
        context.Set<EconomyWalletRow>().AddRange(
            new EconomyWalletRow
            {
                Id = operation.SourceWalletId.Value,
                OwnerId = operation.ActorId,
                TenantId = Guid.NewGuid(),
                State = WalletLifecycleState.Active,
                CreatedAt = Now.AddMinutes(-2)
            },
            new EconomyWalletRow
            {
                Id = operation.DestinationWalletId.Value,
                OwnerId = operation.ActorId,
                TenantId = Guid.NewGuid(),
                State = WalletLifecycleState.Active,
                CreatedAt = Now.AddMinutes(-2)
            });
        context.Set<EconomyRiskDecisionRow>().Add(new EconomyRiskDecisionRow
        {
            Id = decision.Id,
            Outcome = decision.Outcome,
            OperationFingerprint = decision.OperationFingerprint,
            ActorHash = "actor-hash",
            TemplateKind = operation.Operation,
            SourceWalletId = operation.SourceWalletId.Value,
            DestinationWalletId = operation.DestinationWalletId.Value,
            Currency = operation.Amount.Currency,
            AmountUnits = operation.Amount.Units,
            CurrencyLegs = "[]",
            SourceRoots = "[]",
            ProviderReferenceHash = operation.ProviderReferenceHash,
            PolicyVersion = operation.PolicyVersion.Value,
            ReserveVersion = operation.ReserveVersion.Value,
            ReserveAuthorizationEpoch = operation.ReserveAuthorizationEpoch,
            FeatureVersion = operation.FeatureVersion,
            KillSwitchEpoch = operation.KillSwitchEpoch,
            CounterVersion = operation.CounterVersion,
            EntityGraphVersion = operation.EntityGraphVersion,
            EntityGraphEvidenceHash = operation.EntityGraphEvidenceHash,
            ReasonCodes = "[1]",
            IssuedAt = decision.IssuedAt,
            ExpiresAt = decision.ExpiresAt
        });
        await context.SaveChangesAsync();
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }
}