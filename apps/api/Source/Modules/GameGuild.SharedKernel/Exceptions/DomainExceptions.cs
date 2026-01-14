namespace GameGuild.SharedKernel;

/// <summary>
///     Exception thrown when an invalid state transition is attempted.
/// </summary>
public class InvalidStateTransitionException : DomainException
{
    /// <summary>
    ///     The entity type that attempted the invalid transition.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    ///     The current state of the entity.
    /// </summary>
    public string FromState { get; }

    /// <summary>
    ///     The target state that was rejected.
    /// </summary>
    public string ToState { get; }

    public InvalidStateTransitionException(string entityType, string fromState, string toState)
        : base($"Invalid state transition for {entityType}: {fromState} -> {toState}")
    {
        EntityType = entityType;
        FromState = fromState;
        ToState = toState;
    }
}

/// <summary>
///     Base class for domain-specific exceptions.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
    protected DomainException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
///     Exception thrown when a requested entity is not found.
/// </summary>
public class EntityNotFoundException : DomainException
{
    /// <summary>
    ///     The type of entity that was not found.
    /// </summary>
    public string EntityType { get; }

    /// <summary>
    ///     The ID of the entity that was not found.
    /// </summary>
    public Guid EntityId { get; }

    public EntityNotFoundException(string entityType, Guid entityId)
        : base($"{entityType} with ID {entityId} was not found")
    {
        EntityType = entityType;
        EntityId = entityId;
    }
}

/// <summary>
///     Exception thrown when a subscription is not found.
/// </summary>
public class SubscriptionNotFoundException : EntityNotFoundException
{
    public SubscriptionNotFoundException(Guid subscriptionId)
        : base("Subscription", subscriptionId) { }
}

/// <summary>
///     Exception thrown when an order is not found.
/// </summary>
public class OrderNotFoundException : EntityNotFoundException
{
    public OrderNotFoundException(Guid orderId)
        : base("Order", orderId) { }
}

/// <summary>
///     Exception thrown when a payment is not found.
/// </summary>
public class PaymentNotFoundException : EntityNotFoundException
{
    public PaymentNotFoundException(Guid paymentId)
        : base("Payment", paymentId) { }
}

/// <summary>
///     Exception thrown when a product is not found.
/// </summary>
public class ProductNotFoundException : EntityNotFoundException
{
    public ProductNotFoundException(Guid productId)
        : base("Product", productId) { }
}

/// <summary>
///     Exception thrown when an invoice is not found.
/// </summary>
public class InvoiceNotFoundException : EntityNotFoundException
{
    public InvoiceNotFoundException(Guid invoiceId)
        : base("Invoice", invoiceId) { }
}

/// <summary>
///     Exception thrown when a webhook event is not found.
/// </summary>
public class WebhookEventNotFoundException : EntityNotFoundException
{
    public WebhookEventNotFoundException(Guid eventId)
        : base("WebhookEvent", eventId) { }
}
