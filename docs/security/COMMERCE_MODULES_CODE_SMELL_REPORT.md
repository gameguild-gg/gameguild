# Commerce Modules Code Smell Report

**Date:** January 14, 2026  
**Scope:** GameGuild.Commerce.* Modules (Products, Orders, Subscriptions, Billing, Payments)  
**Review Type:** DRY, SOLID, KISS, YAGNI, and General Code Smell Analysis

---

## Executive Summary

This report identifies code smells and design issues in the Commerce modules that violate established software engineering principles. While the modules are functionally complete and secure (as verified in the Security Audit), there are opportunities for improvement in code organization, maintainability, and adherence to best practices.

### Summary by Severity

| Severity | Count | Impact |
|----------|-------|--------|
| HIGH     | 3     | Significant maintainability/scalability issues |
| MEDIUM   | 7     | Code quality and DRY violations |
| LOW      | 5     | Minor improvements and cleanup |

---

## 1. DRY (Don't Repeat Yourself) Violations

### DRY-1: Duplicated State Machine Pattern (HIGH)

**Files:**
- [Subscription.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Subscriptions/Entities/Subscription.cs#L29-L86)
- [Order.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Orders/Entities/Order.cs#L138-L175)

**Issue:** Both entities implement nearly identical state machine patterns with:
- `ValidTransitions` dictionary
- `CanTransitionTo()` method
- `TransitionTo()` method

```csharp
// Subscription.cs
private static readonly Dictionary<SubscriptionStatus, HashSet<SubscriptionStatus>> ValidTransitions = new()
{
    { SubscriptionStatus.PendingActivation, new HashSet<SubscriptionStatus> { ... } },
    // ...
};

public bool CanTransitionTo(SubscriptionStatus newStatus) { ... }
private void TransitionTo(SubscriptionStatus newStatus) { ... }

// Order.cs - IDENTICAL PATTERN
private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
{
    { OrderStatus.Pending, new HashSet<OrderStatus> { ... } },
    // ...
};

public bool CanTransitionTo(OrderStatus newStatus) { ... }
private void TransitionTo(OrderStatus newStatus, string? reason = null) { ... }
```

**Recommendation:** Extract a generic `StateMachine<TStatus>` or `IStatefulEntity<TStatus>` abstraction to SharedKernel:

```csharp
public abstract class StatefulEntity<TStatus> : EntityBase where TStatus : Enum
{
    protected abstract Dictionary<TStatus, HashSet<TStatus>> ValidTransitions { get; }
    public abstract TStatus Status { get; protected set; }
    
    public bool CanTransitionTo(TStatus newStatus) => 
        ValidTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);
    
    protected void TransitionTo(TStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid state transition: {Status} -> {newStatus}");
        Status = newStatus;
    }
}
```

---

### DRY-2: Duplicated Command Handler Pattern (MEDIUM)

**Files:** All subscription command handlers in `GameGuild.Commerce.Subscriptions/Commands/`

**Issue:** 26+ command handlers follow identical boilerplate pattern:
1. Get subscription by ID
2. Throw if null
3. Call entity method
4. Save changes

```csharp
// ActivateSubscriptionCommandHandler.cs
public async Task<Unit> Handle(ActivateSubscriptionCommand request, CancellationToken ct)
{
    var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, ct);
    if (subscription == null) throw new InvalidOperationException("Subscription not found");
    subscription.Activate();
    await subscriptionRepository.UpdateAsync(subscription, ct);
    return Unit.Value;
}

// CancelSubscriptionCommandHandler.cs - SAME PATTERN
public async Task<Unit> Handle(CancelSubscriptionCommand request, CancellationToken ct)
{
    var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, ct);
    if (subscription == null) throw new InvalidOperationException("Subscription not found");
    subscription.Cancel(request.Reason, request.Note, request.EffectiveDate);
    await subscriptionRepository.UpdateAsync(subscription, ct);
    return Unit.Value;
}

// SuspendSubscriptionCommandHandler.cs - SAME PATTERN
// SetSubscriptionAutoRenewCommandHandler.cs - SAME PATTERN
// UpdateSubscriptionMetadataCommandHandler.cs - SAME PATTERN
// ... 20+ more handlers with identical structure
```

**Recommendation:** Create a base handler or use a pipeline behavior for common entity retrieval:

```csharp
public abstract class SubscriptionCommandHandler<TCommand> : ICommandHandler<TCommand>
    where TCommand : ISubscriptionCommand
{
    protected readonly ISubscriptionRepository Repository;
    
    protected async Task<Subscription> GetSubscriptionOrThrowAsync(Guid id, CancellationToken ct)
    {
        var subscription = await Repository.GetByIdAsync(id, ct);
        return subscription ?? throw new NotFoundException($"Subscription {id} not found");
    }
}
```

---

### DRY-3: Duplicated Webhook Processing Pattern (MEDIUM)

**Files:**
- [StripeBillingWebhookService.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Services/StripeBillingWebhookService.cs#L33-L87)
- [PayPalBillingWebhookService.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Services/PayPalBillingWebhookService.cs#L40-L114)
- [ApplePayBillingWebhookService.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Services/ApplePayBillingWebhookService.cs#L33-L98)

**Issue:** All three services have identical webhook processing flow:
1. Check for duplicate event (idempotency)
2. Create webhook event record
3. Store event before processing
4. Route to handler
5. Mark as processed/failed
6. Return result

```csharp
// Stripe - 50 lines
var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, "stripe", ct);
if (existingEvent != null) return WebhookProcessingResult.AlreadyProcessed(...);
var webhookEvent = new BillingWebhookEvent { ... };
webhookEvent = await _webhookRepository.CreateAsync(webhookEvent, ct);
try { /* process */ webhookEvent.MarkAsProcessed(); }
catch { webhookEvent.MarkAsFailed(ex.Message); }
await _webhookRepository.UpdateAsync(webhookEvent, ct);

// PayPal - IDENTICAL 50 lines with different provider string
// ApplePay - IDENTICAL 50 lines with different provider string
```

**Recommendation:** Use Template Method pattern in base class:

```csharp
protected async Task<WebhookProcessingResult> ProcessWebhookAsync(
    string eventId,
    string provider,
    string payload,
    Func<Task> processAction,
    CancellationToken ct)
{
    var existing = await _webhookRepository.GetByExternalEventIdAsync(eventId, provider, ct);
    if (existing != null) return WebhookProcessingResult.AlreadyProcessed(eventId, existing.ProcessedAt);
    
    var webhookEvent = await CreateAndStoreEventAsync(eventId, provider, payload, ct);
    try
    {
        await processAction();
        webhookEvent.MarkAsProcessed();
    }
    catch (Exception ex)
    {
        webhookEvent.MarkAsFailed(ex.Message);
        throw;
    }
    await _webhookRepository.UpdateAsync(webhookEvent, ct);
    return WebhookProcessingResult.Success(eventId);
}
```

---

### DRY-4: Repeated Repository Query Patterns (LOW)

**Files:**
- [PaymentRepository.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Payments/Repositories/PaymentRepository.cs)
- [SubscriptionRepository.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Subscriptions/Repositories/SubscriptionRepository.cs)
- [OrderRepository.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Orders/Repositories/OrderRepository.cs)

**Issue:** All repositories repeat the same filtering patterns:
- `DeletedAt == null` (soft delete filter)
- `OrderByDescending(x => x.CreatedAt)`
- `Include(x => x.Plan)` / `Include(o => o.LineItems)`

**Recommendation:** Use base repository with common query methods or Specification pattern (already partially implemented for Subscriptions).

---

## 2. SOLID Principle Violations

### SOLID-1: Single Responsibility Principle - Product Entity (MEDIUM)

**File:** [Product.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Products/Entities/Product.cs)

**Issue:** Product entity has 294 lines with multiple responsibilities:
- Product data management
- Bundle item management (deprecated + new)
- Commission configuration (deprecated + new)
- Creator relationship management
- JSON serialization/deserialization

**Recommendation:** Already partially addressed with `ProductCommissionConfig` and `ProductBundleItem` extraction. Complete the migration by:
1. Removing deprecated properties entirely in next major version
2. Moving bundle validation logic to a domain service

---

### SOLID-2: Single Responsibility Principle - Subscription Entity (MEDIUM)

**File:** [Subscription.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Subscriptions/Entities/Subscription.cs)

**Issue:** 650-line entity with responsibilities including:
- State machine management
- Billing date calculations
- Proration calculations
- Idempotency tracking
- Metadata management
- Domain events

**Recommendation:** Extract:
- `BillingCalculator` service for date/proration calculations
- Consider CQRS read model for complex queries

---

### SOLID-3: Interface Segregation Principle - ISubscriptionService (MEDIUM)

**File:** [ISubscriptionService.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Subscriptions/Abstractions/ISubscriptionService.cs)

**Issue:** Interface has too many methods mixing different concerns:
- Creation operations
- State transitions
- Query operations
- External ID management

**Recommendation:** Split into focused interfaces:
- `ISubscriptionLifecycleService` - create, activate, cancel, suspend
- `ISubscriptionQueryService` - get by ID, get by tenant, etc.
- `ISubscriptionExternalIdService` - set/get external IDs

---

### SOLID-4: Dependency Inversion - Direct Entity References (LOW)

**Files:** Multiple command handlers

**Issue:** Handlers throw `InvalidOperationException` directly instead of using a domain exception type:

```csharp
if (subscription == null) 
    throw new InvalidOperationException("Subscription not found");
```

**Recommendation:** Use domain-specific exceptions:
```csharp
if (subscription == null) 
    throw new SubscriptionNotFoundException(request.SubscriptionId);
```

---

## 3. KISS (Keep It Simple, Stupid) Violations

### KISS-1: Complex Webhook Payload Hierarchy (LOW)

**Files:**
- `SubscriptionWebhookPayload.cs` (abstract)
- `PaymentWebhookPayload.cs` (abstract)
- `StripeSubscriptionWebhookPayload.cs`
- `StripePaymentWebhookPayload.cs`
- `PayPalSubscriptionWebhookPayload.cs`
- `PayPalPaymentWebhookPayload.cs`
- `ApplePaySubscriptionWebhookPayload.cs`
- `ApplePayPaymentWebhookPayload.cs`

**Issue:** 8 classes for 2 concepts (subscription/payment) × 3 providers when they all have nearly identical properties.

**Recommendation:** Use a single `WebhookPayload` record with provider discriminator:
```csharp
public record WebhookPayload(
    string Provider,
    PayloadType Type,
    Guid TenantId,
    Guid? PlanId,
    string ExternalSubscriptionId,
    // ... common properties
);
```

---

### KISS-2: Unnecessary Abstraction - BillingConfiguration (LOW)

**File:** [BillingConfiguration.cs](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Configuration/BillingConfiguration.cs)

**Issue:** Abstract class with no implementation or shared behavior. Just a marker.

**Recommendation:** Remove if not used, or convert to interface if needed for polymorphism.

---

## 4. YAGNI (You Aren't Gonna Need It) Violations

### YAGNI-1: Unused/Speculative Features (LOW)

**Files:** Multiple configuration files

**Issue:** Some configuration classes have properties that don't appear to be used:
- `WebhookSettings.RetryPolicy` - no retry implementation uses it
- Webhook settings for providers not yet integrated

**Recommendation:** Remove unused configuration properties until needed.

---

## 5. Other Code Smells

### CS-1: Inconsistent Logging Patterns (MEDIUM)

**Files:** All service and handler files

**Issue:** Inconsistent use of `_logger` vs `logger` (primary constructor):

```csharp
// Some files use field
private readonly ILogger<Foo> _logger;
_logger.LogInformation(...);

// Others use primary constructor parameter
public class Bar(ILogger<Bar> logger)
{
    logger.LogInformation(...);  // No underscore
}
```

**Recommendation:** Standardize on one pattern (prefer primary constructor without underscore for modern C#).

---

### CS-2: Magic Strings for Providers (MEDIUM)

**Files:** Webhook services and repositories

**Issue:** Provider names are hardcoded strings throughout:

```csharp
await _webhookRepository.GetByExternalEventIdAsync(eventId, "stripe", ct);
await _webhookRepository.GetByExternalEventIdAsync(eventId, "paypal", ct);
await _webhookRepository.GetByExternalEventIdAsync(eventId, "apple_app_store", ct);
```

**Recommendation:** Use constants or enum:
```csharp
public static class PaymentProviders
{
    public const string Stripe = "stripe";
    public const string PayPal = "paypal";
    public const string AppleAppStore = "apple_app_store";
}
```

---

### CS-3: Missing Cancellation Token Forwarding (LOW)

**Files:** Some repository methods

**Issue:** Some methods don't use `.ConfigureAwait(false)` or forward `CancellationToken`:

```csharp
// OrderRepository.cs - inconsistent ConfigureAwait usage
return await Orders
    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
    .ConfigureAwait(false);  // Sometimes present

return await Orders
    .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);  // Sometimes missing
```

**Recommendation:** Consistently apply `.ConfigureAwait(false)` in library code.

---

### CS-4: Data Clumps - Webhook Processing Parameters (LOW)

**Files:** Webhook command handlers

**Issue:** Webhook commands pass many related parameters:

```csharp
public record ProcessPayPalWebhookCommand(
    string WebhookId,
    string Payload,
    string TransmissionId,
    string TransmissionTime,
    string TransmissionSig,
    string? CertUrl,
    string? AuthAlgo
) : ICommand<WebhookProcessingResult>;
```

**Recommendation:** Extract `PayPalWebhookHeaders` value object:
```csharp
public record PayPalWebhookHeaders(
    string TransmissionId,
    string TransmissionTime,
    string TransmissionSig,
    string? CertUrl,
    string? AuthAlgo);

public record ProcessPayPalWebhookCommand(
    string WebhookId,
    string Payload,
    PayPalWebhookHeaders Headers
) : ICommand<WebhookProcessingResult>;
```

---

## 6. Recommendations Summary

### Priority 1 (High Impact, Should Fix)

| Issue | Files Affected | Effort | Impact |
|-------|----------------|--------|--------|
| DRY-1: State Machine Duplication | 2 entities | Medium | Reduces 100+ lines, enables reuse |
| DRY-2: Command Handler Boilerplate | 26+ handlers | Medium | Reduces 500+ lines |
| DRY-3: Webhook Processing Pattern | 3 services | Low | Reduces 100+ lines |

### Priority 2 (Medium Impact, Should Consider)

| Issue | Files Affected | Effort | Impact |
|-------|----------------|--------|--------|
| SOLID-2: Large Subscription Entity | 1 entity | High | Improves maintainability |
| SOLID-3: Fat Interface | 1 interface | Medium | Improves testability |
| CS-1: Logging Consistency | All services | Low | Improves readability |
| CS-2: Magic Strings | Webhook services | Low | Prevents typos |

### Priority 3 (Low Impact, Nice to Have)

| Issue | Files Affected | Effort | Impact |
|-------|----------------|--------|--------|
| KISS-1: Payload Hierarchy | 8 classes | Medium | Simplifies code |
| DRY-4: Repository Patterns | 3 repositories | Medium | Minor improvement |
| CS-3: ConfigureAwait | Multiple | Low | Consistency |

---

## Metrics

| Metric | Current | Target |
|--------|---------|--------|
| Average Entity Lines | 400 | < 200 |
| Duplicate Code Blocks | 15+ | < 5 |
| Command Handler Boilerplate | 26 identical patterns | 1 base class |
| Magic Strings | 10+ locations | 0 (use constants) |

---

## Conclusion

The Commerce modules are functionally complete and secure but have accumulated technical debt in the form of code duplication and oversized entities. The highest-priority improvements are:

1. **Extract State Machine Pattern** - Will benefit Order, Subscription, and future stateful entities
2. **Base Command Handler** - Will reduce 26+ handlers to minimal implementations
3. **Template Method for Webhooks** - Will simplify adding new payment providers

These improvements would reduce the codebase by approximately 700+ lines while improving maintainability and reducing the risk of inconsistencies.

---

*Last Updated: January 14, 2026*
