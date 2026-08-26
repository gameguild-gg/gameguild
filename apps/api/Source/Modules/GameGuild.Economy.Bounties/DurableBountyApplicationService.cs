using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Economy.Bounties.Persistence;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Risk;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Bounties;

public sealed record CreateDurableBountyRequest(
    Guid TenantId,
    Guid ActorId,
    CoinAmount Amount,
    BountyEligibilityRequirements Eligibility,
    DateTimeOffset ExpiresAt,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    IdempotencyKey IdempotencyKey,
    DateTimeOffset RequestedAt);

public sealed record ClaimDurableBountyRequest(
    Guid TenantId,
    Guid ActorId,
    BountyId BountyId,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    IdempotencyKey IdempotencyKey,
    DateTimeOffset RequestedAt);

public sealed record ReclaimDurableBountyRequest(
    Guid TenantId,
    Guid ActorId,
    BountyId BountyId,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    IdempotencyKey IdempotencyKey,
    DateTimeOffset RequestedAt);

public sealed record DurableBountyView(
    BountyId Id,
    Guid PosterId,
    CoinAmount Amount,
    BountyEligibilityRequirements Eligibility,
    int ReclaimFeePpm,
    BountyStatus Status,
    DateTimeOffset PostedAt,
    DateTimeOffset ExpiresAt,
    long Version,
    PersistedBountyTerminalEvent? TerminalEvent);

public interface IDurableBountyApplicationService
{
    ValueTask<DurableBountyView> CreateAsync(
        CreateDurableBountyRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DurableBountyView> ClaimAsync(
        ClaimDurableBountyRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DurableBountyView> ReclaimAsync(
        ReclaimDurableBountyRequest request,
        CancellationToken cancellationToken = default);

    ValueTask<DurableBountyView?> FindAsync(
        Guid tenantId,
        BountyId bountyId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<DurableBountyView>> ListAsync(
        Guid tenantId,
        BountyStatus? status,
        CancellationToken cancellationToken = default);
}

public sealed class DurableBountyApplicationService : IDurableBountyApplicationService
{
    private const string EscrowCapabilityName = "bounty-escrow";
    private const string ClaimCapabilityName = "bounty-claim";
    private const string ReclaimCapabilityName = "bounty-reclaim";
    private readonly DbContext _db;
    private readonly IEconomyWalletDirectory _wallets;
    private readonly IBountyPostableLotReader _lots;
    private readonly IBountyEscrowStore _escrows;
    private readonly IBountyTerminalEventStore _terminals;
    private readonly IEconomyCapabilityPolicyStore _policies;
    private readonly IEconomyCapabilityAuthorizationService _capabilities;
    private readonly IRegisteredPostingCapabilityResolver _postingAuthority;
    private readonly IDurableBountyEscrowPostWorkflow _posts;
    private readonly IDurableBountyClaimWorkflow _claims;
    private readonly IDurableBountyReclaimWorkflow _reclaims;

    public DurableBountyApplicationService(
        IApplicationDbContext context,
        IEconomyWalletDirectory wallets,
        IBountyPostableLotReader lots,
        IBountyEscrowStore escrows,
        IBountyTerminalEventStore terminals,
        IEconomyCapabilityPolicyStore policies,
        IEconomyCapabilityAuthorizationService capabilities,
        IRegisteredPostingCapabilityResolver postingAuthority,
        IDurableBountyEscrowPostWorkflow posts,
        IDurableBountyClaimWorkflow claims,
        IDurableBountyReclaimWorkflow reclaims)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext ?? throw new InvalidOperationException(
            "Durable Bounty application flows require the application's relational DbContext.");
        _wallets = wallets ?? throw new ArgumentNullException(nameof(wallets));
        _lots = lots ?? throw new ArgumentNullException(nameof(lots));
        _escrows = escrows ?? throw new ArgumentNullException(nameof(escrows));
        _terminals = terminals ?? throw new ArgumentNullException(nameof(terminals));
        _policies = policies ?? throw new ArgumentNullException(nameof(policies));
        _capabilities = capabilities ?? throw new ArgumentNullException(nameof(capabilities));
        _postingAuthority = postingAuthority ?? throw new ArgumentNullException(nameof(postingAuthority));
        _posts = posts ?? throw new ArgumentNullException(nameof(posts));
        _claims = claims ?? throw new ArgumentNullException(nameof(claims));
        _reclaims = reclaims ?? throw new ArgumentNullException(nameof(reclaims));
    }

    public async ValueTask<DurableBountyView> CreateAsync(
        CreateDurableBountyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCommon(request.TenantId, request.ActorId, request.JurisdictionCode,
            request.RiskDecisionId, request.OperationFingerprint);
        ArgumentNullException.ThrowIfNull(request.Eligibility);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Amount.Units);
        var policy = await RequiredPolicyAsync(
            request.TenantId, EconomyValueMovementCapability.BountyEscrow,
            request.JurisdictionCode, cancellationToken);
        var settings = ParseEscrowPolicy(policy);
        var lifetime = request.ExpiresAt - request.RequestedAt;
        if (lifetime < settings.MinimumLifetime || lifetime > settings.MaximumLifetime)
            throw new BountyPolicyUnavailableException("Bounty lifetime is outside the signed policy window.");

        var posterWallet = await _wallets.GetOwnerWalletAsync(
            request.TenantId, request.ActorId, cancellationToken);
        var escrowWallet = await _wallets.GetWalletAsync(
            request.TenantId, settings.EscrowWalletId, cancellationToken);
        if (posterWallet.WalletId == escrowWallet.WalletId)
            throw new BountyPolicyUnavailableException("The signed bounty escrow wallet cannot be the poster wallet.");

        var bountyId = DeterministicBountyId(request.TenantId, request.IdempotencyKey);
        var preview = BountyEscrowPositionFactory.Create(new PostBountyCommand(
            bountyId,
            request.ActorId,
            posterWallet.WalletId,
            escrowWallet.WalletId,
            request.Amount,
            _lots.Read(posterWallet.WalletId, request.Amount.Currency, request.RequestedAt),
            request.Eligibility,
            settings.ReclaimFeePpm,
            request.RequestedAt,
            request.ExpiresAt,
            request.IdempotencyKey));
        var receipt = await AuthorizeAsync(
            request.TenantId,
            request.ActorId,
            request.JurisdictionCode,
            EconomyValueMovementCapability.BountyEscrow,
            request.RiskDecisionId,
            request.OperationFingerprint,
            policy.PayloadHash,
            Hash(escrowWallet.WalletId.Value.ToString("N")),
            RootHashes(preview.EscrowFragments),
            request.RequestedAt,
            cancellationToken);
        var authority = await _postingAuthority.ResolveAuthorityAsync(
            EscrowCapabilityName, PostingTemplateKind.BountyEscrow, receipt, cancellationToken);
        var requestHash = Hash(string.Join('|',
            request.TenantId.ToString("N"), request.ActorId.ToString("N"), bountyId.Value.ToString("N"),
            (int)request.Amount.Currency, request.Amount.Units, request.ExpiresAt.UtcTicks,
            settings.ReclaimFeePpm, policy.PayloadHash, request.IdempotencyKey.Value));
        var persisted = await _posts.PostAsync(new DurableBountyEscrowPostRequest(
            bountyId,
            request.ActorId,
            posterWallet.WalletId,
            escrowWallet.WalletId,
            request.Amount,
            request.Eligibility,
            settings.ReclaimFeePpm,
            request.RequestedAt,
            request.ExpiresAt,
            request.IdempotencyKey,
            requestHash,
            authority,
            new ReserveVersion(receipt.ReserveVersion),
            new PolicyVersion(receipt.PolicyVersion),
            receipt.ReceiptHash), cancellationToken);
        return Map(persisted, null);
    }

    public async ValueTask<DurableBountyView> ClaimAsync(
        ClaimDurableBountyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCommon(request.TenantId, request.ActorId, request.JurisdictionCode,
            request.RiskDecisionId, request.OperationFingerprint);
        var escrow = _escrows.Get(request.TenantId, request.BountyId);
        var policy = await RequiredPolicyAsync(
            request.TenantId, EconomyValueMovementCapability.BountyClaim,
            request.JurisdictionCode, cancellationToken);
        var claimantWallet = await _wallets.GetOwnerWalletAsync(
            request.TenantId, request.ActorId, cancellationToken);
        var receipt = await AuthorizeAsync(
            request.TenantId,
            request.ActorId,
            request.JurisdictionCode,
            EconomyValueMovementCapability.BountyClaim,
            request.RiskDecisionId,
            request.OperationFingerprint,
            policy.PayloadHash,
            Hash(claimantWallet.WalletId.Value.ToString("N")),
            RootHashes(escrow.Fragments),
            request.RequestedAt,
            cancellationToken);
        var authority = await _postingAuthority.ResolveAuthorityAsync(
            ClaimCapabilityName, PostingTemplateKind.BountyClaim, receipt, cancellationToken);
        var terminal = await _claims.ClaimAsync(new DurableBountyClaimRequest(
            request.BountyId,
            request.ActorId,
            claimantWallet.WalletId,
            request.RequestedAt,
            request.IdempotencyKey,
            receipt.ReceiptHash,
            authority,
            new ReserveVersion(receipt.ReserveVersion),
            new PolicyVersion(receipt.PolicyVersion),
            receipt.ReceiptHash), cancellationToken);
        return Map(_escrows.Get(request.TenantId, request.BountyId), terminal);
    }

    public async ValueTask<DurableBountyView> ReclaimAsync(
        ReclaimDurableBountyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateCommon(request.TenantId, request.ActorId, request.JurisdictionCode,
            request.RiskDecisionId, request.OperationFingerprint);
        var escrow = _escrows.Get(request.TenantId, request.BountyId);
        var policy = await RequiredPolicyAsync(
            request.TenantId, EconomyValueMovementCapability.BountyReclaim,
            request.JurisdictionCode, cancellationToken);
        var posterWallet = await _wallets.GetOwnerWalletAsync(
            request.TenantId, request.ActorId, cancellationToken);
        var receipt = await AuthorizeAsync(
            request.TenantId,
            request.ActorId,
            request.JurisdictionCode,
            EconomyValueMovementCapability.BountyReclaim,
            request.RiskDecisionId,
            request.OperationFingerprint,
            policy.PayloadHash,
            Hash(posterWallet.WalletId.Value.ToString("N")),
            RootHashes(escrow.Fragments),
            request.RequestedAt,
            cancellationToken);
        var authority = await _postingAuthority.ResolveAuthorityAsync(
            ReclaimCapabilityName, PostingTemplateKind.BountyReclaim, receipt, cancellationToken);
        var terminal = await _reclaims.ReclaimAsync(new DurableBountyReclaimRequest(
            request.BountyId,
            request.ActorId,
            posterWallet.WalletId,
            request.RequestedAt,
            request.IdempotencyKey,
            authority,
            new ReserveVersion(receipt.ReserveVersion),
            new PolicyVersion(receipt.PolicyVersion),
            receipt.ReceiptHash), cancellationToken);
        return Map(_escrows.Get(request.TenantId, request.BountyId), terminal);
    }

    public async ValueTask<DurableBountyView?> FindAsync(
        Guid tenantId,
        BountyId bountyId,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        var exists = await _db.Set<BountyRow>().AsNoTracking()
            .AnyAsync(row => row.TenantId == tenantId && row.Id == bountyId.Value, cancellationToken);
        if (!exists) return null;
        return Map(_escrows.Get(tenantId, bountyId), _terminals.FindByBounty(tenantId, bountyId));
    }

    public async ValueTask<IReadOnlyList<DurableBountyView>> ListAsync(
        Guid tenantId,
        BountyStatus? status,
        CancellationToken cancellationToken = default)
    {
        ValidateTenant(tenantId);
        var query = _db.Set<BountyRow>().AsNoTracking().Where(row => row.TenantId == tenantId);
        if (status.HasValue) query = query.Where(row => row.Status == status.Value);
        var ids = await query.OrderByDescending(row => row.PostedAt).ThenBy(row => row.Id)
            .Select(row => row.Id).ToArrayAsync(cancellationToken);
        return ids.Select(id =>
        {
            var bountyId = new BountyId(id);
            return Map(_escrows.Get(tenantId, bountyId), _terminals.FindByBounty(tenantId, bountyId));
        }).ToArray();
    }

    private async ValueTask<CapabilityAuthorizationReceipt> AuthorizeAsync(
        Guid tenantId,
        Guid actorId,
        string jurisdictionCode,
        EconomyValueMovementCapability capability,
        Guid riskDecisionId,
        string operationFingerprint,
        string providerHash,
        string destinationHash,
        IReadOnlyList<string> sourceRootHashes,
        DateTimeOffset evaluatedAt,
        CancellationToken cancellationToken) =>
        await _capabilities.AuthorizeAndConsumeAsync(new EconomyCapabilityEvaluationContext(
            tenantId,
            actorId,
            EconomySubjectReference.ForUser(tenantId, actorId),
            jurisdictionCode.Trim().ToUpperInvariant(),
            capability,
            riskDecisionId,
            operationFingerprint.Trim(),
            providerHash,
            destinationHash,
            sourceRootHashes,
            evaluatedAt), cancellationToken);

    private async ValueTask<EconomyCapabilityPolicy> RequiredPolicyAsync(
        Guid tenantId,
        EconomyValueMovementCapability capability,
        string jurisdictionCode,
        CancellationToken cancellationToken)
    {
        var policy = await _policies.CurrentAsync(
            tenantId, capability, jurisdictionCode.Trim().ToUpperInvariant(), cancellationToken);
        return policy is { State: EconomyCapabilityPolicyState.Active }
            ? policy
            : throw new BountyPolicyUnavailableException(
                $"No active signed policy is available for {capability}.");
    }

    private static BountyEscrowPolicySettings ParseEscrowPolicy(EconomyCapabilityPolicy policy)
    {
        try
        {
            using var document = JsonDocument.Parse(policy.CanonicalPayload);
            var root = document.RootElement;
            var escrowWalletId = root.GetProperty("escrowWalletId").GetGuid();
            var reclaimFeePpm = root.GetProperty("reclaimFeePpm").GetInt32();
            var minimumLifetimeSeconds = root.GetProperty("minimumLifetimeSeconds").GetInt64();
            var maximumLifetimeSeconds = root.GetProperty("maximumLifetimeSeconds").GetInt64();
            if (escrowWalletId == Guid.Empty || reclaimFeePpm is < 0 or >= 1_000_000 ||
                minimumLifetimeSeconds <= 0 || maximumLifetimeSeconds < minimumLifetimeSeconds)
                throw new BountyPolicyUnavailableException("The active bounty escrow policy is invalid.");
            return new BountyEscrowPolicySettings(
                new WalletId(escrowWalletId),
                reclaimFeePpm,
                TimeSpan.FromSeconds(minimumLifetimeSeconds),
                TimeSpan.FromSeconds(maximumLifetimeSeconds));
        }
        catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException or
                                           FormatException or OverflowException or JsonException)
        {
            throw new BountyPolicyUnavailableException(
                "The active bounty escrow policy payload is invalid.", exception);
        }
    }

    private static IReadOnlyList<string> RootHashes(IEnumerable<PersistedBountyEscrowFragment> fragments) =>
        fragments.SelectMany(fragment => fragment.SelectedRanges)
            .Select(range => Hash(range.Root.Value.ToString("N")))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> RootHashes(IEnumerable<BountyEscrowFragment> fragments) =>
        fragments.SelectMany(fragment => fragment.SelectedRanges)
            .Select(range => Hash(range.Root.Value.ToString("N")))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static DurableBountyView Map(
        PersistedBountyEscrow escrow,
        PersistedBountyTerminalEvent? terminal) => new(
        escrow.Id,
        escrow.PosterId,
        escrow.Amount,
        escrow.Eligibility,
        escrow.ReclaimFeePpm,
        terminal?.Status ?? escrow.Status,
        escrow.PostedAt,
        escrow.ExpiresAt,
        escrow.Version,
        terminal);

    private static BountyId DeterministicBountyId(Guid tenantId, IdempotencyKey idempotencyKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(
            $"economy-bounty-v1|{tenantId:N}|{idempotencyKey.Value}"));
        return new BountyId(new Guid(bytes.AsSpan(0, 16)));
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static void ValidateCommon(
        Guid tenantId,
        Guid actorId,
        string jurisdictionCode,
        Guid riskDecisionId,
        string operationFingerprint)
    {
        ValidateTenant(tenantId);
        if (actorId == Guid.Empty || riskDecisionId == Guid.Empty)
            throw new ArgumentException("Actor and risk decision IDs are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationFingerprint);
    }

    private static void ValidateTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A non-quarantine tenant ID is required.", nameof(tenantId));
    }

    private sealed record BountyEscrowPolicySettings(
        WalletId EscrowWalletId,
        int ReclaimFeePpm,
        TimeSpan MinimumLifetime,
        TimeSpan MaximumLifetime);
}

public sealed class BountyPolicyUnavailableException : InvalidOperationException
{
    public BountyPolicyUnavailableException(string message) : base(message) { }
    public BountyPolicyUnavailableException(string message, Exception innerException) : base(message, innerException) { }
}
