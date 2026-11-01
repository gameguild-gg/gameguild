using System.Text.Json;
using GameGuild;
using GameGuild.Commerce;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Products;

/// <summary>
///     <see cref="IFraudReviewTicketCoordinator"/> implementation (issue #409: create a
///     support ticket for manual review cases). Opens one in-app support ticket on the
///     existing ticket system for a payment held for manual fraud review, so review cases
///     surface in the same support queue staff already work. The ticket's customer slot
///     carries the held payment id (the course-support precedent overloads it the same
///     way); the reporter is the platform's fraud gate, attributed to the tenant. Tickets
///     are High priority, or Urgent for Critical-band scores.
/// </summary>
public sealed class FraudReviewSupportTicketCoordinator(
    IApplicationDbContext context,
    ILogger<FraudReviewSupportTicketCoordinator> logger) : IFraudReviewTicketCoordinator
{
    private const string Category = "fraud-review";

    /// <inheritdoc />
    public async Task<Guid?> OpenReviewTicketAsync(FraudReviewTicketRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.TenantId == Guid.Empty || request.SubjectEntityId == Guid.Empty)
        {
            logger.LogDebug(
                "Fraud review ticket for {EntityType} {SubjectEntityId} skipped: incomplete case identity",
                request.SubjectEntityType,
                request.SubjectEntityId);
            return null;
        }

        var priority = request.Score > 85 ? SupportTicketPriority.Urgent : SupportTicketPriority.High;
        var ticket = SupportTicket.Open(
            request.TenantId,
            customerId: request.SubjectEntityId,
            customerName: $"{request.SubjectEntityType} {request.SubjectEntityId}",
            reporterUserId: request.TenantId,
            reporterName: "Fraud detection gate",
            reporterEmail: null,
            subject: $"Fraud review required: {request.SubjectEntityType} {request.SubjectEntityId} (score {request.Score})",
            body: BuildBody(request),
            priority,
            Category);

        await context.Set<SupportTicket>().AddAsync(ticket, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Fraud review ticket {TicketId} opened for {EntityType} {SubjectEntityId} (score {Score}, priority {Priority})",
            ticket.Id,
            request.SubjectEntityType,
            request.SubjectEntityId,
            request.Score,
            priority);

        return ticket.Id;
    }

    private static string BuildBody(FraudReviewTicketRequest request) =>
        JsonSerializer.Serialize(new
        {
            source = request.Source,
            subjectEntityType = request.SubjectEntityType,
            subjectEntityId = request.SubjectEntityId,
            score = request.Score,
            reason = request.Reason,
            amount = request.Amount,
            currency = request.Currency,
            evidence = request.EvidenceJson
        });
}
