using FluentAssertions;
using GameGuild.API.Controllers;
using GameGuild.Economy.Treasury;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace GameGuild.API.UnitTests.Controllers;

public sealed class EconomyTreasuryStepUpTests
{
    [Fact]
    public async Task ApprovalAndDispatchConsumeSeparateOperationBoundReceipts()
    {
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var riskDecisionId = Guid.NewGuid();
        var withdrawals = new Mock<IDurableAdminWithdrawalApplicationService>(MockBehavior.Strict);
        withdrawals.Setup(item => item.ApproveAsync(
                new ApproveAdminWithdrawalCommand(tenantId, actorId, runId, 3), default))
            .ReturnsAsync((AdminWithdrawalRun)null!);
        withdrawals.Setup(item => item.DispatchAsync(
                new DispatchAdminWithdrawalCommand(
                    tenantId, actorId, runId, 4, riskDecisionId, "dispatch-fingerprint"), default))
            .ReturnsAsync((AdminWithdrawalRun)null!);
        var stepUp = new TestEconomyStepUpExecutor();
        var controller = new EconomyTreasuryAdministrationController(
            withdrawals.Object, stepUp, Accessor(tenantId, actorId));

        (await controller.Approve(runId, new(3, "approval-receipt"), default))
            .Should().BeOfType<OkObjectResult>();
        (await controller.Dispatch(
                runId,
                new(4, riskDecisionId, "dispatch-fingerprint", "dispatch-receipt"),
                default))
            .Should().BeOfType<OkObjectResult>();

        stepUp.Calls.Select(call => (call.Operation.OperationType, call.Receipt)).Should().Equal(
            ("economy.treasury.approve", "approval-receipt"),
            ("economy.treasury.dispatch", "dispatch-receipt"));
        withdrawals.VerifyAll();
    }

    private static IActorContextAccessor Accessor(Guid tenantId, Guid actorId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(item => item.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = actorId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string> { EconomyPermission.Keys.OperateTreasury },
            TypedAttributes = ActorAttributes.Empty,
            IsAuthenticated = true
        });
        return accessor.Object;
    }
}
