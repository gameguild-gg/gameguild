using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Entity representing a payment dispute</summary>
[Table("payment_disputes")]
[Index(nameof(PaymentId))]
[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(Type))]
[Index(nameof(CreatedAt))]
public class PaymentDispute : EntityBase
{
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

    /// <summary>Disputed amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>Dispute reason</summary>
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Detailed description</summary>
    [MaxLength(5000)]
    public string? Description { get; set; }

    /// <summary>Resolution details</summary>
    [MaxLength(2000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>Resolution type (won, lost, partial refund, etc.)</summary>
    public DisputeResolution? Resolution { get; set; }

    /// <summary>Resolved timestamp</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Resolved by user ID</summary>
    public Guid? ResolvedBy { get; set; }

    /// <summary>Due date for response</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Navigation property to dispute evidence</summary>
    public virtual ICollection<DisputeEvidence> Evidence { get; set; } = new List<DisputeEvidence>();

    /// <summary>Submit dispute</summary>
    public void Submit(DateTime dueDate)
    {
        Status = DisputeStatus.Submitted;
        DueDate = dueDate;
    }

    /// <summary>Move dispute to under review</summary>
    public void MoveToReview()
    {
        if (Status != DisputeStatus.Submitted)
            throw new InvalidOperationException($"Cannot move dispute to review from status {Status}");

        Status = DisputeStatus.UnderReview;
    }

    /// <summary>Request customer response</summary>
    public void RequestCustomerResponse(DateTime dueDate)
    {
        if (Status != DisputeStatus.UnderReview)
            throw new InvalidOperationException($"Cannot request customer response from status {Status}");

        Status = DisputeStatus.PendingCustomerResponse;
        DueDate = dueDate;
    }

    /// <summary>Request merchant response</summary>
    public void RequestMerchantResponse(DateTime dueDate)
    {
        if (Status != DisputeStatus.UnderReview)
            throw new InvalidOperationException($"Cannot request merchant response from status {Status}");

        Status = DisputeStatus.PendingMerchantResponse;
        DueDate = dueDate;
    }

    /// <summary>Resolve dispute</summary>
    public void Resolve(DisputeResolution resolution, string notes, Guid resolvedBy)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Cancelled)
            throw new InvalidOperationException($"Cannot resolve dispute with status {Status}");

        Status = DisputeStatus.Resolved;
        Resolution = resolution;
        ResolutionNotes = notes;
        ResolvedAt = DateTime.UtcNow;
        ResolvedBy = resolvedBy;
        DueDate = null;
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

    /// <summary>Cancel dispute</summary>
    public void Cancel(string reason)
    {
        if (Status == DisputeStatus.Resolved || Status == DisputeStatus.Won || Status == DisputeStatus.Lost)
            throw new InvalidOperationException($"Cannot cancel resolved dispute");

        Status = DisputeStatus.Cancelled;
        ResolutionNotes = reason;
        ResolvedAt = DateTime.UtcNow;
        DueDate = null;
    }
}

/// <summary>Dispute types</summary>
public enum DisputeType
{
    /// <summary>Fraudulent transaction</summary>
    Fraudulent = 0,

    /// <summary>Product not received</summary>
    ProductNotReceived = 1,

    /// <summary>Product not as described</summary>
    ProductNotAsDescribed = 2,

    /// <summary>Duplicate charge</summary>
    Duplicate = 3,

    /// <summary>Incorrect amount charged</summary>
    IncorrectAmount = 4,

    /// <summary>Service not provided</summary>
    ServiceNotProvided = 5,

    /// <summary>Credit not processed</summary>
    CreditNotProcessed = 6,

    /// <summary>Other reason</summary>
    Other = 7
}

/// <summary>Dispute status</summary>
public enum DisputeStatus
{
    /// <summary>Dispute submitted</summary>
    Submitted = 0,

    /// <summary>Under review</summary>
    UnderReview = 1,

    /// <summary>Pending customer response</summary>
    PendingCustomerResponse = 2,

    /// <summary>Pending merchant response</summary>
    PendingMerchantResponse = 3,

    /// <summary>Resolved</summary>
    Resolved = 4,

    /// <summary>Won by customer</summary>
    Won = 5,

    /// <summary>Lost by customer</summary>
    Lost = 6,

    /// <summary>Cancelled</summary>
    Cancelled = 7
}

/// <summary>Dispute resolution types</summary>
public enum DisputeResolution
{
    /// <summary>Customer won - full refund</summary>
    Won = 0,

    /// <summary>Customer lost - no refund</summary>
    Lost = 1,

    /// <summary>Partial refund</summary>
    PartialRefund = 2,

    /// <summary>Merchant credit</summary>
    MerchantCredit = 3,

    /// <summary>Replacement product</summary>
    Replacement = 4,

    /// <summary>Mutual agreement</summary>
    MutualAgreement = 5
}
