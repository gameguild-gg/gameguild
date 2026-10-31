namespace GameGuild.Commerce;

/// <summary>
///     Coordinate a support ticket for a fraud manual-review case (issue #409: create a
///     support ticket for manual review cases). Lives in the shared commerce contracts so
///     the Payments and Subscriptions project graphs can raise review tickets without
///     referencing the Products module that owns the ticket system.
/// </summary>
public interface IFraudReviewTicketCoordinator
{
    /// <summary>
    ///     Opens a support ticket for one fraud review case. Returns the created ticket id,
    ///     or null when the coordinator cannot open one (missing subject identity, disabled
    ///     host composition) — ticket creation is a review convenience, never a gate
    ///     dependency, so a null result must never fail the calling flow.
    /// </summary>
    /// <param name="request">The review case to open a ticket for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Guid?> OpenReviewTicketAsync(FraudReviewTicketRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
///     One fraud manual-review case, as presented to the support-ticket system (issue #409).
///     All identity fields are subject-scoped: the ticket's customer slot carries the case
///     subject id (the held payment), following the course-support precedent.
/// </summary>
/// <param name="TenantId">Tenant that owns the case.</param>
/// <param name="SubjectEntityId">Id of the held entity (the payment held for review).</param>
/// <param name="SubjectEntityType">Type name of the held entity (e.g. <c>Payment</c>).</param>
/// <param name="Source">Where the review originated (e.g. <c>payment-gate</c>, <c>subscription-change</c>).</param>
/// <param name="Score">Aggregate fraud score (0-100).</param>
/// <param name="Reason">Bounded decision reason.</param>
/// <param name="Amount">Amount of the held attempt.</param>
/// <param name="Currency">ISO 4217 currency of the held attempt.</param>
/// <param name="EvidenceJson">Bounded JSON signal evidence bundle backing the score.</param>
public sealed record FraudReviewTicketRequest(
    Guid TenantId,
    Guid SubjectEntityId,
    string SubjectEntityType,
    string Source,
    int Score,
    string Reason,
    decimal Amount,
    string Currency,
    string? EvidenceJson);
