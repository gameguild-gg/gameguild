using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Domain event raised when a payment method is registered locally (issue #414).
///     Carries display-safe metadata so confirmation handlers can render the method
///     (e.g. "Visa ending 4242") without re-reading the registry.
/// </summary>
public class PaymentMethodRegisteredEvent(
    Guid paymentMethodId,
    Guid tenantId,
    Guid userId,
    string gatewayPaymentMethodId,
    string? cardBrand = null,
    string? cardLast4 = null,
    bool restored = false) : DomainEvent
{
    /// <summary>Local payment method ID.</summary>
    public Guid PaymentMethodId { get; } = paymentMethodId;

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>Owning user.</summary>
    public Guid UserId { get; } = userId;

    /// <summary>Gateway payment method reference (token only).</summary>
    public string GatewayPaymentMethodId { get; } = gatewayPaymentMethodId;

    /// <summary>Card brand for display (Visa, Mastercard, ...); null for non-card methods.</summary>
    public string? CardBrand { get; } = cardBrand;

    /// <summary>Last four digits for display; null for non-card methods.</summary>
    public string? CardLast4 { get; } = cardLast4;

    /// <summary>Whether this registration restored a previously removed token.</summary>
    public bool Restored { get; } = restored;
}

/// <summary>
///     Domain event raised when a payment method is removed locally (issue #414).
/// </summary>
public class PaymentMethodDeletedEvent(
    Guid paymentMethodId,
    Guid tenantId,
    Guid userId,
    string? cardBrand = null,
    string? cardLast4 = null) : DomainEvent
{
    /// <summary>Local payment method ID.</summary>
    public Guid PaymentMethodId { get; } = paymentMethodId;

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>Owning user.</summary>
    public Guid UserId { get; } = userId;

    /// <summary>Card brand for display; null for non-card methods.</summary>
    public string? CardBrand { get; } = cardBrand;

    /// <summary>Last four digits for display; null for non-card methods.</summary>
    public string? CardLast4 { get; } = cardLast4;
}

/// <summary>
///     Domain event raised when a payment method becomes its owner's default.
/// </summary>
public class PaymentMethodSetAsDefaultEvent(Guid paymentMethodId, Guid tenantId, Guid userId) : DomainEvent
{
    /// <summary>Local payment method ID.</summary>
    public Guid PaymentMethodId { get; } = paymentMethodId;

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>Owning user.</summary>
    public Guid UserId { get; } = userId;
}

/// <summary>
///     Domain event raised when a payment method's expiry month passes and it is auto-expired.
/// </summary>
public class PaymentMethodExpiredEvent(Guid paymentMethodId, Guid tenantId, Guid userId) : DomainEvent
{
    /// <summary>Local payment method ID.</summary>
    public Guid PaymentMethodId { get; } = paymentMethodId;

    /// <summary>Owning tenant.</summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>Owning user.</summary>
    public Guid UserId { get; } = userId;
}
