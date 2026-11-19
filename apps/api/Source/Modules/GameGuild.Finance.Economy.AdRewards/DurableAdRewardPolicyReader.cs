using System.Security.Cryptography;
using System.Text;
using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.AdRewards.Persistence;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.Finance.Economy.Risk;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Finance.Economy.AdRewards;

public sealed class PostgreSqlDurableAdRewardPolicyReader : IDurableAdRewardPolicyReader
{
    private readonly DbContext _db;
    private readonly ICapabilityPolicySignatureVerifier _signatureVerifier;

    public PostgreSqlDurableAdRewardPolicyReader(
        IApplicationDbContext context,
        ICapabilityPolicySignatureVerifier signatureVerifier)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(signatureVerifier);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "Durable ad reward policies require the application's relational DbContext.");
        _signatureVerifier = signatureVerifier;
    }

    public async ValueTask<AdRewardNetworkPolicySnapshot> GetEffectiveAsync(
        Guid tenantId,
        string network,
        DateTimeOffset at,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(tenantId, network);
        var normalizedNetwork = network.Trim();
        var row = await _db.Set<EconomyPolicyVersionRow>()
            .AsNoTracking()
            .Where(policy => policy.PolicyDomain == EconomyPolicyDomain.AdNetwork &&
                             policy.TenantId == tenantId &&
                             policy.SubjectKey == normalizedNetwork &&
                             policy.EffectiveAt <= at && at < policy.ExpiresAt)
            .OrderByDescending(policy => policy.Version)
            .FirstOrDefaultAsync(cancellationToken);
        return await ValidateAndMapAsync(row, tenantId, normalizedNetwork, cancellationToken);
    }

    public async ValueTask<AdRewardNetworkPolicySnapshot> GetVersionAsync(
        Guid tenantId,
        string network,
        PolicyVersion version,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(tenantId, network);
        var normalizedNetwork = network.Trim();
        var row = await _db.Set<EconomyPolicyVersionRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                policy => policy.PolicyDomain == EconomyPolicyDomain.AdNetwork &&
                          policy.TenantId == tenantId &&
                          policy.SubjectKey == normalizedNetwork &&
                          policy.Version == version.Value,
                cancellationToken);
        return await ValidateAndMapAsync(row, tenantId, normalizedNetwork, cancellationToken);
    }

    private async ValueTask<AdRewardNetworkPolicySnapshot> ValidateAndMapAsync(
        EconomyPolicyVersionRow? row,
        Guid tenantId,
        string network,
        CancellationToken cancellationToken)
    {
        if (row is null)
            throw new AdNetworkPolicyUnavailableException("No signed ad network policy is available.");
        var payloadHash = Convert.ToHexStringLower(
            SHA256.HashData(Encoding.UTF8.GetBytes(row.CanonicalPayload)));
        var signatureValid = string.Equals(payloadHash, row.PayloadHash, StringComparison.Ordinal) &&
                             await _signatureVerifier.VerifyAsync(
                                 row.CanonicalPayload, row.KeyId, row.Signature, cancellationToken);
        if (!signatureValid || row.ProposedBy == row.ApprovedBy || row.SubjectKey != network)
            throw new AdNetworkPolicyUnavailableException("The ad network policy is invalid or not effective.");

        var policy = new AdNetworkPolicy(
            row.SubjectKey!,
            new PolicyVersion(row.Version),
            row.EffectiveAt,
            row.ExpiresAt,
            (AdRewardIssuanceMode)row.IssuanceMode!.Value,
            (AdNetworkYieldState)row.YieldState!.Value,
            row.EstimatedNetEcpmUsdNanos!.Value,
            row.ContractedRevenueSharePpm!.Value,
            row.SafetyBufferPpm!.Value,
            row.MinimumVisiblePpm!.Value,
            TimeSpan.FromTicks(row.MaximumFocusLossTicks!.Value),
            row.MaximumRewardSoftUnits!.Value,
            row.ReportsCurrentThrough!.Value,
            TimeSpan.FromTicks(row.ReportStaleAfterTicks!.Value),
            row.Ranking!.Value);
        var budget = new AdRewardBudgetPolicy(
            row.MaximumUserSoftUnits!.Value,
            row.MaximumDeviceSoftUnits!.Value,
            row.MaximumNetworkSoftUnits!.Value,
            row.MaximumGlobalSoftUnits!.Value,
            row.FundedLossBudgetUsdNanos!.Value,
            TimeSpan.FromTicks(row.BudgetWindowTicks!.Value));
        return new AdRewardNetworkPolicySnapshot(
            tenantId,
            policy,
            budget,
            row.MaximumIpSoftUnits!.Value,
            row.MaximumAsnSoftUnits!.Value,
            row.ProviderHash!,
            row.ProviderCertified!.Value,
            row.PayloadHash,
            row.KeyId,
            row.Signature);
    }

    private static void ValidateScope(Guid tenantId, string network)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("Tenant ID is required.", nameof(tenantId));
        ArgumentException.ThrowIfNullOrWhiteSpace(network);
    }
}
