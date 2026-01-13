namespace GameGuild.Commerce.Payments;

/// <summary>
///     Service for managing payment disputes
/// </summary>
public interface IDisputeService
{
    /// <summary>Create a new dispute</summary>
    Task<PaymentDispute> CreateDisputeAsync(Guid paymentId, Guid userId, DisputeType type, decimal amount, string reason, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>Get dispute by ID</summary>
    Task<PaymentDispute?> GetDisputeByIdAsync(Guid disputeId, CancellationToken cancellationToken = default);

    /// <summary>Get disputes by payment ID</summary>
    Task<List<PaymentDispute>> GetDisputesByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    /// <summary>Get disputes by user ID</summary>
    Task<List<PaymentDispute>> GetDisputesByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Get disputes by status</summary>
    Task<List<PaymentDispute>> GetDisputesByStatusAsync(DisputeStatus status, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Update dispute status</summary>
    Task UpdateDisputeStatusAsync(Guid disputeId, DisputeStatus newStatus, DateTime? dueDate = null, CancellationToken cancellationToken = default);

    /// <summary>Resolve dispute</summary>
    Task ResolveDisputeAsync(Guid disputeId, DisputeResolution resolution, string? notes = null, Guid? resolvedBy = null, CancellationToken cancellationToken = default);

    /// <summary>Cancel dispute</summary>
    Task CancelDisputeAsync(Guid disputeId, string reason, CancellationToken cancellationToken = default);

    /// <summary>Add evidence to dispute</summary>
    Task<DisputeEvidence> AddEvidenceAsync(
        Guid disputeId,
        EvidenceType evidenceType,
        string title,
        string description,
        Guid submittedBy,
        bool isFromMerchant = false,
        string? fileUrl = null,
        string? fileName = null,
        long? fileSize = null,
        string? mimeType = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>Get evidence for dispute</summary>
    Task<List<DisputeEvidence>> GetDisputeEvidenceAsync(Guid disputeId, CancellationToken cancellationToken = default);
}
