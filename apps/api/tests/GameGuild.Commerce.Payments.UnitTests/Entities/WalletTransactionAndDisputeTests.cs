using FluentAssertions;
using Xunit;

namespace GameGuild.Commerce.Payments.UnitTests.Entities;

public class WalletTransactionTests
{
    [Fact]
    public void Complete_ShouldSetStatusAndProcessedAt()
    {
        var tx = new WalletTransaction
        {
            WalletId = Guid.NewGuid(),
            Amount = 50m,
            Status = TransactionStatus.Pending
        };

        tx.Complete();

        tx.Status.Should().Be(TransactionStatus.Completed);
        tx.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void Fail_ShouldSetStatusAndNotes()
    {
        var tx = new WalletTransaction
        {
            WalletId = Guid.NewGuid(),
            Amount = 50m,
            Status = TransactionStatus.Pending
        };

        tx.Fail("Insufficient funds");

        tx.Status.Should().Be(TransactionStatus.Failed);
        tx.Notes.Should().Be("Insufficient funds");
        tx.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public void DefaultStatus_ShouldBePending()
    {
        var tx = new WalletTransaction();
        tx.Status.Should().Be(TransactionStatus.Pending);
    }
}

public class PaymentDisputeTests
{
    private PaymentDispute CreateDispute() => new()
    {
        PaymentId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        Type = DisputeType.Fraudulent,
        Status = DisputeStatus.Submitted,
        Amount = 100m,
        Reason = "Test"
    };

    [Fact]
    public void Submit_ShouldSetStatusAndDueDate()
    {
        var dispute = CreateDispute();
        var dueDate = DateTime.UtcNow.AddDays(30);

        dispute.Submit(dueDate);

        dispute.Status.Should().Be(DisputeStatus.Submitted);
        dispute.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void MoveToReview_ShouldChangeStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.Submitted;

        dispute.MoveToReview();

        dispute.Status.Should().Be(DisputeStatus.UnderReview);
    }

    [Fact]
    public void MoveToReview_ShouldThrow_WhenNotSubmitted()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.Resolved;

        var act = () => dispute.MoveToReview();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequestCustomerResponse_ShouldChangeStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.UnderReview;
        var dueDate = DateTime.UtcNow.AddDays(7);

        dispute.RequestCustomerResponse(dueDate);

        dispute.Status.Should().Be(DisputeStatus.PendingCustomerResponse);
        dispute.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void RequestMerchantResponse_ShouldChangeStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.UnderReview;
        var dueDate = DateTime.UtcNow.AddDays(7);

        dispute.RequestMerchantResponse(dueDate);

        dispute.Status.Should().Be(DisputeStatus.PendingMerchantResponse);
        dispute.DueDate.Should().Be(dueDate);
    }

    [Fact]
    public void Resolve_ShouldSetResolutionDetails()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.UnderReview;
        var resolvedBy = Guid.NewGuid();

        dispute.Resolve(DisputeResolution.Won, "Refund issued", resolvedBy);

        dispute.Status.Should().Be(DisputeStatus.Resolved);
        dispute.Resolution.Should().Be(DisputeResolution.Won);
        dispute.ResolutionNotes.Should().Be("Refund issued");
        dispute.ResolvedBy.Should().Be(resolvedBy);
        dispute.ResolvedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsWon_ShouldSetWonStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.UnderReview;
        var resolvedBy = Guid.NewGuid();

        dispute.MarkAsWon("Customer wins", resolvedBy);

        dispute.Status.Should().Be(DisputeStatus.Won);
    }

    [Fact]
    public void MarkAsLost_ShouldSetLostStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.UnderReview;
        var resolvedBy = Guid.NewGuid();

        dispute.MarkAsLost("Insufficient evidence", resolvedBy);

        dispute.Status.Should().Be(DisputeStatus.Lost);
    }

    [Fact]
    public void Cancel_ShouldSetCancelledStatus()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.Submitted;

        dispute.Cancel("No longer needed");

        dispute.Status.Should().Be(DisputeStatus.Cancelled);
    }

    [Fact]
    public void Cancel_ShouldThrow_WhenAlreadyResolved()
    {
        var dispute = CreateDispute();
        dispute.Status = DisputeStatus.Resolved;

        var act = () => dispute.Cancel("reason");

        act.Should().Throw<InvalidOperationException>();
    }
}
