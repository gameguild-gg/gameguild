# Economic Model Alignment Report

**Date:** January 14, 2026  
**Scope:** GameGuild.Commerce.* (Products, Orders, Subscriptions, Billing, Payments), GameGuild.Resources, GameGuild.Features  
**Review Type:** Economic Model Alignment, Order Model Definition, Invariant Analysis  
**Status:** PARTIALLY SOUND — Critical gaps identified requiring minimal changes

---

## Executive Summary

This report defines a **UNIFIED ECONOMIC MODEL** for the GameGuild commerce system and proposes minimal, additive changes to align all modules around a single economic truth:

> **"Who is allowed to consume which resources, under which contract, after which payment, at which moment."**

### Current Assessment

| Category | Status | Notes |
|----------|--------|-------|
| Order Model | ⚠️ PARTIAL | Missing `OrderType`, `FulfilledAt`, `PendingPayment` state |
| State Machine | ✅ PASS | Monotonic FSM exists, well-designed |
| Idempotency | ✅ PASS | `IdempotencyKey` present, duplicate detection works |
| Tenant Isolation | ✅ PASS | Fail-closed TenantId guards in all financial entities |
| Order→Subscription Link | ❌ FAIL | No `FulfilledOrderId` on Subscription |
| Subscription→Quota Link | ❌ FAIL | No handler for `SubscriptionActivatedEvent` to sync quotas |
| Payment→Order Link | ⚠️ PARTIAL | `ExternalPaymentId` exists but no formal Payment entity reference |
| Economic Causality | ⚠️ PARTIAL | `CompleteOrderAsync` grants entitlements directly, bypassing subscription for one-time purchases |

---

## PART 1 — UNIFIED ECONOMIC MODEL

### 1.1 Module Responsibilities (SINGLE SOURCE OF TRUTH)

| Module | IS Responsible For | MUST NEVER Do |
|--------|-------------------|---------------|
| **Products** | Product catalog, pricing definitions, entitlements (UserProduct) | Grant entitlements without Order reference |
| **Orders** | Economic intent, price snapshots, immutable purchase records | Directly grant quota or subscription changes |
| **Payments** | Financial settlement, payment state, retry logic | Change subscription status without Order |
| **Billing** | Invoice generation, payment provider webhooks, tax calculation | Create subscriptions or quotas directly |
| **Subscriptions** | Contractual truth, plan association, billing cycle management | Grant quotas without Subscription change event |
| **Resources** | Quota enforcement, usage tracking, limit management | Grant quota without subscription/order reference |
| **Features** | Feature flag management, rollout control | Grant access without checking subscription tier |

### 1.2 What Each Module MUST NEVER Do (ADVERSARIAL ANALYSIS)

| Module | Anti-Pattern | Risk |
|--------|-------------|------|
| **Products** | `EntitlementService.GrantEntitlementAsync()` called without `orderId` | Entitlement without audit trail = financial loss |
| **Orders** | `CompleteOrderAsync()` grants entitlements before payment confirmed | Resource granted before money received |
| **Payments** | Payment success handler creates subscription directly | Bypasses Order layer, no idempotency |
| **Billing** | Webhook handler modifies subscription without Payment record | Duplicate webhooks cause duplicate state changes |
| **Subscriptions** | `CreateSubscriptionCommand` runs without prior fulfilled Order | Subscription granted without payment |
| **Resources** | `SetQuotaAsync()` called without subscription reference | Quotas out of sync with entitlements |
| **Features** | Feature access checked without tenant context | Cross-tenant feature leakage |

### 1.3 Causal Order of Operations (CANONICAL FLOW)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ECONOMIC CAUSALITY CHAIN                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  1. ORDER CREATED                                                           │
│     ├─ IdempotencyKey prevents duplicates                                   │
│     ├─ TenantId required (fail-closed)                                      │
│     ├─ Price snapshot captured                                              │
│     └─ Status: Pending                                                      │
│                         │                                                   │
│                         ▼                                                   │
│  2. PAYMENT INITIATED                                                       │
│     ├─ Payment entity created with OrderId reference                        │
│     ├─ IdempotencyKey for payment deduplication                             │
│     └─ Status: Pending → Processing                                         │
│                         │                                                   │
│                         ▼                                                   │
│  3. PAYMENT SUCCEEDED (via webhook or sync)                                 │
│     ├─ Payment status: Succeeded                                            │
│     ├─ Order status: Completed                                              │
│     └─ Order.PaidAt timestamp set                                           │
│                         │                                                   │
│                         ▼                                                   │
│  4. ORDER FULFILLED                                                         │
│     ├─ Entitlements granted (UserProduct created)                           │
│     ├─ UserProduct.OrderId references the Order                             │
│     ├─ For subscriptions: Subscription created/activated                    │
│     │   └─ Subscription.FulfilledOrderId references the Order               │
│     └─ Status: Fulfilled (NEW STATE PROPOSED)                               │
│                         │                                                   │
│                         ▼                                                   │
│  5. SUBSCRIPTION ACTIVATED (if applicable)                                  │
│     ├─ Subscription status: Active                                          │
│     ├─ SubscriptionActivatedEvent raised                                    │
│     └─ Event contains TenantId, PlanId                                      │
│                         │                                                   │
│                         ▼                                                   │
│  6. QUOTAS RECALCULATED                                                     │
│     ├─ Handler listens for SubscriptionActivatedEvent                       │
│     ├─ Quotas derived from SubscriptionPlan limits                          │
│     │   └─ MaxUsers, MaxStorageMb, MaxApiCallsPerMonth                      │
│     └─ ResourceQuota entities created/updated for TenantId                  │
│                         │                                                   │
│                         ▼                                                   │
│  7. RESOURCES ENFORCED                                                      │
│     ├─ Commands with [RequiresQuota] checked against quotas                 │
│     ├─ TryAtomicConsumeAsync for thread-safe enforcement                    │
│     └─ Denial logged if quota exceeded                                      │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 1.4 Why Skipping Steps Is Unsafe

| Skipped Step | Economic Risk | Attack Vector |
|--------------|--------------|---------------|
| Skip Order creation | No audit trail, no idempotency | User claims they didn't purchase |
| Skip Payment verification | Grant access before payment clears | Payment fails after entitlement granted |
| Skip Order completion | Payment received but no fulfillment | Money taken, nothing delivered |
| Skip Subscription creation | Quota granted without contract | User has unlimited resources without paying |
| Skip Quota recalculation | Old plan limits apply to new plan | User downgrades but keeps high limits |
| Skip Resource enforcement | Over-consumption not prevented | DoS via resource exhaustion |

---

## PART 2 — ORDER MODEL DEFINITION

### 2.1 Order Purpose

**What an Order Represents:**
- **Economic Intent**: A formal declaration of what the buyer wants to purchase
- **Price Lock**: An immutable snapshot of prices at the moment of purchase
- **Audit Trail**: A permanent record linking User → Products → Payment → Entitlements
- **Idempotency Anchor**: The single source of truth for "did this purchase happen?"

**What an Order Does NOT Represent:**
- **Payment**: Order ≠ Payment. Payment is a separate entity/transaction
- **Subscription**: Order creates subscriptions, but is not the subscription itself
- **Entitlement**: Order leads to entitlements, but doesn't directly grant access
- **Invoice**: Invoices are billing artifacts, Orders are commerce artifacts

**Why Order Must Exist Separately:**

| Concern | Order | Payment | Subscription |
|---------|-------|---------|--------------|
| Created at | User intent | Payment initiated | Order fulfilled |
| Mutable? | Immutable after creation | State changes only | State changes, plan changes |
| Idempotency scope | Purchase attempt | Payment attempt | Contract lifecycle |
| Contains money? | Price snapshot (no movement) | Actual fund transfer | Billing amount per cycle |
| Tenant relationship | Direct FK | Via Order or Subscription | Direct FK |

### 2.2 Current Order Aggregate (Analysis)

```csharp
// Current Order Entity (Order.cs lines 1-276)
public class Order : EntityBase
{
    // ✅ Present and correct
    public Guid UserId { get; set; }               // Owner
    public string IdempotencyKey { get; set; }     // Duplicate prevention
    public OrderStatus Status { get; set; }        // State machine
    public decimal Subtotal/DiscountTotal/Total { get; set; }  // Price snapshot
    public string? ExternalPaymentId { get; set; } // Payment reference
    public DateTime? PaidAt { get; set; }          // Completion timestamp
    public Guid TenantId { get; }                  // Inherited, fail-closed
    
    // ✅ Correct relationships
    public virtual ICollection<OrderLineItem> LineItems { get; set; }
    
    // ❌ MISSING
    // - OrderType (Subscribe, Upgrade, Downgrade, AddOn, OneTimePurchase)
    // - TargetSubscriptionId (for upgrades/downgrades)
    // - FulfilledAt (separate from PaidAt)
    // - PaymentId (FK to Payment entity)
}
```

### 2.3 Proposed Order Aggregate (MINIMAL ADDITIONS)

```csharp
/// <summary>
/// Proposed additions to Order entity - ADDITIVE ONLY
/// </summary>
public class Order : EntityBase
{
    // ═══════════════════════════════════════════════════════════════
    // NEW PROPERTIES (Additive)
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Type of order - determines fulfillment logic
    /// </summary>
    public OrderType OrderType { get; set; } = OrderType.OneTimePurchase;
    
    /// <summary>
    /// Target subscription ID for upgrade/downgrade orders
    /// </summary>
    public Guid? TargetSubscriptionId { get; set; }
    
    /// <summary>
    /// When fulfillment was completed (entitlements granted)
    /// Distinct from PaidAt - payment can succeed before fulfillment completes
    /// </summary>
    public DateTime? FulfilledAt { get; private set; }
    
    /// <summary>
    /// Foreign key to Payment entity for reconciliation
    /// </summary>
    public Guid? PaymentId { get; private set; }
    
    // ═══════════════════════════════════════════════════════════════
    // NEW METHODS
    // ═══════════════════════════════════════════════════════════════
    
    /// <summary>
    /// Marks order as fulfilled after entitlements are granted
    /// </summary>
    public void MarkAsFulfilled()
    {
        if (Status != OrderStatus.Completed)
            throw new InvalidOperationException("Order must be Completed before fulfillment");
        FulfilledAt = DateTime.UtcNow;
        Touch();
    }
    
    /// <summary>
    /// Associates a Payment entity with this order
    /// </summary>
    public void AssociatePayment(Guid paymentId)
    {
        if (PaymentId.HasValue && PaymentId != paymentId)
            throw new InvalidOperationException($"Order already has payment {PaymentId}");
        PaymentId = paymentId;
        Touch();
    }
}

/// <summary>
/// NEW ENUM - Order type classification
/// </summary>
public enum OrderType
{
    /// <summary>One-time product purchase</summary>
    OneTimePurchase = 0,
    
    /// <summary>New subscription creation</summary>
    Subscribe = 1,
    
    /// <summary>Subscription upgrade to higher tier</summary>
    Upgrade = 2,
    
    /// <summary>Subscription downgrade to lower tier</summary>
    Downgrade = 3,
    
    /// <summary>Add-on purchase for existing subscription</summary>
    AddOn = 4,
    
    /// <summary>Subscription renewal (automated or manual)</summary>
    Renewal = 5
}
```

### 2.4 Order State Machine (EXPLICIT FSM)

#### Current State Machine (from code):

```
┌────────────────────────────────────────────────────────────────────────────┐
│                         CURRENT ORDER FSM                                   │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│                            ┌──────────┐                                    │
│                            │ Pending  │ ◄── Initial state                  │
│                            └────┬─────┘                                    │
│                                 │                                          │
│              ┌──────────────────┼──────────────────┐                       │
│              │                  │                  │                       │
│              ▼                  ▼                  ▼                       │
│      ┌───────────┐      ┌───────────┐      ┌──────────┐                    │
│      │Processing │      │ Completed │      │  Failed  │ ◄── Terminal      │
│      └─────┬─────┘      └─────┬─────┘      └──────────┘                    │
│            │                  │                                            │
│     ┌──────┼──────┐           │                                            │
│     │      │      │           │                                            │
│     ▼      ▼      ▼           ▼                                            │
│ ┌───────┐ ┌────┐ ┌────────┐  ┌────────────────┐                            │
│ │Compltd│ │Fail│ │Canceld │  │ Refunded/      │                            │
│ └───────┘ └────┘ └────────┘  │ PartialRefund/ │                            │
│                              │ Disputed       │                            │
│                              └────────────────┘                            │
│                                                                            │
│  ╔═══════════════════════════════════════════════════════════════════╗    │
│  ║  ISSUE: "Completed" conflates payment + fulfillment               ║    │
│  ║  ISSUE: No "PendingPayment" state for async payment flows         ║    │
│  ╚═══════════════════════════════════════════════════════════════════╝    │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

#### Proposed State Machine (EXTENDED):

```
┌────────────────────────────────────────────────────────────────────────────┐
│                       PROPOSED ORDER FSM (Extended)                         │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                            │
│                         ┌─────────┐                                        │
│                         │  Draft  │ ◄── Optional: cart/preview state       │
│                         └────┬────┘                                        │
│                              │ Confirm                                     │
│                              ▼                                             │
│                      ┌──────────────┐                                      │
│                      │   Pending    │ ◄── Order created, awaiting payment  │
│                      └──────┬───────┘                                      │
│                             │                                              │
│          ┌──────────────────┼──────────────────┐                           │
│          │                  │                  │                           │
│          ▼                  ▼                  ▼                           │
│  ┌───────────────┐  ┌───────────────┐  ┌──────────┐                        │
│  │PendingPayment │  │   Cancelled   │  │  Failed  │                        │
│  │ (NEW STATE)   │  │   (terminal)  │  │(terminal)│                        │
│  └───────┬───────┘  └───────────────┘  └──────────┘                        │
│          │                                                                 │
│          │ Payment initiated                                               │
│          ▼                                                                 │
│  ┌───────────────┐                                                         │
│  │  Processing   │ ◄── Payment in progress                                 │
│  └───────┬───────┘                                                         │
│          │                                                                 │
│   ┌──────┴──────┐                                                          │
│   │             │                                                          │
│   ▼             ▼                                                          │
│ ┌────┐   ┌──────────┐                                                      │
│ │Paid│   │  Failed  │                                                      │
│ │NEW │   └──────────┘                                                      │
│ └──┬─┘                                                                     │
│    │ Entitlements granted                                                  │
│    ▼                                                                       │
│ ┌───────────┐                                                              │
│ │ Fulfilled │ ◄── Entitlements/subscriptions created (NEW STATE)           │
│ │  (NEW)    │                                                              │
│ └─────┬─────┘                                                              │
│       │                                                                    │
│       ▼                                                                    │
│ ┌──────────────────┐                                                       │
│ │Refunded/Disputed │ ◄── Post-fulfillment reversals                        │
│ │  PartialRefund   │                                                       │
│ └──────────────────┘                                                       │
│                                                                            │
│  FORBIDDEN TRANSITIONS (Backward economic movements):                      │
│  ❌ Paid → Pending           ❌ Fulfilled → Processing                     │
│  ❌ Fulfilled → Paid         ❌ Refunded → Paid                             │
│  ❌ Cancelled → Pending      ❌ Failed → Processing                         │
│                                                                            │
└────────────────────────────────────────────────────────────────────────────┘
```

#### State Transition Table (FORMAL):

| From State | To State | Trigger | Side Effects | Idempotent? |
|------------|----------|---------|--------------|-------------|
| Draft | Pending | `Confirm()` | Validates line items | Yes |
| Pending | PendingPayment | `InitiatePayment()` | Creates Payment entity | Yes |
| Pending | Cancelled | `Cancel(reason)` | None | Yes |
| Pending | Failed | `MarkAsFailed(reason)` | None | Yes |
| PendingPayment | Processing | `PaymentInitiated()` | None | Yes |
| Processing | Paid | `MarkAsPaid(paymentId)` | Sets PaidAt, ExternalPaymentId | Yes |
| Processing | Failed | `MarkAsFailed(reason)` | None | Yes |
| Processing | Cancelled | `Cancel(reason)` | Releases payment hold | Yes |
| Paid | Fulfilled | `MarkAsFulfilled()` | Sets FulfilledAt, creates entitlements | Yes (noop if already fulfilled) |
| Fulfilled | Refunded | `ProcessRefund(full)` | Revokes entitlements, sets RefundedAt | Yes |
| Fulfilled | PartiallyRefunded | `ProcessRefund(partial)` | Partial revocation | No (accumulates) |
| Fulfilled | Disputed | `MarkAsDisputed()` | Suspends entitlements | Yes |
| PartiallyRefunded | Refunded | `ProcessRefund(remaining)` | Full revocation | Yes |
| Disputed | Fulfilled | `ResolveDispute(in_favor)` | Restores entitlements | Yes |
| Disputed | Refunded | `ResolveDispute(chargeback)` | Revokes entitlements | Yes |

---

## PART 3 — ECONOMIC INVARIANTS

### 3.1 Non-Negotiable Invariants

| # | Invariant | Current Status | Evidence |
|---|-----------|----------------|----------|
| 1 | **No financial entity exists without valid TenantId** | ✅ PASS | `Order.Create()`, `Invoice()`, `Subscription()` all throw on empty TenantId |
| 2 | **Orders are immutable after creation (except state transitions)** | ✅ PASS | `RecalculateTotals()` throws if Status != Pending/Processing |
| 3 | **Orders are idempotent (IdempotencyKey enforced)** | ✅ PASS | `GetByIdempotencyKeyAsync()` check in `CreateOrderAsync()` |
| 4 | **No subscription change without a fulfilled order** | ❌ FAIL | `CreateSubscriptionCommand` has no `FulfilledOrderId` parameter |
| 5 | **No quota change without a subscription change** | ❌ FAIL | No handler for `SubscriptionActivatedEvent` → quota sync |
| 6 | **No resource creation without quota allowance** | ⚠️ PARTIAL | `[RequiresQuota]` attribute exists but not on all create commands |
| 7 | **No economic effect without tenant context** | ✅ PASS | Fail-closed guards throughout |
| 8 | **Payment webhook processing is idempotent** | ✅ PASS | `GetByExternalEventIdAsync()` prevents duplicate processing |
| 9 | **Subscription renewals are idempotent** | ✅ PASS | `LastRenewalIdempotencyKey` check in `ProcessRenewal()` |
| 10 | **Economic state transitions are monotonic** | ✅ PASS | FSM prevents backward transitions |
| 11 | **Single payment per invoice** | ✅ PASS | `Invoice.PaymentId` unique constraint |
| 12 | **Invoice amounts immutable after issuance** | ✅ PASS | `EnsureMutable()` checks in Invoice |

### 3.2 Invariant Gaps (Critical Findings)

#### GAP 1: Subscription Creation Without Order

**Current Code Path:**
```csharp
// CreateSubscriptionCommand.cs - NO OrderId reference
public record CreateSubscriptionCommand(
    Guid TenantId, 
    Guid PlanId, 
    Guid CreatedByUserId, 
    BillingCycle BillingCycle, 
    decimal Amount,
    ...
) : ICommand<Guid>;
// ❌ No FulfilledOrderId required!
```

**Attack Vector:** Admin or compromised code can call `CreateSubscriptionCommand` directly, granting subscription without payment.

**Fix Required:** Add `required Guid? FulfilledOrderId` to command, validate order exists and is Fulfilled.

#### GAP 2: Quota Disconnected from Subscription

**Current Flow:**
1. `Subscription.Activate()` raises `SubscriptionActivatedEvent`
2. **No handler exists** to sync quotas from `SubscriptionPlan.MaxUsers/MaxStorageMb/MaxApiCallsPerMonth`

**Attack Vector:** User upgrades to higher tier, gets subscription activated, but quotas remain at old tier limits.

**Fix Required:** Create `SubscriptionActivatedEventHandler` that reads plan limits and calls `IResourceQuotaService.SetQuotaAsync()`.

#### GAP 3: EntitlementService Allows No-Order Grants

**Current Code:**
```csharp
// EntitlementService.cs line 69
public async Task<EntitlementResult> GrantEntitlementAsync(
    Guid userId,
    Guid productId,
    ...
    Guid? orderId = null,  // ❌ OPTIONAL!
    ...)
```

**Attack Vector:** Any code can call `GrantEntitlementAsync` without an order, creating unauditable entitlements.

**Fix Required:** Make `orderId` required or add `[Obsolete]` warning for null usage.

---

## PART 4 — MODULE INTEGRATION

### 4.1 Order ↔ Payment Integration

**Current State:**
- Order has `ExternalPaymentId` (string from payment gateway)
- Payment has `OrderId` (FK to Order)
- Order does not have `PaymentId` (FK to Payment entity)

**Proposed Flow:**
```
1. Order created (Status: Pending)
2. Payment.Create() called with OrderId reference
3. Order.AssociatePayment(paymentId) called
4. Payment gateway called
5. Webhook received → Payment.MarkAsSucceeded()
6. Order.MarkAsPaid() triggered
7. Order.MarkAsFulfilled() after entitlements granted
```

**Mutation Rights:**

| Entity | Can Be Mutated By | Forbidden Mutations |
|--------|-------------------|---------------------|
| Order | OrderService only | Direct status changes without FSM |
| Payment | PaymentService, Webhook handlers | Amount changes after creation |
| Invoice | BillingService only | Amount changes after issuance |

### 4.2 Order ↔ Subscription Integration

**Current State:**
- `OrderLineItem.SubscriptionPlanId` exists
- `UserProduct.SubscriptionId` exists
- `Subscription` has no `OriginatingOrderId` or `FulfilledOrderId`

**Proposed Flow:**
```
1. Order with OrderType=Subscribe fulfilled
2. CreateSubscriptionCommand called with FulfilledOrderId
3. Subscription created with reference back to Order
4. SubscriptionActivatedEvent raised
```

### 4.3 Subscription ↔ Quota Integration

**Current State:**
- `SubscriptionPlan` has `MaxUsers`, `MaxStorageMb`, `MaxApiCallsPerMonth`
- `ResourceQuota` has `Type`, `SoftLimit`, `HardLimit`
- **NO LINK between them**

**Proposed Flow:**
```
1. SubscriptionActivatedEvent raised
2. SubscriptionActivatedEventHandler receives event
3. Handler loads SubscriptionPlan for subscription
4. Handler calls IResourceQuotaService.SetQuotaAsync() for each limit:
   - MaxUsers → ResourceUsageType.Users
   - MaxStorageMb → ResourceUsageType.Storage
   - MaxApiCallsPerMonth → ResourceUsageType.ApiCalls
5. Quotas now match subscription tier
```

### 4.4 Asynchronous vs Synchronous Operations

| Operation | Sync/Async | Reason |
|-----------|------------|--------|
| Order creation | Sync | Must return Order ID immediately |
| Payment initiation | Sync | Must return payment intent/URL |
| Payment confirmation | **Async** (webhook) | Payment gateway callback |
| Fulfillment | Sync (within webhook handler) | Must complete atomically |
| Quota sync | Async (event handler) | Can tolerate slight delay |
| Usage recording | Async | Non-blocking for performance |

---

## PART 5 — FAILURE & ATTACK SCENARIOS

### Scenario 1: Payment Webhook Retry

**Situation:** Stripe sends `payment_intent.succeeded` webhook. Network timeout. Stripe retries 3 times.

**Expected Behavior:**
1. First webhook: Payment marked Succeeded, Order marked Paid/Fulfilled
2. Second webhook: `GetByExternalEventIdAsync()` returns existing event → skip
3. Third webhook: Same → skip

**Current Code Behavior:** ✅ CORRECT
```csharp
// StripeBillingWebhookService.cs
var existingEvent = await _webhookRepository.GetByExternalEventIdAsync(eventId, "stripe", ct);
if (existingEvent != null) return WebhookProcessingResult.AlreadyProcessed(...);
```

**Economic Risk:** LOW (protected)

---

### Scenario 2: Order Processed Twice

**Situation:** Client double-clicks "Pay" button, sending two requests with same IdempotencyKey.

**Expected Behavior:**
1. First request: Creates order, initiates payment
2. Second request: Returns existing order (idempotent)

**Current Code Behavior:** ✅ CORRECT
```csharp
// OrderService.cs CreateOrderAsync()
var existingOrder = await orderRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct);
if (existingOrder != null) return OrderResult.Succeeded(existingOrder, wasDuplicate: true);
```

**Economic Risk:** LOW (protected)

---

### Scenario 3: Upgrade + Downgrade Race Condition

**Situation:** User on "Pro" plan. Admin clicks "Upgrade to Enterprise" while user clicks "Downgrade to Free" simultaneously.

**Expected Behavior:**
1. Both operations should require separate Orders
2. Only one Order can be Fulfilled for the subscription at a time
3. Later order should fail validation (subscription already changed)

**Current Code Behavior:** ⚠️ PARTIALLY PROTECTED
- `Subscription.ChangePlan()` checks `Status == Active` but doesn't prevent concurrent changes
- No optimistic concurrency lock on subscription during plan change

**Economic Risk:** MEDIUM
- User could end up on wrong plan
- Billing could be inconsistent

**Proposed Fix:**
```csharp
// Add to Subscription entity
[Timestamp]
public byte[]? RowVersion { get; set; }

// In command handler
try {
    subscription.ChangePlan(...);
    await repository.UpdateAsync(subscription, ct);
} catch (DbUpdateConcurrencyException) {
    throw new ConcurrencyException("Subscription was modified by another operation");
}
```

---

### Scenario 4: Subscription Cancellation During Payment

**Situation:** User initiates payment for renewal. While payment is processing, admin cancels subscription.

**Expected Behavior:**
1. Payment succeeds at gateway level
2. Webhook arrives
3. System detects subscription is Cancelled
4. Payment should be refunded or at minimum not extend subscription

**Current Code Behavior:** ❌ VULNERABLE
```csharp
// Subscription.RecordPayment() - does NOT check Status
public PaymentRecordResult RecordPayment(decimal amount, ...) {
    // ❌ No check for Cancelled status!
    LastPaymentAt = paymentDate;
    BillingCycleCount++;
    ...
}
```

**Economic Risk:** HIGH
- User gets charged for cancelled subscription
- Must issue manual refund
- Legal/compliance issue

**Proposed Fix:**
```csharp
public PaymentRecordResult RecordPayment(decimal amount, ...) {
    if (Status == SubscriptionStatus.Cancelled)
        return PaymentRecordResult.RejectedCancelled(
            "Cannot record payment for cancelled subscription");
    // ... rest of method
}
```

---

### Scenario 5: Tenant Context Mismatch

**Situation:** Malicious actor intercepts webhook and modifies TenantId in payload.

**Expected Behavior:**
1. Webhook should validate TenantId against existing entities
2. Mismatch should be rejected

**Current Code Behavior:** ⚠️ DEPENDS ON IMPLEMENTATION
- Entity constructors validate TenantId not empty
- Webhook handlers should cross-check TenantId

**Economic Risk:** MEDIUM (if webhook handlers don't validate)

**Proposed Fix:** Add tenant validation in webhook processor base:
```csharp
protected async Task ValidateTenantContextAsync(Guid tenantId, Guid subscriptionId, CancellationToken ct) {
    var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId, ct);
    if (subscription?.TenantId != tenantId)
        throw new TenantMismatchException($"Subscription {subscriptionId} belongs to different tenant");
}
```

---

### Scenario 6: Partial Failure During Fulfillment

**Situation:** Order has 3 line items. First 2 entitlements granted successfully. Third fails (product deleted).

**Expected Behavior:**
1. Transaction should roll back all entitlements
2. Order remains in Paid state (not Fulfilled)
3. Error logged for investigation
4. Retry or manual intervention

**Current Code Behavior:** ✅ CORRECT
```csharp
// OrderService.cs CompleteOrderAsync()
await using var transaction = await dbContext.BeginTransactionAsync(ct);
try {
    foreach (var lineItem in order.LineItems) {
        var entitlementResult = await entitlementService.GrantEntitlementAsync(...);
        // ...
    }
    order.MarkAsPaid(...);
    await transaction.CommitAsync(ct);
} catch (Exception) {
    await transaction.RollbackAsync(ct);
    throw;
}
```

**Economic Risk:** LOW (protected by transaction)

---

## PART 6 — MINIMAL CHANGE PLAN

### Priority 1: Critical (Economic Safety)

| # | Change | Module/File | Type | Backward Compatible? |
|---|--------|-------------|------|---------------------|
| 1.1 | Add `OrderType` enum | Orders/Entities/OrderEnums.cs | Additive | Yes |
| 1.2 | Add `OrderType`, `TargetSubscriptionId`, `FulfilledAt`, `PaymentId` to Order | Orders/Entities/Order.cs | Additive | Yes |
| 1.3 | Add `FulfilledOrderId` to Subscription | Subscriptions/Entities/Subscription.cs | Additive | Yes |
| 1.4 | Add cancelled payment rejection in `RecordPayment()` | Subscriptions/Entities/Subscription.cs | Behavioral fix | Yes (returns new result type) |
| 1.5 | Create `SubscriptionActivatedEventHandler` for quota sync | Resources/Handlers/ | Additive | Yes |

### Priority 2: High (Audit & Traceability)

| # | Change | Module/File | Type | Backward Compatible? |
|---|--------|-------------|------|---------------------|
| 2.1 | Add `Paid` and `Fulfilled` to OrderStatus enum | Orders/Entities/OrderEnums.cs | Additive | Yes (new values) |
| 2.2 | Update FSM transitions for new states | Orders/Entities/Order.cs | Additive | Yes |
| 2.3 | Add `[Obsolete]` to `GrantEntitlementAsync` without orderId | Products/Abstractions/IEntitlementService.cs | Warning | Yes |
| 2.4 | Add `RowVersion` to Subscription for concurrency | Subscriptions/Entities/Subscription.cs | Additive | Yes |

### Priority 3: Recommended (Defense in Depth)

| # | Change | Module/File | Type | Backward Compatible? |
|---|--------|-------------|------|---------------------|
| 3.1 | Add tenant validation helper in WebhookProcessorBase | Billing/Services/WebhookProcessorBase.cs | Additive | Yes |
| 3.2 | Add `OrderFulfilledEvent` domain event | Orders/Events/ | Additive | Yes |
| 3.3 | Add quota rollback on subscription downgrade/cancellation | Resources/Handlers/ | Additive | Yes |

---

## PART 7 — TEST PLAN

### 7.1 Required Tests (MUST EXIST)

| Test Category | Test Case | Expected Result |
|---------------|-----------|-----------------|
| **Order Idempotency** | Create order twice with same IdempotencyKey | Second returns existing order, `wasDuplicate: true` |
| **Payment Retry Safety** | Process same webhook 3 times | First succeeds, others return AlreadyProcessed |
| **Subscription via Order** | Create subscription without FulfilledOrderId | Throws validation exception |
| **Quota from Subscription** | Activate subscription with MaxUsers=50 | ResourceQuota.HardLimit = 50 for Users type |
| **Quota Enforcement** | Create user when quota exceeded | Returns CanProceed=false, no user created |
| **Tenant Isolation** | Access order from different tenant | Returns null or throws TenantMismatchException |
| **Cancelled Subscription Payment** | Record payment on cancelled subscription | Returns RejectedCancelled result |
| **Concurrent Plan Change** | Two simultaneous plan changes | One succeeds, other throws ConcurrencyException |
| **Fulfillment Rollback** | Grant 2/3 entitlements, third fails | All rolled back, order remains Paid |
| **FSM Enforcement** | Transition Fulfilled → Paid | Throws InvalidStateTransitionException |

### 7.2 Integration Test Scenarios

```csharp
[Fact]
public async Task Complete_Purchase_Flow_Creates_Order_Payment_Entitlement()
{
    // Arrange
    var order = await CreateOrder(OrderType.Subscribe, planId);
    var payment = await InitiatePayment(order.Id);
    
    // Act
    await SimulateWebhook("payment_intent.succeeded", payment.ExternalId);
    
    // Assert
    var updatedOrder = await GetOrder(order.Id);
    updatedOrder.Status.Should().Be(OrderStatus.Fulfilled);
    updatedOrder.PaymentId.Should().Be(payment.Id);
    updatedOrder.FulfilledAt.Should().NotBeNull();
    
    var subscription = await GetSubscriptionByOrderId(order.Id);
    subscription.Should().NotBeNull();
    subscription.FulfilledOrderId.Should().Be(order.Id);
    subscription.Status.Should().Be(SubscriptionStatus.Active);
    
    var quota = await GetQuota(tenantId, ResourceUsageType.Users);
    quota.HardLimit.Should().Be(expectedPlanMaxUsers);
}

[Fact]
public async Task Downgrade_Reduces_Quotas()
{
    // Arrange
    var subscription = await CreateActiveSubscription(enterprisePlanId);
    var initialQuota = await GetQuota(tenantId, ResourceUsageType.Users);
    
    // Act
    var downgradeOrder = await CreateOrder(OrderType.Downgrade, freePlanId, subscription.Id);
    await CompletePayment(downgradeOrder.Id);
    
    // Assert
    var newQuota = await GetQuota(tenantId, ResourceUsageType.Users);
    newQuota.HardLimit.Should().BeLessThan(initialQuota.HardLimit);
}
```

---

## PART 8 — FINAL ASSESSMENT

### Is the System Economically Sound?

## **PARTIALLY** — with identified gaps requiring minimal fixes

### Summary

| Area | Status | Risk Level |
|------|--------|------------|
| Order Idempotency | ✅ Sound | Low |
| Payment Idempotency | ✅ Sound | Low |
| Tenant Isolation | ✅ Sound | Low |
| Order FSM | ⚠️ Incomplete | Medium |
| Subscription-Order Link | ❌ Missing | **High** |
| Subscription-Quota Link | ❌ Missing | **High** |
| Concurrent Mutation Protection | ⚠️ Partial | Medium |
| Cancelled Subscription Payment | ❌ Vulnerable | **High** |

### Required Actions (Minimal Set)

1. **Add `FulfilledOrderId` to Subscription** — Prevents subscription creation without payment
2. **Create quota sync handler** — Ensures quotas match subscription tier
3. **Add cancelled payment rejection** — Prevents charging cancelled subscriptions
4. **Add concurrency protection** — Prevents race conditions on subscription changes

### Estimated Effort

| Priority | Changes | Effort |
|----------|---------|--------|
| Critical (P1) | 5 changes | 2-3 days |
| High (P2) | 4 changes | 1-2 days |
| Recommended (P3) | 3 changes | 1 day |

**Total: 4-6 days of focused work**

---

## Appendix A: Module Dependency Graph

```
                    ┌──────────────┐
                    │   Products   │
                    │ (Catalog +   │
                    │ Entitlements)│
                    └──────┬───────┘
                           │
                           ▼
┌──────────┐       ┌──────────────┐       ┌──────────────┐
│ Billing  │◄──────│    Orders    │──────►│  Payments    │
│(Invoices)│       │ (Intent +    │       │ (Settlement) │
└────┬─────┘       │  Snapshot)   │       └──────┬───────┘
     │             └──────┬───────┘              │
     │                    │                      │
     │                    ▼                      │
     │             ┌──────────────┐              │
     └────────────►│Subscriptions │◄─────────────┘
                   │ (Contracts)  │
                   └──────┬───────┘
                          │
                          ▼
                   ┌──────────────┐
                   │  Resources   │
                   │  (Quotas +   │
                   │   Usage)     │
                   └──────┬───────┘
                          │
                          ▼
                   ┌──────────────┐
                   │   Features   │
                   │ (Flags +     │
                   │  Rollouts)   │
                   └──────────────┘
```

---

## Appendix B: Event Flow Diagram

```
Order Created
    │
    ├─► OrderCreatedEvent (audit log)
    │
    ▼
Payment Initiated
    │
    ├─► PaymentInitiatedEvent (audit log)
    │
    ▼
Payment Succeeded (webhook)
    │
    ├─► Order.MarkAsPaid()
    │   └─► OrderStateChangedEvent (Pending → Paid)
    │
    ▼
Entitlements Granted
    │
    ├─► Order.MarkAsFulfilled()
    │   └─► OrderFulfilledEvent (proposed)
    │
    ▼
Subscription Created/Activated
    │
    ├─► SubscriptionActivatedEvent
    │   │
    │   └─► [MISSING] SubscriptionActivatedEventHandler
    │           │
    │           ▼
    │       ResourceQuotaService.SetQuotaAsync()
    │           │
    │           └─► QuotaChangedEvent
    │
    ▼
Resources Enforced
    │
    ├─► Commands with [RequiresQuota]
    │   └─► ResourceQuotaBehavior
    │       └─► TryAtomicConsumeAsync()
    │           └─► QuotaExceededEvent (if blocked)
```

---

**Document Version:** 1.0  
**Author:** Economic Model Analysis  
**Review Required By:** Platform Architecture Team, Finance Team
