using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>Entity representing a payment dispute</summary>
[Table("payment_disputes")]
[Index(nameof(PaymentId))]
[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(Type))]
[Index(nameof(CreatedAt))]
[Index(nameof(DueDate))]
public class PaymentDispute : EntityBase
{
    /// <summary>Default constructor</summary>
    public PaymentDispute() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial dispute data</param>
    public PaymentDispute(object partial) : base(partial) { }

    /// <summary>Foreign key to the Payment entity</summary>
    [Required]
    public Guid PaymentId { get; set; }

    /// <summary>Foreign key to the User entity</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Dispute type</summary>
    public DisputeType Type { get; set; }

    /// <summary>Dispute status</summary>
    public DisputeStatus Status { get; set; } = DisputeStatus.Submitted;

    /// <summary>Dispute amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Dispute reason</summary>
    [Required]
    [MaxLength(100)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Detailed description</summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>Resolution notes</summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>Dispute resolution</summary>
    public DisputeResolution? Resolution { get; set; }

    /// <summary>Resolved timestamp</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Resolved by user ID</summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>Due date for response</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Navigation property to dispute evidence</summary>
    public virtual ICollection<DisputeEvidence> Evidence { get; } = new List<DisputeEvidence>();

    /// <summary>Submit the dispute</summary>
    public void Submit(DateTime dueDate)
    {
        Status = DisputeStatus.Submitted;
        DueDate = dueDate;
    }

    /// <summary>Move dispute to review</summary>
    public void MoveToReview()
    {
        if (Status != DisputeStatus.Submitted) throw new InvalidOperationException("Can only move submitted disputes to review");

        Status = DisputeStatus.UnderReview;
    }

    /// <summary>Request customer response</summary>
    public void RequestCustomerResponse(DateTime dueDate)
    {
        if (Status != DisputeStatus.UnderReview) throw new InvalidOperationException("Can only request customer response when under review");

        Status = DisputeStatus.PendingCustomerResponse;
        DueDate = dueDate;
    }

    /// <summary>Request merchant response</summary>
    public void RequestMerchantResponse(DateTime dueDate)
    {
        if (Status != DisputeStatus.UnderReview) throw new InvalidOperationException("Can only request merchant response when under review");

        Status = DisputeStatus.PendingMerchantResponse;
        DueDate = dueDate;
    }

    /// <summary>Resolve the dispute</summary>
    public void Resolve(DisputeResolution resolution, string notes, Guid resolvedBy)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Won || Status == DisputeStatus.Lost || Status == DisputeStatus.Cancelled) throw new InvalidOperationException("Dispute is already resolved");

        Status = DisputeStatus.Resolved;
        Resolution = resolution;
        ResolutionNotes = notes;
        ResolvedAt = SystemClock.UtcNow;
        ResolvedBy = resolvedBy;
    }

    /// <summary>Mark dispute as won</summary>
    public void MarkAsWon(string notes, Guid resolvedBy)
    {
        Resolve(DisputeResolution.Won, notes, resolvedBy);
        Status = DisputeStatus.Won;
    }

    /// <summary>Mark dispute as lost</summary>
    public void MarkAsLost(string notes, Guid resolvedBy)
    {
        Resolve(DisputeResolution.Lost, notes, resolvedBy);
        Status = DisputeStatus.Lost;
    }

    /// <summary>Cancel the dispute</summary>
    public void Cancel(string reason)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Won || Status == DisputeStatus.Lost) throw new InvalidOperationException("Cannot cancel a resolved dispute");

        Status = DisputeStatus.Cancelled;
        ResolutionNotes = reason;
        ResolvedAt = SystemClock.UtcNow;
    }
}
