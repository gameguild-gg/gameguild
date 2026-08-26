using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GameGuild.API.Authorization;
using GameGuild.Identity.Authentication;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace GameGuild.API.UnitTests.Authorization;

public sealed class EconomyStepUpExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_ConsumesBoundReceiptAndCommitsWithHashedEvidence()
    {
        var receipt = "opaque-step-up-receipt";
        var operation = EconomyStepUpOperation.Create(
            "economy.policy.approve", "policy:123", "123", "7");
        var stepUp = new Mock<IStepUpReceiptService>(MockBehavior.Strict);
        stepUp.Setup(service => service.ConsumeAsync(
                It.Is<StepUpOperationBinding>(binding =>
                    binding.OperationType == operation.OperationType &&
                    binding.TargetReference == operation.TargetReference &&
                    binding.PayloadHash == operation.PayloadHash),
                receipt,
                default))
            .Returns(Task.CompletedTask);
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(item => item.CommitAsync(default)).Returns(Task.CompletedTask);
        transaction.Setup(item => item.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var context = new Mock<IApplicationDbContext>(MockBehavior.Strict);
        context.Setup(item => item.BeginTransactionAsync(default)).ReturnsAsync(transaction.Object);
        var executor = new EconomyStepUpExecutor(stepUp.Object, context.Object);

        var result = await executor.ExecuteAsync(
            operation,
            receipt,
            (evidenceHash, _) => Task.FromResult(evidenceHash),
            default);

        result.Should().Be(Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(receipt))));
        stepUp.VerifyAll();
        context.VerifyAll();
        transaction.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_RollsBackReceiptConsumptionWhenProtectedActionFails()
    {
        var operation = EconomyStepUpOperation.Create(
            "economy.reserve.approve", "reserve:123", "123");
        var stepUp = new Mock<IStepUpReceiptService>();
        var transaction = new Mock<IDbContextTransaction>(MockBehavior.Strict);
        transaction.Setup(item => item.RollbackAsync(CancellationToken.None)).Returns(Task.CompletedTask);
        transaction.Setup(item => item.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var context = new Mock<IApplicationDbContext>();
        context.Setup(item => item.BeginTransactionAsync(default)).ReturnsAsync(transaction.Object);
        var executor = new EconomyStepUpExecutor(stepUp.Object, context.Object);

        var action = () => executor.ExecuteAsync<string>(
            operation,
            "receipt",
            (_, _) => throw new InvalidOperationException("posting failed"),
            default);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("posting failed");
        transaction.Verify(item => item.RollbackAsync(CancellationToken.None), Times.Once);
        transaction.Verify(item => item.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Create_ProducesStableUnambiguousPayloadHash()
    {
        var first = EconomyStepUpOperation.Create("operation", "target", "a|b", "c");
        var replay = EconomyStepUpOperation.Create("operation", "target", "a|b", "c");
        var ambiguousWithoutLengthPrefix = EconomyStepUpOperation.Create("operation", "target", "a", "b|c");

        first.Should().Be(replay);
        first.PayloadHash.Should().HaveLength(64);
        first.PayloadHash.Should().NotBe(ambiguousWithoutLengthPrefix.PayloadHash);
    }
}
