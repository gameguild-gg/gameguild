using GameGuild.Compliance.Consent;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

/// <summary>Consent monitoring snapshot for one data subject.</summary>
public sealed record DataSubjectConsentMonitoringResponse(
    Guid UserId,
    IReadOnlyList<ConsentStateResponse> Consents,
    int GrantedCount,
    int RevokedCount,
    IReadOnlyList<DataSubjectRequestResponse> Requests)
{
    public bool HasOpenRequests => Requests.Any(request => request.Status is "Pending" or "InProgress");
}

public sealed record ConsentStateResponse(
    Guid Id,
    Guid PolicyVersionId,
    bool IsGranted,
    DateTime ConsentGivenAt,
    DateTime? ConsentRevokedAt,
    string? ConsentMethod);

public sealed record DataSubjectRequestResponse(
    Guid Id,
    Guid UserId,
    string RequestType,
    string Status,
    DateTime Deadline,
    DateTime? ProcessedAt,
    string? ProcessingNotes,
    bool IsOverdue)
{
    public static DataSubjectRequestResponse From(DataSubjectRequest request) => new(
        request.Id,
        request.UserId,
        request.RequestType.ToString(),
        request.Status.ToString(),
        request.Deadline,
        request.ProcessedAt,
        request.ProcessingNotes,
        request.Status is DataSubjectRequestStatus.Pending or DataSubjectRequestStatus.InProgress
            && request.Deadline < DateTime.UtcNow);
}

public interface IDataSubjectAccessMonitoringService
{
    /// <summary>Consent monitoring for one data subject: every consent state plus their rights requests.</summary>
    Task<DataSubjectConsentMonitoringResponse> GetSubjectMonitoringAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Data subject rights requests visible to the caller's tenant, with overdue flags and consent summaries.</summary>
    Task<IReadOnlyList<DataSubjectRequestResponse>> GetRequestsAsync(
        Guid? tenantId, string? status, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Aggregated monitoring report: open, fulfilled and overdue request counts with consent totals.</summary>
    Task<DataSubjectAccessMonitoringSummary> GetSummaryAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data subject access tracking with consent monitoring (issue #186): surfaces the consent state and
/// the GDPR rights-request lifecycle per data subject, with statutory deadline (30 day) tracking.
/// The lifecycle itself is owned by the consent module; this service provides the audited monitoring
/// surface over the same durable records that every lifecycle transition is recorded against.
/// </summary>
public sealed class DataSubjectAccessMonitoringService(
    IApplicationDbContext context) : IDataSubjectAccessMonitoringService
{
    public async Task<DataSubjectConsentMonitoringResponse> GetSubjectMonitoringAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var consents = await context.Set<UserConsent>()
            .Where(consent => consent.UserId == userId)
            .OrderByDescending(consent => consent.ConsentGivenAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var requests = await context.Set<DataSubjectRequest>()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.CreatedAt)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new DataSubjectConsentMonitoringResponse(
            userId,
            consents.Select(consent => new ConsentStateResponse(
                consent.Id, consent.PolicyVersionId, consent.IsGranted, consent.ConsentGivenAt,
                consent.ConsentRevokedAt, consent.ConsentMethod)).ToList(),
            consents.Count(consent => consent.IsGranted),
            consents.Count(consent => !consent.IsGranted),
            requests.Select(DataSubjectRequestResponse.From).ToList());
    }

    public async Task<IReadOnlyList<DataSubjectRequestResponse>> GetRequestsAsync(
        Guid? tenantId, string? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<DataSubjectRequest>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<DataSubjectRequestStatus>(status, ignoreCase: true, out var parsed))
            {
                throw new FieldLevelAuditValidationException("status", $"Unknown data subject request status '{status}'.");
            }
            query = query.Where(request => request.Status == parsed);
        }

        var requests = await query
            .OrderByDescending(request => request.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return requests.Select(DataSubjectRequestResponse.From).ToList();
    }

    public async Task<DataSubjectAccessMonitoringSummary> GetSummaryAsync(
        Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var requests = await context.Set<DataSubjectRequest>()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var consents = await context.Set<UserConsent>()
            .CountAsync(consent => consent.IsGranted, cancellationToken).ConfigureAwait(false);

        return new DataSubjectAccessMonitoringSummary(
            requests.Count,
            requests.Count(request => request.Status is DataSubjectRequestStatus.Pending or DataSubjectRequestStatus.InProgress),
            requests.Count(request => request.Status == DataSubjectRequestStatus.Completed),
            requests.Count(request => request.Status is DataSubjectRequestStatus.Pending or DataSubjectRequestStatus.InProgress
                && request.Deadline < DateTime.UtcNow),
            consents);
    }
}

/// <summary>Aggregate data subject request monitoring counters.</summary>
public sealed record DataSubjectAccessMonitoringSummary(
    int TotalRequests,
    int OpenRequests,
    int FulfilledRequests,
    int OverdueRequests,
    int ActiveConsents);
