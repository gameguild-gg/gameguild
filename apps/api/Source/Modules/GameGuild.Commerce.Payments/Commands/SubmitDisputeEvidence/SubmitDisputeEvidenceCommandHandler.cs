using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Handler for <see cref="SubmitDisputeEvidenceCommand"/> (issue #419 "submit evidence bundle
///     to gateway" / "track submission status"). Guarded (permission + authenticated actor +
///     tenant match) and audited. Assembles every evidence row attached to the dispute — uploaded
///     files and the automatically collected invoice/receipt/subscription records — into one
///     bundle and delivers it through the gateway's dispute-challenge surface:
///     <list type="bullet">
///         <item>each stored file is delivered through the provider's native file/evidence fields
///         (card networks do not review external download links), with the accepted provider file
///         ids and the SHA-256 digests of the delivered bytes retained on the evidence rows;</item>
///         <item>the accepted evidence manifest is frozen on the dispute: an identical re-submission
///         replays idempotently (no second gateway call), while added or changed evidence is
///         rejected — the provider's final evidence response cannot be amended;</item>
///         <item>the idempotency key is bound to the manifest hash, so a retry after a crash between
///         the gateway call and the local persist replays the same provider request.</item>
///     </list>
///     Fails closed when the dispute has no gateway identity or carries no evidence.
/// </summary>
public sealed class SubmitDisputeEvidenceCommandHandler(
    IApplicationDbContext context,
    IPaymentRepository paymentRepository,
    IPaymentGateway paymentGateway,
    IRevenueAuditService revenueAuditService,
    IActorContextAccessor actorContextAccessor,
    ILogger<SubmitDisputeEvidenceCommandHandler> logger,
    IDisputeEvidenceFileStore? evidenceFileStore = null) : ICommandHandler<SubmitDisputeEvidenceCommand, DisputeActionResult>
{
    private const int IdempotencyHashLength = 16;

    public async Task<DisputeActionResult> Handle(SubmitDisputeEvidenceCommand request, CancellationToken cancellationToken)
    {
        var actor = actorContextAccessor.ActorContext;
        var actorId = actor.SubjectIdAsGuid
            ?? throw new UnauthorizedAccessException("An authenticated administrator is required to submit dispute evidence.");

        var dispute = await context.Set<PaymentDispute>()
            .Include(d => d.Evidence)
            .FirstOrDefaultAsync(d => d.Id == request.DisputeId, cancellationToken)
            .ConfigureAwait(false);
        if (dispute is null)
        {
            return Failure(request.DisputeId, $"Dispute {request.DisputeId} not found");
        }

        if (dispute.GatewayDisputeId is null)
        {
            return Failure(request.DisputeId, "Only disputes reconciled from a gateway webhook can have evidence submitted to the gateway.");
        }

        var payment = await paymentRepository.GetByIdAsync(dispute.PaymentId, cancellationToken).ConfigureAwait(false);
        if (payment is null)
        {
            return Failure(request.DisputeId, $"Payment {dispute.PaymentId} for the dispute no longer exists");
        }

        if (!actor.IsSystemAdmin && payment.TenantId != actor.TenantId)
        {
            throw new UnauthorizedAccessException("The dispute does not belong to the actor's tenant.");
        }

        if (dispute.Status is DisputeStatus.Resolved or DisputeStatus.Won or DisputeStatus.Lost or DisputeStatus.Cancelled)
        {
            return Failure(request.DisputeId, $"Dispute {request.DisputeId} is already resolved ({dispute.Status})");
        }

        var evidenceRows = dispute.Evidence
            .OrderBy(evidence => evidence.SubmittedAt)
            .ThenBy(evidence => evidence.Id)
            .ToList();
        if (evidenceRows.Count == 0)
        {
            return Failure(request.DisputeId, "Dispute has no evidence attached; attach evidence before submitting the bundle.");
        }

        // Resolve the authorized stored bytes once per row so the digest computed here is the
        // digest of exactly the bytes the provider will receive (issue #419 provider file
        // delivery). Rows whose stored content cannot be resolved degrade to their textual
        // reference — never to a fabricated upload.
        var preparedItems = new List<(DisputeEvidence Row, GatewayDisputeEvidenceItem Item, string Digest)>();
        foreach (var row in evidenceRows)
        {
            var digest = $"unresolved:{row.Id:N}";
            byte[]? bufferedContent = null;
            string? bufferedContentType = null;
            if (evidenceFileStore is not null && (!string.IsNullOrWhiteSpace(row.FileUrl) || !string.IsNullOrWhiteSpace(row.FileName)))
            {
                try
                {
                    var resolved = await evidenceFileStore
                        .ResolveAsync(row, cancellationToken)
                        .ConfigureAwait(false);
                    if (resolved is not null)
                    {
                        using (resolved.Content)
                        {
                            using var buffer = new MemoryStream();
                            await resolved.Content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                            bufferedContent = buffer.ToArray();
                        }

                        bufferedContentType = resolved.ContentType ?? row.MimeType;
                        digest = Convert.ToHexString(SHA256.HashData(bufferedContent)).ToLowerInvariant();
                    }
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogWarning(
                        exception,
                        "Stored content for dispute evidence {EvidenceId} could not be resolved; the item degrades to its textual reference. Error type: {ErrorType}",
                        row.Id,
                        exception.GetType().FullName);
                }
            }

            // Each delivery hands the provider a fresh read-only view of the buffered bytes so
            // multiple evidence fields can consume the same stored content.
            Func<CancellationToken, Task<GatewayDisputeEvidenceContent?>>? contentAccessor = null;
            if (bufferedContent is not null)
            {
                var bytes = bufferedContent;
                var contentType = bufferedContentType;
                contentAccessor = _ => Task.FromResult<GatewayDisputeEvidenceContent?>(
                    new GatewayDisputeEvidenceContent(
                        new MemoryStream(bytes, writable: false),
                        contentType,
                        bytes.Length));
            }

            var item = new GatewayDisputeEvidenceItem(
                Key: row.Id.ToString(),
                FileName: row.FileName,
                FileUrl: row.FileUrl,
                MimeType: bufferedContentType ?? row.MimeType,
                EvidenceTypeKey: row.EvidenceType.ToString(),
                ContentDigest: digest,
                ContentAccessor: contentAccessor);
            preparedItems.Add((row, item, digest));
        }

        // Freeze the candidate manifest: one entry per evidence row (id, retained provider file
        // id, digest). The manifest hash binds both the replay check and the idempotency key.
        var manifestHash = ComputeManifestHash(preparedItems.Select(entry =>
            (entry.Row.Id.ToString(), entry.Row.ProviderFileId, entry.Digest)));

        if (dispute.EvidenceManifestHash is not null)
        {
            if (string.Equals(dispute.EvidenceManifestHash, manifestHash, StringComparison.Ordinal))
            {
                // Identical evidence after an accepted submission: an idempotent replay. The
                // provider already holds the final evidence response, so no new gateway call is
                // made and the frozen submission state is returned untouched.
                logger.LogInformation(
                    "Evidence submission for dispute {DisputeId} replayed against the frozen manifest; no gateway call made",
                    dispute.Id);
                return new DisputeActionResult { Success = true, DisputeId = dispute.Id };
            }

            return Failure(
                dispute.Id,
                "An evidence bundle was already submitted to the gateway for this dispute and the gateway's final response cannot be amended with additional or changed files; open a new dispute cycle or manage it in the gateway console.");
        }

        var bundle = ComposeBundle(request.Summary, evidenceRows);
        var gatewayResult = await paymentGateway.ChallengeDisputeAsync(
                new GatewayDisputeChallengeRequest(
                    IdempotencyKey: $"dispute_evidence_{dispute.Id}_{manifestHash[..IdempotencyHashLength]}",
                    ExternalDisputeId: dispute.GatewayDisputeId,
                    EvidenceSummary: bundle,
                    ProviderId: payment.Provider,
                    EvidenceFiles: preparedItems.Select(entry => entry.Item).ToList()),
                cancellationToken)
            .ConfigureAwait(false);

        if (!gatewayResult.Success)
        {
            logger.LogWarning(
                "Gateway refused the evidence bundle for dispute {DisputeId} ({GatewayDisputeId}): {ErrorMessage}",
                dispute.Id,
                dispute.GatewayDisputeId,
                gatewayResult.ErrorMessage);
            return Failure(request.DisputeId, $"The gateway refused the evidence bundle: {gatewayResult.ErrorMessage}");
        }

        var submittedAt = SystemClock.UtcNow;
        var statusBefore = dispute.Status;

        // Submission tracking (issue #419): every evidence row keeps the timestamp of when it
        // first reached the gateway and the provider acceptance (file id + digest) returned for
        // it; re-submissions never rewrite that history.
        var acceptancesByKey = gatewayResult.AcceptedEvidence?.ToDictionary(
            acceptance => acceptance.Key,
            acceptance => acceptance,
            StringComparer.Ordinal) ?? new Dictionary<string, GatewayDisputeEvidenceAcceptance>(StringComparer.Ordinal);
        foreach (var (row, _, digest) in preparedItems)
        {
            row.GatewaySubmittedAt ??= submittedAt;
            acceptancesByKey.TryGetValue(row.Id.ToString(), out var acceptance);
            row.RecordProviderAcceptance(acceptance?.ProviderFileId, digest);
        }

        dispute.MarkEvidenceSubmitted(submittedAt);
        dispute.FreezeEvidenceManifest(BuildManifestJson(preparedItems), manifestHash);
        if (dispute.Status == DisputeStatus.Submitted)
        {
            dispute.MoveToReview();
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await revenueAuditService.RecordAuditTrailAsync(
            entityType: nameof(PaymentDispute),
            entityId: dispute.Id,
            action: "DisputeEvidenceSubmitted",
            changedBy: actorId,
            oldValue: JsonSerializer.Serialize(new
            {
                status = statusBefore.ToString(),
                evidenceSubmittedAt = (DateTime?)null
            }),
            newValue: JsonSerializer.Serialize(new
            {
                status = dispute.Status.ToString(),
                evidenceSubmittedAt = dispute.EvidenceSubmittedAt,
                evidenceCount = evidenceRows.Count,
                manifestHash,
                providerFiles = preparedItems.Count(entry => entry.Row.ProviderFileId is not null)
            }),
            reason: $"Evidence bundle of {evidenceRows.Count} item(s) submitted to gateway {dispute.GatewayName}:{dispute.GatewayDisputeId}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Evidence bundle of {Count} item(s) ({ProviderFileCount} through provider file fields) submitted to gateway for dispute {DisputeId} by {ActorId}",
            evidenceRows.Count,
            preparedItems.Count(entry => entry.Row.ProviderFileId is not null),
            dispute.Id,
            actorId);

        return new DisputeActionResult { Success = true, DisputeId = dispute.Id };
    }

    /// <summary>
    ///     Assembles the textual evidence bundle (issue #419 evidence package): an optional
    ///     summary line followed by one entry per evidence row — type, title, description and the
    ///     file reference when present. This text accompanies the structured provider file
    ///     delivery so the gateway receives the complete merchant evidence in one submission.
    /// </summary>
    internal static string ComposeBundle(string? summary, IReadOnlyList<DisputeEvidence> evidenceRows)
    {
        var bundle = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(summary))
        {
            bundle.AppendLine(summary.Trim());
        }

        bundle.Append($"Evidence bundle: {evidenceRows.Count} item(s).");
        foreach (var evidence in evidenceRows)
        {
            bundle.AppendLine();
            bundle.Append($"[{evidence.EvidenceType}] {evidence.Title}");
            if (!string.IsNullOrWhiteSpace(evidence.Description))
            {
                bundle.Append($" — {evidence.Description.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(evidence.FileName) || !string.IsNullOrWhiteSpace(evidence.FileUrl))
            {
                bundle.Append($" (file: {evidence.FileName ?? "n/a"}, {evidence.FileUrl ?? "n/a"})");
            }
        }

        return bundle.ToString();
    }

    /// <summary>
    ///     Computes the frozen-manifest digest (issue #419 replay-safe submission): SHA-256 over
    ///     the ordered <c>id|providerFileId|digest</c> entries, so any added, removed or changed
    ///     evidence item produces a different hash.
    /// </summary>
    internal static string ComputeManifestHash(IEnumerable<(string EvidenceId, string? ProviderFileId, string Digest)> entries)
    {
        var canonical = string.Join(
            "\n",
            entries.Select(entry => $"{entry.EvidenceId}|{entry.ProviderFileId ?? string.Empty}|{entry.Digest}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private static string BuildManifestJson(
        IEnumerable<(DisputeEvidence Row, GatewayDisputeEvidenceItem Item, string Digest)> preparedItems) =>
        JsonSerializer.Serialize(
            preparedItems.Select(entry => new EvidenceManifestEntry(
                entry.Row.Id.ToString(),
                entry.Row.ProviderFileId,
                entry.Digest)));

    private sealed record EvidenceManifestEntry(string EvidenceId, string? ProviderFileId, string ContentDigest);

    private static DisputeActionResult Failure(Guid disputeId, string reason) => new()
    {
        Success = false,
        DisputeId = disputeId,
        FailureReason = reason
    };
}
