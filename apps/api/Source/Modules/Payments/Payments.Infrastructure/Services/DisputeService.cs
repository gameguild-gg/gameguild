using GameGuild.Modules.Payments.Payments.Application.Services;
using GameGuild.Database;
using GameGuild.Modules.Payments.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Payments.Payments.Infrastructure.Services;

/// <summary>Dispute service implementation</summary>
public class DisputeService : IDisputeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DisputeService> _logger;

    public DisputeService(ApplicationDbContext context, ILogger<DisputeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PaymentDispute> CreateDisputeAsync(
        Guid paymentId,
        Guid userId,
        DisputeType type,
        decimal amount,
        string reason,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating dispute for payment {PaymentId} by user {UserId}", paymentId, userId);

        var dueDate = DateTime.UtcNow.AddDays(14); // Default 14 days to respond

        var dispute = new PaymentDispute
        {
            PaymentId = paymentId,
            UserId = userId,
            Type = type,
            Amount = amount,
            Reason = reason,
            Description = description,
            Status = DisputeStatus.Submitted,
            DueDate = dueDate
        };

        _context.PaymentDisputes.Add(dispute);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dispute created with ID {DisputeId}", dispute.Id);
        return dispute;
    }

    public async Task<PaymentDispute?> GetDisputeByIdAsync(Guid disputeId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentDisputes
            .Include(d => d.Evidence)
            .FirstOrDefaultAsync(d => d.Id == disputeId, cancellationToken);
    }

    public async Task<List<PaymentDispute>> GetDisputesByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentDisputes
            .Include(d => d.Evidence)
            .Where(d => d.PaymentId == paymentId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PaymentDispute>> GetDisputesByUserIdAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentDisputes
            .Include(d => d.Evidence)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<PaymentDispute>> GetDisputesByStatusAsync(DisputeStatus status, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _context.PaymentDisputes
            .Include(d => d.Evidence)
            .Where(d => d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateDisputeStatusAsync(Guid disputeId, DisputeStatus newStatus, DateTime? dueDate = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating dispute {DisputeId} status to {Status}", disputeId, newStatus);

        var dispute = await GetDisputeByIdAsync(disputeId, cancellationToken);
        if (dispute == null)
            throw new InvalidOperationException($"Dispute {disputeId} not found");

        switch (newStatus)
        {
            case DisputeStatus.UnderReview:
                dispute.MoveToReview();
                break;
            case DisputeStatus.PendingCustomerResponse:
                dispute.RequestCustomerResponse(dueDate ?? DateTime.UtcNow.AddDays(7));
                break;
            case DisputeStatus.PendingMerchantResponse:
                dispute.RequestMerchantResponse(dueDate ?? DateTime.UtcNow.AddDays(7));
                break;
            default:
                dispute.Status = newStatus;
                if (dueDate.HasValue)
                    dispute.DueDate = dueDate.Value;
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Dispute {DisputeId} status updated to {Status}", disputeId, newStatus);
    }

    public async Task ResolveDisputeAsync(
        Guid disputeId,
        DisputeResolution resolution,
        string notes,
        Guid resolvedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resolving dispute {DisputeId} with resolution {Resolution}", disputeId, resolution);

        var dispute = await GetDisputeByIdAsync(disputeId, cancellationToken);
        if (dispute == null)
            throw new InvalidOperationException($"Dispute {disputeId} not found");

        if (resolution == DisputeResolution.Won)
            dispute.MarkAsWon(notes, resolvedBy);
        else if (resolution == DisputeResolution.Lost)
            dispute.MarkAsLost(notes, resolvedBy);
        else
            dispute.Resolve(resolution, notes, resolvedBy);

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Dispute {DisputeId} resolved", disputeId);
    }

    public async Task CancelDisputeAsync(Guid disputeId, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling dispute {DisputeId}", disputeId);

        var dispute = await GetDisputeByIdAsync(disputeId, cancellationToken);
        if (dispute == null)
            throw new InvalidOperationException($"Dispute {disputeId} not found");

        dispute.Cancel(reason);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Dispute {DisputeId} cancelled", disputeId);
    }

    public async Task<DisputeEvidence> AddEvidenceAsync(
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
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding evidence to dispute {DisputeId}", disputeId);

        var dispute = await GetDisputeByIdAsync(disputeId, cancellationToken);
        if (dispute == null)
            throw new InvalidOperationException($"Dispute {disputeId} not found");

        var evidence = new DisputeEvidence
        {
            DisputeId = disputeId,
            EvidenceType = evidenceType,
            Title = title,
            Description = description,
            SubmittedBy = submittedBy,
            IsFromMerchant = isFromMerchant,
            FileUrl = fileUrl,
            FileName = fileName,
            FileSize = fileSize,
            MimeType = mimeType,
            SubmittedAt = DateTime.UtcNow
        };

        _context.DisputeEvidence.Add(evidence);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Evidence {EvidenceId} added to dispute {DisputeId}", evidence.Id, disputeId);
        return evidence;
    }

    public async Task<List<DisputeEvidence>> GetDisputeEvidenceAsync(Guid disputeId, CancellationToken cancellationToken = default)
    {
        return await _context.DisputeEvidence
            .Where(e => e.DisputeId == disputeId)
            .OrderByDescending(e => e.SubmittedAt)
            .ToListAsync(cancellationToken);
    }
}
