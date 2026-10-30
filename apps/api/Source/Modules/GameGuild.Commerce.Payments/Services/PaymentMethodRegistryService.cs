using System.Text.Json;
using GameGuild.Commerce;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Payment-method registry management (issue #414): tokenized registration with
/// duplicate-card (fingerprint) rejection, display updates, single-default invariant with
/// subscription re-pointing, a fail-closed delete guard while billable subscriptions still
/// reference a method, encrypted-at-rest gateway references (DataProtection), audit
/// trail rows for every registry mutation, and confirmation events on add/remove.
/// </summary>
public class PaymentMethodRegistryService(
    IPaymentMethodRepository repository,
    IStripeCustomerService stripeCustomerService,
    ISubscriptionPaymentMethodCoordination subscriptionCoordination,
    IApplicationDbContext context,
    ILogger<PaymentMethodRegistryService> logger,
    IGatewayPaymentMethodIdProtector? gatewayIdProtector = null,
    IRevenueAuditService? revenueAuditService = null,
    IPublisher? publisher = null) : IPaymentMethodService
{
    private const string GatewayProviderStripe = "stripe";

    private readonly IGatewayPaymentMethodIdProtector _gatewayIdProtector =
        gatewayIdProtector ?? PassthroughGatewayPaymentMethodIdProtector.Instance;

    private readonly IRevenueAuditService? _revenueAuditService = revenueAuditService;
    private readonly IPublisher? _publisher = publisher;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> ListAsync(
        PaymentMethodActor actor,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure<IReadOnlyList<PaymentMethodDto>>(actorError);
        }

        var scopeToOwner = !actor.IsSystemAdmin;
        var methods = await repository
            .ListAsync(tenantId, scopeToOwner ? actor.UserId : null, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<IReadOnlyList<PaymentMethodDto>>(methods.Select(method => ToDto(method, _gatewayIdProtector)).ToList());
    }

    /// <inheritdoc />
    public async Task<Result<PaymentMethodDto>> GetByIdAsync(
        PaymentMethodActor actor,
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure<PaymentMethodDto>(actorError);
        }

        var paymentMethod = await repository.GetByIdAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (paymentMethod is null)
        {
            return NotFound(paymentMethodId);
        }

        if (!CanAccess(actor, paymentMethod, out var accessError))
        {
            return Result.Failure<PaymentMethodDto>(accessError);
        }

        return Result.Success(ToDto(paymentMethod, _gatewayIdProtector));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentMethodDto>> CreateAsync(
        PaymentMethodActor actor,
        CreatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure<PaymentMethodDto>(actorError);
        }

        if (request.TenantId == Guid.Empty)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation("PaymentMethods.TenantRequired", "TenantId cannot be empty."));
        }

        if (!actor.IsSystemAdmin && actor.TenantId != request.TenantId)
        {
            return Result.Failure<PaymentMethodDto>(Error.Forbidden("PaymentMethods.CrossTenant", "Cross-tenant payment method registration is denied."));
        }

        if (GatewayPaymentMethodTokens.ResolveProvider(request.GatewayPaymentMethodId, request.GatewayProvider) is not { } gatewayProvider)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation("PaymentMethods.InvalidToken", GatewayPaymentMethodTokens.ValidationMessage));
        }

        // Optional method-type hint (issue #414): gateways whose tokens are opaque
        // (Braintree, Adyen, Authorize.Net) expose no server-side metadata lookup, so the
        // registrant states which family the token vaults (e.g. an Apple Pay token
        // provisioned client-side). Fail closed: unknown hints are rejected, never
        // guessed. Stripe ignores the hint — its API lookup is authoritative.
        string? methodTypeHint = null;
        if (!string.IsNullOrWhiteSpace(request.MethodType))
        {
            methodTypeHint = NormalizeMethodTypeHint(request.MethodType);
            if (methodTypeHint is null)
            {
                return Result.Failure<PaymentMethodDto>(Error.Validation(
                    "PaymentMethods.InvalidMethodType",
                    $"MethodType '{request.MethodType}' is not a supported payment method family. Use one of: {string.Join(", ", SupportedMethodTypeHints)}."));
            }
        }

        var gatewayDetails = await ResolveGatewayDetailsAsync(request.GatewayPaymentMethodId, gatewayProvider, methodTypeHint, cancellationToken).ConfigureAwait(false);
        if (gatewayDetails is null)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation(
                "PaymentMethods.GatewayLookupFailed",
                "The payment method could not be verified at the gateway. Confirm the payment method token and retry."));
        }

        var ownerUserId = actor.IsSystemAdmin ? request.OwnerUserId ?? Guid.Empty : actor.UserId;
        if (ownerUserId == Guid.Empty)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation(
                "PaymentMethods.OwnerRequired",
                "OwnerUserId is required when a system administrator registers a payment method."));
        }

        return await UpsertCoreAsync(
            actor,
            request.TenantId,
            ownerUserId,
            request.GatewayPaymentMethodId,
            gatewayProvider,
            gatewayDetails,
            request.SetAsDefault,
            request.Nickname,
            request.BillingAddress,
            subscriptionId: null,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentMethodDto>> UpsertFromCheckoutAsync(
        PaymentMethodActor actor,
        Guid tenantId,
        Guid subscriptionId,
        string gatewayPaymentMethodId,
        CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty || subscriptionId == Guid.Empty)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation("PaymentMethods.InvalidCheckoutContext", "TenantId and SubscriptionId are required to register a checkout payment method."));
        }

        if (GatewayPaymentMethodTokens.ResolveProvider(gatewayPaymentMethodId) is not { } checkoutGatewayProvider)
        {
            return Result.Failure<PaymentMethodDto>(Error.Validation("PaymentMethods.InvalidToken", GatewayPaymentMethodTokens.ValidationMessage));
        }

        var ownerUserId = actor.UserId;
        if (ownerUserId == Guid.Empty)
        {
            // Service actors completing checkouts on behalf of a tenant: attribute the
            // method to the subscription's billing owner.
            ownerUserId = await subscriptionCoordination
                .GetSubscriptionBillingUserIdAsync(subscriptionId, cancellationToken)
                .ConfigureAwait(false)
                ?? Guid.Empty;
        }

        if (ownerUserId == Guid.Empty)
        {
            return Result.Failure<PaymentMethodDto>(Error.Problem(
                "PaymentMethods.OwnerUnresolved",
                "No billing owner could be resolved for the checkout payment method."));
        }

        var gatewayDetails = await ResolveGatewayDetailsAsync(gatewayPaymentMethodId, checkoutGatewayProvider, methodTypeHint: null, cancellationToken).ConfigureAwait(false);
        if (gatewayDetails is null)
        {
            // Checkout already succeeded at the gateway; a metadata lookup failure must not
            // fail the charge. Register the reference without display metadata.
            gatewayDetails = new GatewayPaymentMethodDetails("card", null, null, null, null, null, null, null, null);
            logger.LogWarning(
                "Gateway details unavailable for payment method during checkout upsert; registered without display metadata (Subscription {SubscriptionId}).",
                subscriptionId);
        }

        return await UpsertCoreAsync(
            actor,
            tenantId,
            ownerUserId,
            gatewayPaymentMethodId,
            checkoutGatewayProvider,
            gatewayDetails,
            setAsDefault: true,
            nickname: null,
            billingAddress: null,
            subscriptionId,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<Result<PaymentMethodDto>> UpdateAsync(
        PaymentMethodActor actor,
        Guid paymentMethodId,
        UpdatePaymentMethodRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure<PaymentMethodDto>(actorError);
        }

        var paymentMethod = await repository.GetByIdAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (paymentMethod is null)
        {
            return NotFound(paymentMethodId);
        }

        if (!CanAccess(actor, paymentMethod, out var accessError))
        {
            return Result.Failure<PaymentMethodDto>(accessError);
        }

        paymentMethod.UpdateDisplay(request.Nickname, request.BillingAddress);
        await repository.UpdateAsync(paymentMethod, cancellationToken).ConfigureAwait(false);

        await RecordAuditAsync(actor, paymentMethod, "Updated", cancellationToken).ConfigureAwait(false);

        return Result.Success(ToDto(paymentMethod, _gatewayIdProtector));
    }

    /// <inheritdoc />
    public async Task<Result<PaymentMethodDto>> SetDefaultAsync(
        PaymentMethodActor actor,
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure<PaymentMethodDto>(actorError);
        }

        var paymentMethod = await repository.GetByIdAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (paymentMethod is null)
        {
            return NotFound(paymentMethodId);
        }

        if (!CanAccess(actor, paymentMethod, out var accessError))
        {
            return Result.Failure<PaymentMethodDto>(accessError);
        }

        if (paymentMethod.Status != PaymentMethodStatus.Active)
        {
            return Result.Failure<PaymentMethodDto>(Error.Conflict(
                "PaymentMethods.NotUsable",
                $"Payment method {paymentMethodId} is {paymentMethod.Status.ToString().ToLowerInvariant()} and cannot be the default."));
        }

        var tenantId = paymentMethod.TenantId ?? Guid.Empty;
        var previousDefault = paymentMethod.IsDefault
            ? null
            : await repository.GetDefaultAsync(tenantId, paymentMethod.UserId, cancellationToken).ConfigureAwait(false);

        await using var transaction = await BeginOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (previousDefault is not null)
            {
                previousDefault.ClearDefault();
                await repository.UpdateAsync(previousDefault, cancellationToken).ConfigureAwait(false);
            }

            paymentMethod.MarkAsDefault();
            await repository.UpdateAsync(paymentMethod, cancellationToken).ConfigureAwait(false);

            // Changing the default re-points the subscriptions billed with the old default.
            if (previousDefault is not null)
            {
                var rePointed = await subscriptionCoordination
                    .RepointSubscriptionsAsync(tenantId, previousDefault.Id, paymentMethod.Id, cancellationToken)
                    .ConfigureAwait(false);
                if (rePointed > 0)
                {
                    logger.LogInformation(
                        "Default payment method changed for tenant {TenantId}; {RePointed} subscription(s) re-pointed from {FromId} to {ToId}.",
                        tenantId, rePointed, previousDefault.Id, paymentMethod.Id);
                }
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            throw;
        }

        await RecordAuditAsync(actor, paymentMethod, "StatusChanged", cancellationToken).ConfigureAwait(false);

        return Result.Success(ToDto(paymentMethod, _gatewayIdProtector));
    }

    /// <inheritdoc />
    public async Task<Result> DeleteAsync(
        PaymentMethodActor actor,
        Guid paymentMethodId,
        bool confirm = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryValidateActor(actor, out var actorError))
        {
            return Result.Failure(actorError);
        }

        if (!confirm)
        {
            // Removing a payment method is a destructive, confirmation-required action
            // (issue #414): callers must pass an explicit confirm flag.
            return Result.Failure(Error.Validation(
                "PaymentMethods.DeleteConfirmationRequired",
                "Removing a payment method requires confirmation. Re-issue the request with confirm=true."));
        }

        var paymentMethod = await repository.GetByIdAsync(paymentMethodId, cancellationToken).ConfigureAwait(false);
        if (paymentMethod is null)
        {
            return Result.Failure(NotFoundError(paymentMethodId));
        }

        if (!CanAccess(actor, paymentMethod, out var accessError))
        {
            return Result.Failure(accessError);
        }

        var tenantId = paymentMethod.TenantId ?? Guid.Empty;
        var billableSubscriptions = await subscriptionCoordination
            .CountBillableSubscriptionsOnPaymentMethodAsync(tenantId, paymentMethod.Id, cancellationToken)
            .ConfigureAwait(false);

        if (billableSubscriptions > 0)
        {
            // Fail closed: subscriptions must be migrated to another method first.
            return Result.Failure(Error.Conflict(
                "PaymentMethods.InUseBySubscriptions",
                $"Payment method {paymentMethodId} is the billing method for {billableSubscriptions} active subscription(s). " +
                "Set another payment method as the default (which re-points subscriptions) before removing this one."));
        }

        paymentMethod.SoftDelete();
        await repository.UpdateAsync(paymentMethod, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Payment method {PaymentMethodId} soft-deleted for tenant {TenantId}.",
            paymentMethodId, tenantId);

        await RecordAuditAsync(actor, paymentMethod, "Deleted", cancellationToken).ConfigureAwait(false);
        await PublishSafeAsync(
            new PaymentMethodDeletedEvent(paymentMethod.Id, tenantId, paymentMethod.UserId, paymentMethod.CardBrand, paymentMethod.CardLast4),
            cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }

    private async Task<Result<PaymentMethodDto>> UpsertCoreAsync(
        PaymentMethodActor actor,
        Guid tenantId,
        Guid ownerUserId,
        string gatewayPaymentMethodId,
        string gatewayProvider,
        GatewayPaymentMethodDetails gatewayDetails,
        bool setAsDefault,
        string? nickname,
        string? billingAddress,
        Guid? subscriptionId,
        CancellationToken cancellationToken)
    {
        var type = MapGatewayType(gatewayDetails.Type);

        var existing = await repository
            .GetByGatewayIdAsync(tenantId, gatewayProvider, gatewayPaymentMethodId, cancellationToken)
            .ConfigureAwait(false);

        PaymentMethod? deleted = null;
        if (existing is null)
        {
            deleted = await repository
                .GetDeletedByGatewayIdAsync(tenantId, gatewayProvider, gatewayPaymentMethodId, cancellationToken)
                .ConfigureAwait(false);
        }

        if ((existing ?? deleted) is { } registered && registered.UserId != ownerUserId)
        {
            return Result.Failure<PaymentMethodDto>(Error.Conflict(
                "PaymentMethods.OwnerMismatch",
                "This payment method is already registered to another owner."));
        }

        // Duplicate-card detection (issue #414): a brand-new token (or the restoration of a
        // removed one) whose gateway fingerprint already belongs to another live method of
        // the same owner is rejected — the customer must reuse the existing entry. The same
        // token being re-registered is exempt (it is its own fingerprint match).
        if (existing is null
            && !string.IsNullOrWhiteSpace(gatewayDetails.CardFingerprint))
        {
            var duplicate = await repository
                .GetLiveByFingerprintAsync(
                    tenantId,
                    ownerUserId,
                    gatewayProvider,
                    gatewayDetails.CardFingerprint,
                    excludePaymentMethodId: deleted?.Id,
                    cancellationToken)
                .ConfigureAwait(false);

            if (duplicate is not null)
            {
                logger.LogInformation(
                    "Duplicate card registration rejected for tenant {TenantId} user {UserId}: fingerprint of token {GatewayPaymentMethodId} already registered as payment method {ExistingPaymentMethodId}.",
                    tenantId, ownerUserId, LogRedaction.RedactId(gatewayPaymentMethodId, "pm"), duplicate.Id);
                return Result.Failure<PaymentMethodDto>(Error.Conflict(
                    "PaymentMethods.DuplicateCard",
                    "This card is already registered. A card can only be saved once per user; reuse or set the existing entry as the default instead of adding it again."));
            }
        }

        var firstForOwner = existing is null
                            && deleted is null
                            && await repository.GetDefaultAsync(tenantId, ownerUserId, cancellationToken).ConfigureAwait(false) is null;

        var shouldBeDefault = setAsDefault || firstForOwner;

        // Encrypted-at-rest gateway reference (issue #414): stored value is protected with
        // DataProtection; equality lookups use the deterministic hash instead of ciphertext.
        var protectedGatewayId = _gatewayIdProtector.Protect(gatewayPaymentMethodId);
        var gatewayIdHash = GatewayPaymentMethodIdHash.Compute(gatewayProvider, gatewayPaymentMethodId);

        await using var transaction = await BeginOwnedTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PaymentMethod paymentMethod;
            string auditAction;
            bool restored;
            if (existing is not null)
            {
                paymentMethod = existing;
                paymentMethod.UpdateGatewayMetadata(type, gatewayDetails.CardBrand, gatewayDetails.CardLast4, gatewayDetails.CardExpMonth, gatewayDetails.CardExpYear, gatewayDetails.CardFingerprint);
                auditAction = "Updated";
                restored = false;
            }
            else if (deleted is not null)
            {
                // Re-registering a previously removed token restores its row (history preserved).
                paymentMethod = deleted;
                paymentMethod.Restore();
                paymentMethod.UpdateGatewayMetadata(type, gatewayDetails.CardBrand, gatewayDetails.CardLast4, gatewayDetails.CardExpMonth, gatewayDetails.CardExpYear, gatewayDetails.CardFingerprint);
                auditAction = "Restored";
                restored = true;
            }
            else
            {
                paymentMethod = new PaymentMethod(tenantId, ownerUserId, gatewayProvider, protectedGatewayId, type, isDefault: false);
                paymentMethod.UpdateGatewayMetadata(type, gatewayDetails.CardBrand, gatewayDetails.CardLast4, gatewayDetails.CardExpMonth, gatewayDetails.CardExpYear, gatewayDetails.CardFingerprint);
                paymentMethod.UpdateDisplay(nickname, billingAddress);
                auditAction = "Created";
                restored = false;
            }

            // New rows store the protected reference; legacy rows are migrated lazily the
            // next time their token is registered.
            if (!_gatewayIdProtector.IsProtected(paymentMethod.GatewayPaymentMethodId))
            {
                paymentMethod.StoreProtectedGatewayReference(protectedGatewayId, gatewayIdHash);
            }
            else if (paymentMethod.GatewayPaymentMethodIdHash is null)
            {
                paymentMethod.StoreProtectedGatewayReference(paymentMethod.GatewayPaymentMethodId, gatewayIdHash);
            }

            if (shouldBeDefault && !paymentMethod.IsDefault)
            {
                var previousDefault = await repository
                    .GetDefaultAsync(tenantId, ownerUserId, cancellationToken)
                    .ConfigureAwait(false);

                if (previousDefault is not null && previousDefault.Id != paymentMethod.Id)
                {
                    previousDefault.ClearDefault();
                    await repository.UpdateAsync(previousDefault, cancellationToken).ConfigureAwait(false);

                    var rePointed = await subscriptionCoordination
                        .RepointSubscriptionsAsync(tenantId, previousDefault.Id, paymentMethod.Id, cancellationToken)
                        .ConfigureAwait(false);
                    if (rePointed > 0)
                    {
                        logger.LogInformation(
                            "Default payment method registered for tenant {TenantId}; {RePointed} subscription(s) re-pointed.",
                            tenantId, rePointed);
                    }
                }

                paymentMethod.MarkAsDefault();
            }

            if (subscriptionId.HasValue)
            {
                paymentMethod.MarkUsed(SystemClock.UtcNow);
                await subscriptionCoordination
                    .AssignSubscriptionPaymentMethodAsync(subscriptionId.Value, paymentMethod.Id, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (existing is null && deleted is null)
            {
                await repository.AddAsync(paymentMethod, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await repository.UpdateAsync(paymentMethod, cancellationToken).ConfigureAwait(false);
            }
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            await RecordAuditAsync(actor, paymentMethod, auditAction, cancellationToken).ConfigureAwait(false);
            await PublishRegistrationAsync(paymentMethod, gatewayPaymentMethodId, restored, cancellationToken).ConfigureAwait(false);

            return Result.Success(ToDto(paymentMethod, _gatewayIdProtector));
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }
            throw;
        }
    }

    private async Task<IDbContextTransaction?> BeginOwnedTransactionAsync(CancellationToken cancellationToken)
    {
        // The command pipeline owns its transaction, including the operation outbox.
        // Joining it must neither start a nested transaction nor commit it early.
        if (context is DbContext database && database.Database.IsRelational()
            && database.Database.CurrentTransaction is not null)
        {
            return null;
        }

        return await context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<GatewayPaymentMethodDetails?> ResolveGatewayDetailsAsync(
        string gatewayPaymentMethodId,
        string gatewayProvider,
        string? methodTypeHint,
        CancellationToken cancellationToken)
    {
        // Gateway-aware resolution (issue #413/#414): Stripe tokens are verified against the
        // Stripe API as before (its type report is authoritative); the other registered
        // gateways tokenize client-side and expose no server-side metadata lookup. For
        // those, the family is inferred — PayPal tokens are always billing agreements,
        // opaque tokens take the registrant's explicit method-type hint (e.g. an Apple Pay
        // or Google Pay token vaulted at Braintree/Adyen) and default to card otherwise —
        // mirroring the established checkout fallback.
        if (!string.Equals(gatewayProvider, GatewayProviderStripe, StringComparison.OrdinalIgnoreCase))
        {
            var inferredType = gatewayProvider is GatewayProviderIds.PayPal
                ? "paypal"
                : methodTypeHint ?? "card";
            return new GatewayPaymentMethodDetails(inferredType, null, null, null, null, null, null, null, null);
        }

        var result = await stripeCustomerService
            .GetPaymentMethodAsync(gatewayPaymentMethodId, cancellationToken)
            .ConfigureAwait(false);

        if (result is null || !result.Success || result.Details is null)
        {
            logger.LogWarning(
                "Gateway payment method lookup failed for {PaymentMethodId}: {ErrorCode}",
                LogRedaction.RedactId(gatewayPaymentMethodId, "pm"),
                result?.ErrorCode);
            return null;
        }

        return result.Details;
    }

    private static PaymentMethodType MapGatewayType(string gatewayType) => gatewayType?.Trim().ToLowerInvariant() switch
    {
        "card" => PaymentMethodType.Card,
        "us_bank_account" or "bank_account" or "sepa_debit" => PaymentMethodType.BankAccount,
        "paypal" => PaymentMethodType.PayPal,
        "apple_pay" => PaymentMethodType.ApplePay,
        "google_pay" => PaymentMethodType.GooglePay,
        "link" => PaymentMethodType.Wallet,
        _ => PaymentMethodType.Card
    };

    /// <summary>
    ///     Method-type hint families accepted on registration (issue #414) for gateways
    ///     whose tokens carry no server-side metadata.
    /// </summary>
    public static readonly string[] SupportedMethodTypeHints =
    [
        "card", "bank_account", "us_bank_account", "paypal", "apple_pay", "google_pay", "wallet"
    ];

    /// <summary>
    ///     Normalizes a method-type hint to its gateway type string, or null when the hint
    ///     names no supported family (fail closed — never silently coerced).
    /// </summary>
    internal static string? NormalizeMethodTypeHint(string? methodTypeHint)
    {
        var normalized = methodTypeHint?.Trim().ToLowerInvariant();
        return SupportedMethodTypeHints.Contains(normalized) ? normalized : null;
    }

    private static bool TryValidateActor(PaymentMethodActor actor, out Error error)
    {
        if (actor.IsSystemAdmin || actor.TenantId.HasValue)
        {
            error = Error.None;
            return true;
        }

        error = Error.Forbidden(
            "PaymentMethods.NoTenantContext",
            "The actor is not associated with any tenant and cannot manage payment methods.");
        return false;
    }

    private static bool CanAccess(PaymentMethodActor actor, PaymentMethod paymentMethod, out Error error)
    {
        // Fail closed: foreign-tenant methods look like 404s (no existence leak); same-tenant
        // non-owner access is an explicit 403.
        if (!actor.IsSystemAdmin && actor.TenantId != paymentMethod.TenantId)
        {
            error = NotFoundError(paymentMethod.Id);
            return false;
        }

        if (!actor.IsSystemAdmin && actor.UserId != paymentMethod.UserId)
        {
            error = Error.Forbidden(
                "PaymentMethods.NotOwner",
                "Only the payment method owner or a billing administrator can manage this payment method.");
            return false;
        }

        error = Error.None;
        return true;
    }

    private static Result<PaymentMethodDto> NotFound(Guid paymentMethodId) =>
        Result.Failure<PaymentMethodDto>(NotFoundError(paymentMethodId));

    private static Error NotFoundError(Guid paymentMethodId) =>
        Error.NotFound("PaymentMethods.NotFound", $"Payment method {paymentMethodId} was not found.");

    /// <summary>
    ///     Writes an audit-trail row for a registry mutation (issue #414): every create,
    ///     update, set-default and delete is attributable. Payload is display-safe metadata
    ///     only — the protected gateway reference never enters the audit trail.
    /// </summary>
    private async Task RecordAuditAsync(
        PaymentMethodActor actor,
        PaymentMethod paymentMethod,
        string action,
        CancellationToken cancellationToken)
    {
        if (_revenueAuditService is null)
        {
            return;
        }

        var changedBy = actor.UserId != Guid.Empty ? actor.UserId : paymentMethod.UserId;
        var newValue = JsonSerializer.Serialize(new
        {
            type = paymentMethod.Type.ToString(),
            gatewayProvider = paymentMethod.GatewayProvider,
            cardBrand = paymentMethod.CardBrand,
            cardLast4 = paymentMethod.CardLast4,
            isDefault = paymentMethod.IsDefault,
            status = paymentMethod.Status.ToString()
        });

        await _revenueAuditService.RecordAuditTrailAsync(
            entityType: nameof(PaymentMethod),
            entityId: paymentMethod.Id,
            action: action,
            changedBy: changedBy,
            oldValue: null,
            newValue: newValue,
            reason: action switch
            {
                "Created" => "Payment method registered from a gateway token",
                "Restored" => "Previously removed payment method re-registered",
                "Deleted" => "Payment method removed after explicit confirmation",
                "StatusChanged" => "Payment method set as the owner's default",
                _ => "Payment method updated"
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Publishes the registration confirmation event (issue #414) after the transaction
    ///     committed. Best-effort: notification pipeline problems never fail the registration.
    /// </summary>
    private async Task PublishRegistrationAsync(
        PaymentMethod paymentMethod,
        string gatewayPaymentMethodId,
        bool restored,
        CancellationToken cancellationToken)
        => await PublishSafeAsync(
            new PaymentMethodRegisteredEvent(
                paymentMethod.Id,
                paymentMethod.TenantId ?? Guid.Empty,
                paymentMethod.UserId,
                gatewayPaymentMethodId,
                paymentMethod.CardBrand,
                paymentMethod.CardLast4,
                restored),
            cancellationToken).ConfigureAwait(false);

    private async Task PublishSafeAsync<TNotification>(TNotification domainEvent, CancellationToken cancellationToken)
        where TNotification : INotification
    {
        if (_publisher is null)
        {
            return;
        }

        try
        {
            await _publisher.Publish(domainEvent, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to publish payment-method event {EventType}; the registry mutation stays committed.",
                domainEvent.GetType().Name);
        }
    }

    private static PaymentMethodDto ToDto(PaymentMethod paymentMethod, IGatewayPaymentMethodIdProtector protector) => new(
        paymentMethod.Id,
        paymentMethod.TenantId ?? Guid.Empty,
        paymentMethod.UserId,
        paymentMethod.Type.ToString(),
        paymentMethod.GatewayProvider,
        protector.Unprotect(paymentMethod.GatewayPaymentMethodId),
        paymentMethod.IsDefault,
        paymentMethod.Status.ToString(),
        paymentMethod.CardBrand,
        paymentMethod.CardLast4,
        paymentMethod.CardExpMonth,
        paymentMethod.CardExpYear,
        paymentMethod.Nickname,
        paymentMethod.BillingAddress,
        paymentMethod.LastUsedAt,
        paymentMethod.ExpiresAt,
        paymentMethod.GetDaysUntilExpiry(SystemClock.UtcNow),
        paymentMethod.CreatedAt,
        paymentMethod.UpdatedAt);
}

/// <summary>
///     Identity protector used when no DataProtection-backed protector is supplied (unit
///     tests, legacy hosting): values are stored and returned unchanged.
/// </summary>
internal sealed class PassthroughGatewayPaymentMethodIdProtector : IGatewayPaymentMethodIdProtector
{
    public static readonly PassthroughGatewayPaymentMethodIdProtector Instance = new();

    private PassthroughGatewayPaymentMethodIdProtector()
    {
    }

    public string Protect(string gatewayPaymentMethodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gatewayPaymentMethodId);
        return gatewayPaymentMethodId.Trim();
    }

    public string Unprotect(string storedGatewayPaymentMethodId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storedGatewayPaymentMethodId);
        return storedGatewayPaymentMethodId.Trim();
    }

    public bool IsProtected(string storedGatewayPaymentMethodId) => false;
}
