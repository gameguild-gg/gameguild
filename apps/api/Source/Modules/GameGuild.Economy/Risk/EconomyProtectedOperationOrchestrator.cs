using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Economy.Risk;

public sealed class EconomyProtectedOperationOrchestrator(
    IActorContextAccessor actorContextAccessor,
    IEconomyJurisdictionResolver jurisdictionResolver,
    IEconomyProtectedOperationRiskDecisionIssuer riskDecisionIssuer,
    IEconomyCapabilityAuthorizationService capabilityAuthorization,
    IEconomyProtectedOperationTransaction transaction) : IEconomyProtectedOperationOrchestrator
{
    public async Task<TResult> ExecuteAsync<TResult>(
        EconomyProtectedOperationIntent intent,
        Func<EconomyProtectedOperationAuthorization, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        Validate(intent);
        ArgumentNullException.ThrowIfNull(operation);
        cancellationToken.ThrowIfCancellationRequested();
        var actor = actorContextAccessor.ActorContext;
        if (!actor.IsAuthenticated || actor.TenantId is not { } tenantId ||
            actor.SubjectIdAsGuid is not { } actorId)
            throw new UnauthorizedAccessException(
                "A protected Economy operation requires an authenticated tenant actor.");
        var subjectId = intent.ProtectedSubjectId ?? actorId;

        var jurisdiction = await jurisdictionResolver.ResolveAsync(
            tenantId,
            subjectId,
            intent.ProviderJurisdictionCode,
            intent.DestinationJurisdictionCode,
            intent.RequestedAt,
            cancellationToken).ConfigureAwait(false);
        var fingerprint = Fingerprint(tenantId, actorId, intent);
        var subjectReference = EconomySubjectReference.ForUser(tenantId, subjectId);
        var execution = await transaction.ExecuteAsync(async token =>
        {
            var decision = await riskDecisionIssuer.IssueAsync(
                new EconomyProtectedRiskDecisionRequest(
                    tenantId,
                    actorId,
                    subjectReference,
                    jurisdiction.JurisdictionCode,
                    jurisdiction.EvidenceHash,
                    fingerprint,
                    intent),
                token).ConfigureAwait(false);
            if (decision.Outcome != RiskOutcome.Allow ||
                decision.State != EconomyProtectedOperationState.Ready)
                return ProtectedExecution<TResult>.Rejected(decision);
            if (decision.Id == Guid.Empty)
                throw new InvalidOperationException(
                    "A ready protected operation must have a durable risk decision.");

            var receipt = await capabilityAuthorization.AuthorizeAndConsumeAsync(
                new EconomyCapabilityEvaluationContext(
                    tenantId,
                    actorId,
                    subjectReference,
                    jurisdiction.JurisdictionCode,
                    intent.Capability,
                    decision.Id,
                    fingerprint,
                    intent.ProviderReferenceHash.Trim(),
                    intent.DestinationHash.Trim(),
                    intent.SourceRoots.Select(HashRoot).ToArray(),
                    intent.RequestedAt),
                token).ConfigureAwait(false);
            var authorization = new EconomyProtectedOperationAuthorization(
                tenantId,
                actorId,
                jurisdiction.JurisdictionCode,
                decision.Id,
                fingerprint,
                receipt);
            return ProtectedExecution<TResult>.Succeeded(
                await operation(authorization, token).ConfigureAwait(false),
                decision);
        }, cancellationToken).ConfigureAwait(false);

        if (!execution.Success)
            throw new EconomyProtectedOperationException(
                execution.Decision.State,
                execution.Decision.ReviewId,
                execution.Decision.Diagnostics);
        return execution.Result;
    }

    internal static string Fingerprint(
        Guid tenantId,
        Guid actorId,
        EconomyProtectedOperationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        var fields = new List<string>
        {
            tenantId.ToString("N"),
            actorId.ToString("N"),
            ((int)intent.Capability).ToString(CultureInfo.InvariantCulture),
            ((int)intent.TemplateKind).ToString(CultureInfo.InvariantCulture),
            intent.SourceWalletId.Value.ToString("N"),
            intent.DestinationWalletId.Value.ToString("N"),
            ((int)intent.Amount.Currency).ToString(CultureInfo.InvariantCulture),
            intent.Amount.Units.ToString(CultureInfo.InvariantCulture),
            string.Join(',', intent.CurrencyLegs.Select(leg =>
                $"{(int)leg.Currency}:{leg.Units.ToString(CultureInfo.InvariantCulture)}")),
            string.Join(',', intent.SourceRoots.Select(root => root.Value.ToString("N"))),
            intent.ProviderReferenceHash,
            intent.DestinationHash,
            intent.IdempotencyKey.Value,
            intent.RequestedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)
        };
        if (intent.ProtectedSubjectId is { } protectedSubjectId)
        {
            fields.Insert(2, "protected-subject-v1");
            fields.Insert(3, protectedSubjectId.ToString("N"));
        }
        var canonical = string.Concat(fields.Select(value =>
            $"{Encoding.UTF8.GetByteCount(value)}:{value}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string HashRoot(SourceStampId root) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(root.Value.ToString("N"))));

    private static void Validate(EconomyProtectedOperationIntent intent)
    {
        ArgumentNullException.ThrowIfNull(intent);
        if (!Enum.IsDefined(intent.Capability)) throw new ArgumentOutOfRangeException(nameof(intent));
        if (!Enum.IsDefined(intent.TemplateKind)) throw new ArgumentOutOfRangeException(nameof(intent));
        if (intent.SourceWalletId.Value == Guid.Empty || intent.DestinationWalletId.Value == Guid.Empty)
            throw new ArgumentException("Protected operation wallets are required.", nameof(intent));
        if (intent.ProtectedSubjectId == Guid.Empty)
            throw new ArgumentException("Protected operation subject IDs cannot be empty.", nameof(intent));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(intent.Amount.Units);
        ArgumentNullException.ThrowIfNull(intent.CurrencyLegs);
        if (intent.CurrencyLegs.Count == 0)
            throw new ArgumentException("Protected operation currency legs are required.", nameof(intent));
        ArgumentNullException.ThrowIfNull(intent.SourceRoots);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.ProviderReferenceHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(intent.DestinationHash);
    }

    private sealed record ProtectedExecution<TResult>(
        bool Success,
        TResult Result,
        EconomyProtectedRiskDecision Decision)
    {
        public static ProtectedExecution<TResult> Succeeded(
            TResult result,
            EconomyProtectedRiskDecision decision) => new(true, result, decision);

        public static ProtectedExecution<TResult> Rejected(
            EconomyProtectedRiskDecision decision) => new(false, default!, decision);
    }
}
