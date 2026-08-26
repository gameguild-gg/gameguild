using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.Economy.Operations;
using GameGuild.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public sealed class EconomyLegacyMigrationAdministrationControllerTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task EveryOperationForbidsAnActorWithoutTheSegregatedPermission()
    {
        var migration = new Mock<ILegacyEconomyShadowMigration>(MockBehavior.Strict);
        var controller = CreateController(migration.Object, authorized: false);
        var batchId = Guid.NewGuid();

        (await controller.Get(batchId, default)).Should().BeOfType<ForbidResult>();
        (await controller.Capture(new(Guid.NewGuid(), "BR"), default)).Should().BeOfType<ForbidResult>();
        (await controller.Backfill(batchId, new(Guid.NewGuid(), Guid.NewGuid(), "operation"), default))
            .Should().BeOfType<ForbidResult>();
        (await controller.Reconcile(batchId, default)).Should().BeOfType<ForbidResult>();
        (await controller.ProposeCutover(batchId, new("reason", "reauth"), default))
            .Should().BeOfType<ForbidResult>();
        (await controller.ApproveCutover(batchId, new("reauth"), default)).Should().BeOfType<ForbidResult>();
        (await controller.RollbackCutover(batchId, new("reason", "reauth"), default))
            .Should().BeOfType<ForbidResult>();
        migration.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUsesActorTenantAndReturnsNotFoundWithoutLeakingAnotherTenant()
    {
        var tenantId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var migration = new Mock<ILegacyEconomyShadowMigration>(MockBehavior.Strict);
        migration.Setup(service => service.GetAsync(tenantId, batchId, default))
            .ReturnsAsync((LegacyEconomyShadowBatchView?)null);
        var controller = CreateController(migration.Object, tenantId: tenantId);

        var result = await controller.Get(batchId, default);

        result.Should().BeOfType<NotFoundResult>();
        migration.VerifyAll();
    }

    [Fact]
    public async Task OperationsBindTenantActorAndServerTimeInsteadOfAcceptingAuthorityInBodies()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var batchId = Guid.NewGuid();
        var legacyWalletId = Guid.NewGuid();
        var riskDecisionId = Guid.NewGuid();
        var view = View(batchId, tenantId);
        var migration = new Mock<ILegacyEconomyShadowMigration>(MockBehavior.Strict);
        migration.Setup(service => service.GetAsync(tenantId, batchId, default)).ReturnsAsync(view);
        migration.Setup(service => service.CaptureAsync(
                It.Is<CaptureLegacyEconomyShadowCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.JurisdictionCode == "BR" && command.CapturedAt == Now), default))
            .ReturnsAsync(view);
        migration.Setup(service => service.BackfillAsync(
                It.Is<BackfillLegacyEconomyWalletCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.LegacyWalletId == legacyWalletId && command.RiskDecisionId == riskDecisionId &&
                    command.OperationFingerprint == "backfill" && command.PostedAt == Now), default))
            .ReturnsAsync(view);
        migration.Setup(service => service.ReconcileAsync(
                It.Is<ReconcileLegacyEconomyShadowCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.ReconciledAt == Now), default))
            .ReturnsAsync(view);
        migration.Setup(service => service.ProposeCutoverAsync(
                It.Is<ProposeLegacyEconomyCutoverCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.Reason == "cutover" && command.ReauthenticationHash == "proposal-proof" &&
                    command.ProposedAt == Now), default))
            .ReturnsAsync(view);
        migration.Setup(service => service.ApproveCutoverAsync(
                It.Is<ApproveLegacyEconomyCutoverCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.ReauthenticationHash == "approval-proof" && command.ApprovedAt == Now), default))
            .ReturnsAsync(view);
        migration.Setup(service => service.RollbackCutoverAsync(
                It.Is<RollbackLegacyEconomyCutoverCommand>(command =>
                    command.BatchId == batchId && command.TenantId == tenantId && command.ActorId == actorId &&
                    command.Reason == "rollback" && command.ReauthenticationHash == "rollback-proof" &&
                    command.RolledBackAt == Now), default))
            .ReturnsAsync(view);
        var controller = CreateController(migration.Object, tenantId: tenantId, actorId: actorId);

        (await controller.Get(batchId, default)).Should().BeOfType<OkObjectResult>();
        (await controller.Capture(new(batchId, "BR"), default)).Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        (await controller.Backfill(batchId, new(legacyWalletId, riskDecisionId, "backfill"), default))
            .Should().BeOfType<OkObjectResult>();
        (await controller.Reconcile(batchId, default)).Should().BeOfType<OkObjectResult>();
        (await controller.ProposeCutover(batchId, new("cutover", "proposal-proof"), default))
            .Should().BeOfType<OkObjectResult>();
        (await controller.ApproveCutover(batchId, new("approval-proof"), default))
            .Should().BeOfType<OkObjectResult>();
        (await controller.RollbackCutover(batchId, new("rollback", "rollback-proof"), default))
            .Should().BeOfType<OkObjectResult>();
        migration.VerifyAll();
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task CaptureMapsDomainFailuresToSafeHttpResults(Exception exception, Type expectedType, int? status)
    {
        var migration = new Mock<ILegacyEconomyShadowMigration>();
        migration.Setup(service => service.CaptureAsync(
                It.IsAny<CaptureLegacyEconomyShadowCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);
        var controller = CreateController(migration.Object);

        var result = await controller.Capture(new(Guid.NewGuid(), "BR"), default);

        result.Should().BeOfType(expectedType);
        if (status.HasValue) ((ObjectResult)result).StatusCode.Should().Be(status);
    }

    public static TheoryData<Exception, Type, int?> Failures => new()
    {
        { new KeyNotFoundException("missing"), typeof(NotFoundResult), null },
        { new ArgumentException("invalid"), typeof(BadRequestObjectResult), StatusCodes.Status400BadRequest },
        { new LegacyEconomyShadowMigrationException("conflict"), typeof(ConflictObjectResult), StatusCodes.Status409Conflict },
        { new DbUpdateConcurrencyException("stale"), typeof(ConflictObjectResult), StatusCodes.Status409Conflict },
        {
            new EconomyCapabilityAuthorizationException(
                EconomyCapabilityReadinessStatus.Disabled, ["capability-disabled"]),
            typeof(ObjectResult),
            StatusCodes.Status503ServiceUnavailable
        }
    };

    private static EconomyLegacyMigrationAdministrationController CreateController(
        ILegacyEconomyShadowMigration migration,
        bool authorized = true,
        Guid? tenantId = null,
        Guid? actorId = null)
    {
        var actorContext = new Mock<IActorContextAccessor>();
        actorContext.SetupGet(accessor => accessor.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = (actorId ?? Guid.NewGuid()).ToString(),
            TenantId = tenantId ?? Guid.NewGuid(),
            Roles = new HashSet<string>(),
            Permissions = authorized
                ? new HashSet<string> { EconomyPermission.Keys.ManageLegacyMigration }
                : new HashSet<string>(),
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        });
        return new EconomyLegacyMigrationAdministrationController(
            migration, actorContext.Object, new FixedTimeProvider());
    }

    private static LegacyEconomyShadowBatchView View(Guid batchId, Guid tenantId) => new(
        batchId,
        tenantId,
        LegacyEconomyShadowState.Captured,
        1,
        0,
        0,
        0,
        0,
        0,
        0,
        "wallet-hash",
        "transaction-hash",
        "ledger-hash",
        null,
        []);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
