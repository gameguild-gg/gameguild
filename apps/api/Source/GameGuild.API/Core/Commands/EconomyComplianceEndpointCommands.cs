using GameGuild.API.Authorization;
using GameGuild.Compliance.FinancialCrime;
using GameGuild.Compliance.KYC;
using GameGuild.CQRS;
using GameGuild.Finance.Economy.Risk;
using GameGuild.Identity.Authorization;
using GameGuild.TrustSafety;

namespace GameGuild.API.Controllers;

public sealed record AssignFinancialCrimeCaseEndpointCommand(
    Guid TenantId, Guid CaseId, Guid ActorId, long ExpectedVersion, DateTimeOffset Now)
    : ICommand<FinancialCrimeCase>;
public sealed record DecideFinancialCrimeCaseEndpointCommand(
    FinancialCrimeCaseDecision Decision, long ExpectedCaseVersion)
    : ICommand<FinancialCrimeCaseDecision>;
public sealed record RecordRegulatoryReferenceEndpointCommand(
    Guid TenantId, Guid CaseId, string Kind, string JurisdictionCode, string ReferenceHash, Guid ActorId, DateTimeOffset Now)
    : ICommand;

// Trust-safety appeals T3 (docs/trustsafety/06-gaps.md): the appeal routes were reachable
// with a bare [Authorize] controller attribute only — the in-handler TryActor check is not
// a capability gate at the dispatch layer. The same OperateCompliance capability the rest of
// this controller enforces is now required at the CQRS pipeline as well (defense in depth:
// [AuthorizeRequest] fails closed even if a future caller forgets the controller check).
[AuthorizeRequest(EconomyPermission.Keys.OperateCompliance)]
public sealed record AssignTrustSafetyAppealEndpointCommand(
    Guid TenantId, Guid AppealId, Guid ActorId, long ExpectedVersion, DateTimeOffset Now)
    : ICommand<TrustSafetyAppeal>;
[AuthorizeRequest(EconomyPermission.Keys.OperateCompliance)]
public sealed record DecideTrustSafetyAppealEndpointCommand(
    Guid TenantId, Guid AppealId, Guid ActorId, long ExpectedVersion, bool Overturn, string ReasonCode, string EvidenceHash, DateTimeOffset Now)
    : ICommand<TrustSafetyAppeal>;
public sealed record SubmitTrustSafetyAppealEndpointCommand(
    Guid TenantId, Guid ActorId, string SubjectHash, string RestrictionReferenceHash,
    string SubmissionEvidenceHash, DateTimeOffset Now)
    : ICommand<TrustSafetyAppeal>;

public sealed record ProposeComplianceHoldReleaseEndpointCommand(
    EconomyStepUpOperation Operation, string StepUpReceipt, Guid TenantId, Guid HoldId, Guid ActorId, DateTimeOffset Now)
    : ICommand<ComplianceHoldAdministrationState>;
public sealed record ApproveComplianceHoldReleaseEndpointCommand(
    EconomyStepUpOperation Operation, string StepUpReceipt, Guid TenantId, Guid HoldId, Guid ActorId, DateTimeOffset Now)
    : ICommand<ComplianceHoldAdministrationState>;

public sealed record ApproveEconomyRiskReviewEndpointCommand(
    Guid TenantId, Guid ReviewId, Guid ActorId, RiskManualDecisionCode DecisionCode, string Resolution, DateTimeOffset Now)
    : ICommand<RiskReviewCase>;
public sealed record RejectEconomyRiskReviewEndpointCommand(
    Guid TenantId, Guid ReviewId, Guid ActorId, RiskManualDecisionCode DecisionCode, string Resolution, DateTimeOffset Now)
    : ICommand<RiskReviewCase>;

public sealed record StartKycOnboardingEndpointCommand(StartKycAmlRequest Request) : ICommand<KycAmlOnboarding>;
public sealed record CreateKycAccessTokenEndpointCommand(
    Guid TenantId, string Subject, int LifetimeSeconds)
    : ICommand<KycAmlAccessToken>;
public sealed record IngestSumSubWebhookEndpointCommand(
    byte[] Payload, string Digest, string Algorithm, DateTimeOffset IssuedAt, DateTimeOffset Now)
    : ICommand<SumSubWebhookIngestionResult>;

public sealed class EconomyComplianceEndpointCommandHandler(
    IFinancialCrimeControlPlane financialCrime,
    ITrustSafetyControlPlane trustSafety,
    IComplianceHoldAdministrationStore holds,
    IEconomyStepUpExecutor stepUp,
    IRiskReviewStore riskReviews,
    IKycAmlOrchestrator kyc,
    IFinancialCrimeSignalQueue financialCrimeSignals,
    Microsoft.Extensions.Logging.ILogger<EconomyComplianceEndpointCommandHandler>? logger = null) :
    ICommandHandler<AssignFinancialCrimeCaseEndpointCommand, FinancialCrimeCase>,
    ICommandHandler<DecideFinancialCrimeCaseEndpointCommand, FinancialCrimeCaseDecision>,
    ICommandHandler<RecordRegulatoryReferenceEndpointCommand>,
    ICommandHandler<AssignTrustSafetyAppealEndpointCommand, TrustSafetyAppeal>,
    ICommandHandler<DecideTrustSafetyAppealEndpointCommand, TrustSafetyAppeal>,
    ICommandHandler<SubmitTrustSafetyAppealEndpointCommand, TrustSafetyAppeal>,
    ICommandHandler<ProposeComplianceHoldReleaseEndpointCommand, ComplianceHoldAdministrationState>,
    ICommandHandler<ApproveComplianceHoldReleaseEndpointCommand, ComplianceHoldAdministrationState>,
    ICommandHandler<ApproveEconomyRiskReviewEndpointCommand, RiskReviewCase>,
    ICommandHandler<RejectEconomyRiskReviewEndpointCommand, RiskReviewCase>,
    ICommandHandler<StartKycOnboardingEndpointCommand, KycAmlOnboarding>,
    ICommandHandler<CreateKycAccessTokenEndpointCommand, KycAmlAccessToken>,
    ICommandHandler<IngestSumSubWebhookEndpointCommand, SumSubWebhookIngestionResult>
{
    public async Task<FinancialCrimeCase> Handle(AssignFinancialCrimeCaseEndpointCommand request, CancellationToken ct) =>
        await financialCrime.AssignCaseAsync(
            request.TenantId, request.CaseId, request.ActorId, request.ExpectedVersion, request.Now, ct).ConfigureAwait(false);

    public async Task<FinancialCrimeCaseDecision> Handle(DecideFinancialCrimeCaseEndpointCommand request, CancellationToken ct) =>
        await financialCrime.DecideCaseAsync(request.Decision, request.ExpectedCaseVersion, ct).ConfigureAwait(false);

    public async Task<Unit> Handle(RecordRegulatoryReferenceEndpointCommand request, CancellationToken ct)
    {
        await financialCrime.RecordRegulatoryReferenceAsync(
            request.TenantId, request.CaseId, request.Kind, request.JurisdictionCode,
            request.ReferenceHash, request.ActorId, request.Now, ct).ConfigureAwait(false);
        return Unit.Value;
    }

    public async Task<TrustSafetyAppeal> Handle(AssignTrustSafetyAppealEndpointCommand request, CancellationToken ct) =>
        await trustSafety.AssignAppealAsync(
            request.TenantId, request.AppealId, request.ActorId, request.ExpectedVersion, request.Now, ct).ConfigureAwait(false);

    public async Task<TrustSafetyAppeal> Handle(SubmitTrustSafetyAppealEndpointCommand request, CancellationToken ct)
    {
        // Issue #1319 T1: SubmitAppealAsync was caller-less — the appeals console managed an
        // eternally-empty table. New appeals enter the workflow through this command (state
        // Submitted, version 1, unassigned); idempotent replays return the persisted row.
        var appeal = new TrustSafetyAppeal(
            Id: Guid.NewGuid(),
            TenantId: request.TenantId,
            SubjectHash: request.SubjectHash.Trim(),
            RestrictionReferenceHash: request.RestrictionReferenceHash.Trim(),
            State: TrustSafetyAppealState.Submitted,
            SubmittedBy: request.ActorId,
            AssignedTo: null,
            DecidedBy: null,
            SubmissionEvidenceHash: request.SubmissionEvidenceHash.Trim(),
            DecisionEvidenceHash: null,
            ReasonCode: null,
            SubmittedAt: request.Now,
            DecidedAt: null,
            Version: 1);
        return await trustSafety.SubmitAppealAsync(appeal, ct).ConfigureAwait(false);
    }

    public async Task<TrustSafetyAppeal> Handle(DecideTrustSafetyAppealEndpointCommand request, CancellationToken ct) =>
        await trustSafety.DecideAppealAsync(
            request.TenantId, request.AppealId, request.ActorId, request.ExpectedVersion, request.Overturn,
            request.ReasonCode, request.EvidenceHash, request.Now, ct).ConfigureAwait(false);

    public async Task<ComplianceHoldAdministrationState> Handle(ProposeComplianceHoldReleaseEndpointCommand request, CancellationToken ct) =>
        await stepUp.ExecuteAsync(
            request.Operation,
            request.StepUpReceipt,
            (evidenceHash, token) => holds.ProposeReleaseAsync(
                request.TenantId, request.HoldId, request.ActorId, evidenceHash, request.Now, token).AsTask(),
            ct).ConfigureAwait(false);

    public async Task<ComplianceHoldAdministrationState> Handle(ApproveComplianceHoldReleaseEndpointCommand request, CancellationToken ct) =>
        await stepUp.ExecuteAsync(
            request.Operation,
            request.StepUpReceipt,
            (evidenceHash, token) => holds.ApproveReleaseAsync(
                request.TenantId, request.HoldId, request.ActorId, evidenceHash, request.Now, token).AsTask(),
            ct).ConfigureAwait(false);

    public async Task<RiskReviewCase> Handle(ApproveEconomyRiskReviewEndpointCommand request, CancellationToken ct) =>
        await riskReviews.ApproveAsync(
            request.TenantId, request.ReviewId, request.ActorId, request.DecisionCode, request.Resolution, request.Now, ct).ConfigureAwait(false);

    public async Task<RiskReviewCase> Handle(RejectEconomyRiskReviewEndpointCommand request, CancellationToken ct) =>
        await riskReviews.RejectAsync(
            request.TenantId, request.ReviewId, request.ActorId, request.DecisionCode, request.Resolution, request.Now, ct).ConfigureAwait(false);

    public async Task<KycAmlOnboarding> Handle(StartKycOnboardingEndpointCommand request, CancellationToken ct) =>
        await kyc.StartAsync(request.Request, ct).ConfigureAwait(false);

    public async Task<KycAmlAccessToken> Handle(CreateKycAccessTokenEndpointCommand request, CancellationToken ct) =>
        await kyc.CreateAccessTokenAsync(request.TenantId, request.Subject, request.LifetimeSeconds, ct).ConfigureAwait(false);

    public async Task<SumSubWebhookIngestionResult> Handle(IngestSumSubWebhookEndpointCommand request, CancellationToken ct)
    {
        var result = await kyc.IngestWebhookAsync(
            request.Payload, request.Digest, request.Algorithm, request.IssuedAt, request.Now, ct).ConfigureAwait(false);
        // Issue #1291 option C — KYC enrichment: every published (or replayed) SumSub decision
        // for a known applicant feeds the AML risk profile alongside the economy signals. The
        // enqueue is a single durable row on the FinancialCrime module's own outbox; the AML
        // ingestion worker turns it into identity-context evidence asynchronously. A failed
        // enqueue must not fail the webhook (the KYC ledger is already committed and the
        // provider would only see a duplicate on retry) — it is logged and picked up by
        // reconciliation instead.
        if (result.Status is KycEvidenceIngestionStatus.Published or KycEvidenceIngestionStatus.Duplicate &&
            result.TenantId is { } tenantId && !string.IsNullOrWhiteSpace(result.SubjectHash))
        {
            try
            {
                await financialCrimeSignals.EnqueueKycDecisionAsync(new FinancialCrimeKycDecisionSignal(
                    tenantId,
                    result.SubjectHash,
                    ExtractApplicantId(request.Payload),
                    result.State.ToString(),
                    result.ReviewAnswer,
                    result.ProviderEventId,
                    result.PayloadHash ?? string.Empty,
                    // The provider-issued timestamp keeps the signal canonical across webhook
                    // replays, so the outbox dedupes instead of double-enqueueing.
                    request.IssuedAt), ct).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger?.LogWarning(exception,
                    "Financial Crime KYC enrichment enqueue failed for SumSub event {ProviderEventId}; the KYC ingestion itself was not affected.",
                    result.ProviderEventId);
            }
        }

        return result;
    }

    private static string ExtractApplicantId(byte[] payload)
    {
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(payload);
            return document.RootElement.TryGetProperty("applicantId", out var applicantId) &&
                   applicantId.ValueKind == System.Text.Json.JsonValueKind.String
                ? applicantId.GetString() ?? "unknown"
                : "unknown";
        }
        catch (System.Text.Json.JsonException)
        {
            return "unknown";
        }
    }
}
