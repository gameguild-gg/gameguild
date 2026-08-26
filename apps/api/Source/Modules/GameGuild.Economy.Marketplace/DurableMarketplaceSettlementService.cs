using System.Data;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Marketplace.Persistence;
using GameGuild.Economy.Persistence;
using GameGuild.Economy.Risk;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Marketplace;

public sealed record SettleAuthoritativeMarketplaceOrderRequest(
    Guid TenantId,
    Guid ActorId,
    Guid OrderId,
    MarketplaceCurrencyChoice CurrencyChoice,
    string SubjectReference,
    string JurisdictionCode,
    Guid RiskDecisionId,
    string OperationFingerprint,
    IdempotencyKey IdempotencyKey,
    DateTimeOffset SettledAt);

public sealed record DurableMarketplaceSettlementResult(
    Guid SettlementId,
    Guid OrderId,
    Guid ProductId,
    Guid BuyerId,
    Guid SellerId,
    MarketplaceSettlementStatus Status,
    MarketplaceEntitlementStatus EntitlementStatus,
    IReadOnlyList<MarketplacePriceLegSnapshot> Legs,
    PostingId PostingId,
    long JournalSequence,
    string JournalHash,
    bool IsDuplicate,
    DateTimeOffset SettledAt);

public interface IDurableMarketplaceSettlementService
{
    ValueTask<DurableMarketplaceSettlementResult> SettleAsync(
        SettleAuthoritativeMarketplaceOrderRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class DurableMarketplaceSettlementService : IDurableMarketplaceSettlementService
{
    private const string RegisteredCapabilityName = "marketplace-settlement";
    private readonly DbContext _db;
    private readonly IAuthoritativeMarketplaceOrderReader _orders;
    private readonly IDurableMarketplacePolicyReader _policies;
    private readonly IEconomyWalletDirectory _wallets;
    private readonly IMarketplaceFifoReservationGateway _reservations;
    private readonly IEconomyCapabilityAuthorizationService _capabilities;
    private readonly IRegisteredPostingCapabilityResolver _postingAuthority;
    private readonly IMarketplaceSettlementLedgerGateway _ledger;

    public DurableMarketplaceSettlementService(
        IApplicationDbContext context,
        IAuthoritativeMarketplaceOrderReader orders,
        IDurableMarketplacePolicyReader policies,
        IEconomyWalletDirectory wallets,
        IMarketplaceFifoReservationGateway reservations,
        IEconomyCapabilityAuthorizationService capabilities,
        IRegisteredPostingCapabilityResolver postingAuthority,
        IMarketplaceSettlementLedgerGateway ledger)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(wallets);
        ArgumentNullException.ThrowIfNull(reservations);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(postingAuthority);
        ArgumentNullException.ThrowIfNull(ledger);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "Durable Marketplace settlement requires the application's relational DbContext.");
        _orders = orders;
        _policies = policies;
        _wallets = wallets;
        _reservations = reservations;
        _capabilities = capabilities;
        _postingAuthority = postingAuthority;
        _ledger = ledger;
    }

    public async ValueTask<DurableMarketplaceSettlementResult> SettleAsync(
        SettleAuthoritativeMarketplaceOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);
        var idempotencyHash = Hash(request.IdempotencyKey.Value);
        return await PostgreSqlTransactionExecutor.ExecuteAsync(
            _db, IsolationLevel.Serializable, async _ =>
        {
        var duplicate = await _db.Set<MarketplaceSettlementRow>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.TenantId == request.TenantId && row.OrderId == request.OrderId,
                cancellationToken);
        if (duplicate is not null)
        {
            if (!string.Equals(duplicate.IdempotencyKey, idempotencyHash, StringComparison.Ordinal))
                throw new MarketplaceIdempotencyConflictException(
                    "The authoritative order is already bound to another settlement request.");
            return await MapAsync(duplicate, true, cancellationToken);
        }

        var order = await _orders.ReadAsync(
            request.TenantId, request.ActorId, request.OrderId, cancellationToken);
        var policy = await _policies.GetEffectiveAsync(
            request.TenantId, order.ProductId, request.SettledAt, cancellationToken);
        if (policy.Policy.SellerId != order.SellerId)
            throw new MarketplaceOrderSnapshotException(
                "The signed coin policy does not match the authoritative product creator.");
        var quote = ScaleQuote(
            policy.Policy.Quote(request.CurrencyChoice),
            order.Quantity,
            policy.Policy.PlatformFeePpm);
        var buyerWallet = await _wallets.GetOwnerWalletAsync(
            request.TenantId, order.BuyerId, cancellationToken);
        var sellerWallet = await _wallets.GetOwnerWalletAsync(
            request.TenantId, order.SellerId, cancellationToken);
        var platformWallet = await _wallets.GetWalletAsync(
            request.TenantId, new WalletId(policy.PlatformFeeWalletId), cancellationToken);
        if (buyerWallet.WalletId == sellerWallet.WalletId ||
            buyerWallet.WalletId == platformWallet.WalletId ||
            sellerWallet.WalletId == platformWallet.WalletId)
            throw new MarketplaceOrderSnapshotException(
                "Marketplace settlement wallets must be distinct.");

        var settlementId = DeterministicId(order.OrderId, "settlement");
        var postingId = new PostingId(DeterministicId(settlementId, "posting"));
        var entitlementId = DeterministicId(settlementId, "entitlement");
        var reservations = _reservations.Reserve(new MarketplaceFifoReservationRequest(
            settlementId,
            buyerWallet.WalletId,
            quote.Legs.Select(leg => leg.Amount).ToArray(),
            request.SettledAt));
        var sourceRootHashes = reservations
            .Select(item => Hash(item.RootSourceStampId.Value.ToString("N")))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var receipt = await _capabilities.AuthorizeAndConsumeAsync(
            new EconomyCapabilityEvaluationContext(
                request.TenantId,
                request.ActorId,
                request.SubjectReference.Trim(),
                request.JurisdictionCode.Trim().ToUpperInvariant(),
                EconomyValueMovementCapability.MarketplaceSettlement,
                request.RiskDecisionId,
                request.OperationFingerprint.Trim(),
                policy.PayloadHash,
                Hash($"{sellerWallet.WalletId.Value:N}:{platformWallet.WalletId.Value:N}"),
                sourceRootHashes,
                request.SettledAt),
            cancellationToken);
        var authority = await _postingAuthority.ResolveAuthorityAsync(
            RegisteredCapabilityName,
            PostingTemplateKind.MarketplaceSettlement,
            receipt,
            cancellationToken);
        var posting = _ledger.Settle(new PersistedMarketplaceSettlementRequest(
            authority,
            receipt,
            settlementId,
            postingId,
            new IdempotencyKey(idempotencyHash),
            new PersistedMarketplaceOrderSnapshot(
                order.OrderId,
                order.OrderLineItemId,
                order.ProductId,
                order.ProductPricingVersionId,
                order.PriceVersionSnapshot,
                order.Quantity,
                order.UnitPriceSnapshot,
                order.FiatCurrencySnapshot,
                order.SnapshotHash),
            order.BuyerId,
            buyerWallet.WalletId,
            order.SellerId,
            sellerWallet.WalletId,
            platformWallet.WalletId,
            quote.PolicyVersion,
            (int)quote.Mode,
            quote.Legs.Select(leg => new PersistedMarketplacePriceLeg(
                leg.Currency, leg.Units, leg.SellerUnits, leg.PlatformFeeUnits)).ToArray(),
            reservations.Select(item => item.Id).ToArray(),
            entitlementId,
            request.SettledAt + policy.RefundHold,
            request.SettledAt));
        if (posting.PostingId != postingId)
            throw new RegisteredPostingRejectedException(
                "The Marketplace writer returned an unexpected posting identity.");

        var persisted = await _db.Set<MarketplaceSettlementRow>()
            .AsNoTracking()
            .SingleAsync(row => row.Id == settlementId, cancellationToken);
        return await MapAsync(persisted, posting.IsDuplicate, cancellationToken);
        }, cancellationToken);
    }

    private async ValueTask<DurableMarketplaceSettlementResult> MapAsync(
        MarketplaceSettlementRow row,
        bool duplicate,
        CancellationToken cancellationToken)
    {
        var legs = await _db.Set<MarketplaceSettlementLegRow>()
            .AsNoTracking()
            .Where(leg => leg.SettlementId == row.Id)
            .OrderBy(leg => leg.Currency)
            .Select(leg => new MarketplacePriceLegSnapshot(
                leg.Currency, leg.Units, leg.SellerUnits, leg.PlatformFeeUnits))
            .ToArrayAsync(cancellationToken);
        return new DurableMarketplaceSettlementResult(
            row.Id,
            row.OrderId,
            row.ProductId,
            row.BuyerId,
            row.SellerId,
            row.Status,
            row.EntitlementStatus,
            Array.AsReadOnly(legs),
            new PostingId(row.PostingId),
            row.JournalSequence,
            row.JournalHash,
            duplicate,
            row.SettledAt);
    }

    private static MarketplaceQuoteSnapshot ScaleQuote(
        MarketplaceQuoteSnapshot unitQuote,
        int quantity,
        int platformFeePpm)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var legs = unitQuote.Legs.Select(leg =>
        {
            var units = checked(leg.Units * quantity);
            var fee = ProductCurrencyPolicyVersion.CalculateFee(units, platformFeePpm);
            return new MarketplacePriceLegSnapshot(leg.Currency, units, units - fee, fee);
        }).ToArray();
        return new MarketplaceQuoteSnapshot(
            unitQuote.ProductId,
            unitQuote.SellerId,
            unitQuote.PolicyVersion,
            unitQuote.Mode,
            legs);
    }

    private static void Validate(SettleAuthoritativeMarketplaceOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.TenantId == Guid.Empty || request.ActorId == Guid.Empty ||
            request.OrderId == Guid.Empty || request.RiskDecisionId == Guid.Empty)
            throw new ArgumentException(
                "Tenant, actor, order and risk decision IDs are required.", nameof(request));
        if (!Enum.IsDefined(request.CurrencyChoice))
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
