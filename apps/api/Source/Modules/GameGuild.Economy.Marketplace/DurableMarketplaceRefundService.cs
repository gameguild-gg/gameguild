using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Marketplace.Persistence;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Marketplace;

public enum MarketplaceRefundAuthority
{
    SelfService = 1,
    Operations = 2
}

public sealed record RefundAuthoritativeMarketplaceOrderRequest(
    Guid TenantId,
    Guid ActorId,
    MarketplaceRefundAuthority Authority,
    Guid SettlementId,
    int Quantity,
    string ReasonCode,
    string SubjectReference,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    IdempotencyKey IdempotencyKey,
    DateTimeOffset RefundedAt);

public sealed record DurableMarketplaceRefundDebt(
    WalletId ResponsibleWalletId,
    CurrencyCode Currency,
    long Units,
    string EvidenceHash);

public sealed record DurableMarketplaceRefundResult(
    Guid RefundId,
    Guid SettlementId,
    int Quantity,
    int CumulativeRefundedQuantity,
    MarketplaceSettlementStatus SettlementStatus,
    MarketplaceEntitlementStatus EntitlementStatus,
    IReadOnlyList<CoinAmount> Legs,
    IReadOnlyList<DurableMarketplaceRefundDebt> Debts,
    PostingId PostingId,
    long JournalSequence,
    string JournalHash,
    bool IsDuplicate,
    DateTimeOffset RefundedAt);

public interface IDurableMarketplaceRefundService
{
    ValueTask<DurableMarketplaceRefundResult> RefundAsync(
        RefundAuthoritativeMarketplaceOrderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DurableMarketplaceRefundService : IDurableMarketplaceRefundService
{
    private const string RegisteredCapabilityName = "marketplace-refund";
    private readonly DbContext _db;
    private readonly IDurableMarketplacePolicyReader _policies;
    private readonly IEconomyCapabilityAuthorizationService _capabilities;
    private readonly IRegisteredPostingCapabilityResolver _postingAuthority;
    private readonly IMarketplaceRefundLedgerGateway _ledger;

    public DurableMarketplaceRefundService(
        IApplicationDbContext context,
        IDurableMarketplacePolicyReader policies,
        IEconomyCapabilityAuthorizationService capabilities,
        IRegisteredPostingCapabilityResolver postingAuthority,
        IMarketplaceRefundLedgerGateway ledger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(postingAuthority);
        ArgumentNullException.ThrowIfNull(ledger);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "Durable Marketplace refunds require the application's relational DbContext.");
        _policies = policies;
        _capabilities = capabilities;
        _postingAuthority = postingAuthority;
        _ledger = ledger;
    }

    public async ValueTask<DurableMarketplaceRefundResult> RefundAsync(
        RefundAuthoritativeMarketplaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var idempotencyHash = Hash(request.IdempotencyKey.Value);
        var reasonCode = request.ReasonCode.Trim().ToUpperInvariant();
        var reasonHash = Hash(reasonCode);
        return await PostgreSqlTransactionExecutor.ExecuteAsync(
            _db, IsolationLevel.Serializable, async _ =>
        {

        var duplicate = await _db.Set<MarketplaceRefundRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.TenantId == request.TenantId &&
                                         row.IdempotencyKey == idempotencyHash,
                cancellationToken);
        if (duplicate is not null)
        {
            if (duplicate.SettlementId != request.SettlementId ||
                duplicate.Quantity != request.Quantity ||
                !string.Equals(duplicate.ReasonHash, reasonHash, StringComparison.Ordinal))
                throw new MarketplaceIdempotencyConflictException(
                    "The idempotency key is already bound to another Marketplace refund.");
            return await MapAsync(duplicate, true, cancellationToken);
        }

        var settlement = await _db.Set<MarketplaceSettlementRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.TenantId == request.TenantId &&
                                         row.Id == request.SettlementId,
                cancellationToken)
            ?? throw new MarketplaceRefundException("The Marketplace settlement was not found.");
        if (request.Authority == MarketplaceRefundAuthority.SelfService &&
            settlement.BuyerId != request.ActorId)
            throw new MarketplaceRefundException(
                "Only the authoritative order buyer can request a self-service refund.");
        if (settlement.Status == MarketplaceSettlementStatus.Refunded ||
            settlement.RefundedQuantity >= settlement.Quantity)
            throw new MarketplaceAlreadyRefundedException(
                "The Marketplace settlement has already been refunded in full.");
        var cumulativeQuantity = checked(settlement.RefundedQuantity + request.Quantity);
        if (cumulativeQuantity > settlement.Quantity)
            throw new MarketplaceRefundException(
                "The requested quantity exceeds the refundable order quantity.");

        var policy = await _policies.GetVersionAsync(
            request.TenantId, settlement.ProductId, settlement.PolicyVersion, cancellationToken);
        if (policy.Policy.SellerId != settlement.SellerId ||
            policy.PlatformFeeWalletId != settlement.PlatformFeeWalletId)
            throw new MarketplaceRefundException(
                "The historical signed policy does not match the persisted settlement.");

        var persistedLegs = await _db.Set<MarketplaceSettlementLegRow>()
            .AsNoTracking()
            .Where(row => row.SettlementId == settlement.Id)
            .OrderBy(row => row.Currency)
            .ToArrayAsync(cancellationToken);
        if (persistedLegs.Length == 0)
            throw new MarketplaceRefundException("The settlement has no durable price legs.");
        var refundLegs = persistedLegs.Select(leg =>
        {
            var cumulativeTarget = checked((long)decimal.Truncate(
                (decimal)leg.Units * cumulativeQuantity / settlement.Quantity));
            if (cumulativeTarget < leg.RefundedUnits || cumulativeTarget > leg.Units)
                throw new MarketplaceRefundException(
                    "The persisted Marketplace refund counters are inconsistent.");
            return new PersistedMarketplaceRefundLeg(
                leg.Currency, checked(cumulativeTarget - leg.RefundedUnits));
        }).Where(leg => leg.Units > 0).ToArray();
        if (refundLegs.Length == 0)
            throw new MarketplaceRefundException(
                "The requested quantity does not produce a positive refundable amount.");

        var rootPayloads = await _db.Set<MarketplaceFundingFragmentRow>()
            .AsNoTracking()
            .Where(row => row.SettlementId == settlement.Id)
            .Select(row => row.SelectedRootRanges)
            .ToArrayAsync(cancellationToken);
        var sourceRootHashes = ExtractSourceRootHashes(rootPayloads);
        var receipt = await _capabilities.AuthorizeAndConsumeAsync(
            new EconomyCapabilityEvaluationContext(
                request.TenantId,
                request.ActorId,
                request.SubjectReference.Trim(),
                request.JurisdictionCode.Trim().ToUpperInvariant(),
                EconomyValueMovementCapability.MarketplaceRefund,
                request.RiskDecisionId,
                request.OperationFingerprint.Trim(),
                policy.PayloadHash,
                Hash(settlement.BuyerWalletId.ToString("N")),
                sourceRootHashes,
                request.RefundedAt),
            cancellationToken);
        var authority = await _postingAuthority.ResolveAuthorityAsync(
            RegisteredCapabilityName,
            PostingTemplateKind.MarketplaceRefund,
            receipt,
            cancellationToken);
        var refundId = DeterministicId(settlement.Id, idempotencyHash);
        var postingId = new PostingId(DeterministicId(refundId, "posting"));
        var posting = _ledger.Refund(new PersistedMarketplaceRefundRequest(
            authority,
            receipt,
            refundId,
            settlement.Id,
            postingId,
            new IdempotencyKey(idempotencyHash),
            settlement.BuyerId,
            settlement.PolicyVersion,
            request.Quantity,
            cumulativeQuantity,
            refundLegs,
            reasonCode,
            reasonHash,
            request.RefundedAt));
        if (posting.PostingId != postingId)
            throw new RegisteredPostingRejectedException(
                "The Marketplace writer returned an unexpected refund posting identity.");

        var persisted = await _db.Set<MarketplaceRefundRow>()
            .AsNoTracking()
            .SingleAsync(row => row.Id == refundId, cancellationToken);
        return await MapAsync(persisted, posting.IsDuplicate, cancellationToken);
        }, cancellationToken);
    }

    private async ValueTask<DurableMarketplaceRefundResult> MapAsync(
        MarketplaceRefundRow refund,
        bool duplicate,
        CancellationToken cancellationToken)
    {
        var settlement = await _db.Set<MarketplaceSettlementRow>()
            .AsNoTracking()
            .SingleAsync(row => row.Id == refund.SettlementId, cancellationToken);
        var legs = await _db.Set<MarketplaceRefundLegRow>()
            .AsNoTracking()
            .Where(row => row.RefundId == refund.Id)
            .OrderBy(row => row.Currency)
            .Select(row => new CoinAmount(row.Currency, row.Units))
            .ToArrayAsync(cancellationToken);
        var debts = await _db.Set<MarketplaceRefundDebtRow>()
            .AsNoTracking()
            .Where(row => row.RefundId == refund.Id)
            .OrderBy(row => row.Currency)
            .ThenBy(row => row.ResponsibleWalletId)
            .Select(row => new DurableMarketplaceRefundDebt(
                new WalletId(row.ResponsibleWalletId), row.Currency, row.AmountUnits, row.EvidenceHash))
            .ToArrayAsync(cancellationToken);
        return new DurableMarketplaceRefundResult(
            refund.Id,
            refund.SettlementId,
            refund.Quantity,
            refund.RefundedQuantity,
            settlement.Status,
            settlement.EntitlementStatus,
            Array.AsReadOnly(legs),
            Array.AsReadOnly(debts),
            new PostingId(refund.PostingId),
            refund.FirstJournalSequence,
            refund.JournalHash,
            duplicate,
            refund.RefundedAt);
    }

    private static IReadOnlyList<string> ExtractSourceRootHashes(IEnumerable<string> payloads)
    {
        var roots = new HashSet<Guid>();
        foreach (var payload in payloads)
        {
            using var document = JsonDocument.Parse(payload);
            CollectRootIds(document.RootElement, roots);
        }
        if (roots.Count == 0)
            throw new MarketplaceRefundException(
                "The settlement funding provenance contains no source roots.");
        return roots.Order().Select(root => Hash(root.ToString("N"))).ToArray();
    }

    private static void CollectRootIds(JsonElement element, ISet<Guid> roots)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (Normalize(property.Name) == "rootsourcestampid" &&
                    property.Value.ValueKind == JsonValueKind.String &&
                    Guid.TryParse(property.Value.GetString(), out var rootId))
                    roots.Add(rootId);
                CollectRootIds(property.Value, roots);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray()) CollectRootIds(item, roots);
        }
    }

    private static string Normalize(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static void Validate(RefundAuthoritativeMarketplaceOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId == Guid.Empty || request.ActorId == Guid.Empty ||
            request.SettlementId == Guid.Empty || request.RiskDecisionId == Guid.Empty)
            throw new ArgumentException(
                "Tenant, actor, settlement and risk decision IDs are required.", nameof(request));
        if (!Enum.IsDefined(request.Authority)) throw new ArgumentOutOfRangeException(nameof(request));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Quantity);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ReasonCode);
        if (request.ReasonCode.Trim().Length > 100)
            throw new ArgumentOutOfRangeException(nameof(request));
        ArgumentException.ThrowIfNullOrWhiteSpace(request.SubjectReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.JurisdictionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OperationFingerprint);
    }

    private static Guid DeterministicId(Guid source, string purpose)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{source:N}:marketplace:{purpose}"));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static string Hash(string value) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
