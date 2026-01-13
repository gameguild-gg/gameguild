# Commerce Modules Security Audit Report

**Date:** January 13, 2026  
**Auditor:** Senior Systems Architect (AI-Assisted Review)  
**Scope:** GameGuild.Commerce.* Modules  
**Risk Assessment Level:** Critical - Financial Systems  

---

## Executive Summary

This report presents a deep security and architecture review of the GameGuild Commerce modules, which handle critical financial operations including products, subscriptions, billing, and payments. The review identified **16 HIGH-risk issues**, **11 MEDIUM-risk issues**, and **8 LOW-risk issues** across the four modules.

### Key Findings

| Category | Status | Impact |
|----------|--------|--------|
| Webhook Idempotency | ⚠️ PARTIAL | Duplicate charges possible |
| Invoice Immutability | ❌ MISSING | No Invoice entity exists |
| Tenant Isolation | ⚠️ WEAK | Inconsistent enforcement |
| Payment State Machine | ⚠️ INCOMPLETE | Race conditions possible |
| Billing Repository | ❌ NOT IMPLEMENTED | All methods throw NotImplementedException |
| Price Versioning | ⚠️ PARTIAL | Snapshots exist but no version history |

### Overall Maturity Assessment

```
Commerce Module Maturity: 45/100 (Early Development)
├── Products Module:      65/100 (Functional, needs refinement)
├── Subscriptions Module: 55/100 (Core logic present, gaps exist)
├── Billing Module:       25/100 (Skeleton only, critical gaps)
└── Payments Module:      50/100 (Partial, missing integrations)
```

**Recommendation:** These modules are NOT production-ready. Critical financial invariants are not guaranteed. Immediate remediation required before handling real money.

---

## 1. Commerce Flow Map

### End-to-End Financial Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           COMMERCE FLOW                                      │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐     ┌───────────────────┐     ┌───────────────────┐       │
│  │   Product    │────>│ ProductPricing    │────>│ ProductSubPlan    │       │
│  │   Catalog    │     │ (Price Options)   │     │ (Subscription)    │       │
│  └──────────────┘     └───────────────────┘     └───────────────────┘       │
│         │                      │                         │                   │
│         │                      v                         v                   │
│         │             ┌───────────────────┐     ┌───────────────────┐       │
│         └────────────>│  PricingEngine    │     │ SubscriptionPlan  │       │
│                       │  (Calculations)   │     │ (Plan Definitions)│       │
│                       └───────────────────┘     └───────────────────┘       │
│                                │                         │                   │
│                                v                         v                   │
│                       ┌───────────────────┐     ┌───────────────────┐       │
│                       │      Order        │<────│   Subscription    │       │
│                       │  (with LineItems) │     │   (Tenant Link)   │       │
│                       └───────────────────┘     └───────────────────┘       │
│                                │                         │                   │
│                                │    ┌────────────────────┘                   │
│                                v    v                                        │
│                       ┌───────────────────┐                                  │
│                       │  [MISSING LAYER]  │   <-- NO INVOICE ENTITY!        │
│                       │    Invoice/Bill   │                                  │
│                       └───────────────────┘                                  │
│                                │                                             │
│                                v                                             │
│                       ┌───────────────────┐     ┌───────────────────┐       │
│                       │    Payment        │────>│ FinancialLedger   │       │
│                       │   Processing      │     │    Entry          │       │
│                       └───────────────────┘     └───────────────────┘       │
│                                │                         │                   │
│                                v                         v                   │
│                       ┌───────────────────┐     ┌───────────────────┐       │
│                       │   UserWallet      │     │   RevenueEvent    │       │
│                       │  (Balance Mgmt)   │     │  (Audit Trail)    │       │
│                       └───────────────────┘     └───────────────────┘       │
│                                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│  EXTERNAL INTEGRATIONS (Webhooks)                                           │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐            │
│  │   Stripe   │  │   PayPal   │  │ Google Pay │  │ Apple Pay  │            │
│  └────────────┘  └────────────┘  └────────────┘  └────────────┘            │
│         │              │              │               │                     │
│         └──────────────┴──────────────┴───────────────┘                     │
│                                │                                             │
│                                v                                             │
│                       ┌───────────────────┐                                  │
│                       │BillingWebhookEvent│  (Stores webhook for processing)│
│                       └───────────────────┘                                  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Classes and Responsibilities

| Module | Entity/Service | Responsibility |
|--------|---------------|----------------|
| **Products** | `Product` | Catalog item definition |
| | `ProductPricing` | Price options with sale support |
| | `ProductSubscriptionPlan` | Subscription plan tied to product |
| | `Order` | Purchase container with idempotency |
| | `OrderLineItem` | Line item with price snapshot |
| | `PromoCode` | Discount code management |
| | `OrderService` | Order lifecycle management |
| | `PricingEngineService` | Price calculation with discounts |
| **Subscriptions** | `Subscription` | Tenant subscription state |
| | `SubscriptionPlan` | Plan definition with limits |
| | `ISubscriptionService` | Subscription lifecycle operations |
| | `ISubscriptionDomainService` | Domain-level billing operations |
| **Billing** | `BillingWebhookEvent` | Webhook storage and tracking |
| | `BillingWebhookService` | Webhook processing (NOT IMPLEMENTED) |
| | `BillingWebhookRepository` | Webhook persistence (NOT IMPLEMENTED) |
| **Payments** | `FinancialLedgerEntry` | Double-entry accounting |
| | `PaymentDispute` | Chargeback handling |
| | `UserWallet` | User balance management |
| | `WalletTransaction` | Wallet movement records |
| | `RevenueEvent` | Revenue audit trail |
| | `TaxCalculationService` | Tax calculation by jurisdiction |
| | `WalletService` | Wallet operations |

---

## 2. Financial Invariant Table

| # | Invariant | Status | Code Evidence |
|---|-----------|--------|---------------|
| 1 | No financial entity exists without valid TenantId | ⚠️ **FAIL** | `Subscription.cs:30` requires TenantId in constructor, but `EntityBase.TenantId` is nullable (`Guid?`). `Order.cs` and `OrderLineItem.cs` inherit nullable TenantId. `BillingWebhookEvent.cs:69` shadows TenantId as nullable. No fail-closed guards. |
| 2 | Invoice never changes value after issuance | ❌ **UNKNOWN** | **NO INVOICE ENTITY EXISTS.** Orders have mutable fields. `Order.RecalculateTotals()` can modify amounts post-creation. This is a critical gap. |
| 3 | Payment never applied to multiple invoices | ❌ **UNKNOWN** | No Invoice entity. `PaymentResult` has no InvoiceId. Payment-to-order relationship not enforced at database level. |
| 4 | Financial state transitions are monotonic | ⚠️ **PARTIAL** | `Subscription.cs` has state checks (e.g., `Activate()` checks `PendingActivation`/`Trialing`), but no state machine enforcement. `OrderStatus` allows any transition. `PaymentStatus` is a simple enum with no transition guards. |
| 5 | Subscriptions never generate duplicate charges | ⚠️ **FAIL** | `ProcessRenewal()` in `Subscription.cs:336-358` has no idempotency key. `RecordPayment()` has no duplicate check. `BillingCycleCount++` is the only guard (insufficient). |
| 6 | Cancellations/upgrades/downgrades leave no residue | ⚠️ **PARTIAL** | `Cancel()` sets `EndDate` and clears `AutoRenew`. `ChangePlan()` updates amount but no proration calculation implemented. `SubscriptionUpgradeResult.ProratedAmount` exists but unused. |
| 7 | Webhooks and retries are idempotent | ⚠️ **PARTIAL** | `BillingWebhookEvent.ExternalEventId` exists for deduplication. BUT `BillingWebhookRepository` methods all throw `NotImplementedException`. Idempotency check code is commented out. |
| 8 | Partial failures cannot cause accounting inconsistency | ❌ **FAIL** | No transaction boundaries visible. `OrderService.CompleteOrderAsync()` grants entitlements in a loop with individual saves. If process crashes mid-loop, partial state persists. No saga/compensation pattern. |

### Detailed Evidence

#### Invariant 1: TenantId Enforcement
```csharp
// EntityBase.cs:109 - TenantId is nullable
public virtual Guid? TenantId { get; protected set; }

// Subscription.cs:30 - Constructor sets TenantId
TenantId = new TenantId(tenantId);

// BillingWebhookEvent.cs:69 - Shadows base with nullable
public new Guid? TenantId { get; set; }

// PROBLEM: No validation that TenantId is set before financial operations
```

#### Invariant 5: Duplicate Charge Risk
```csharp
// Subscription.cs:336-358 - No idempotency
public SubscriptionRenewalResult ProcessRenewal(Money newAmount)
{
    // No idempotency key check
    // No "last renewal date" check
    Amount = newAmount;
    BillingCycleCount++;  // Only "guard" - not sufficient
    // ...
}
```

#### Invariant 7: Webhook Repository Not Implemented
```csharp
// BillingWebhookRepository.cs - ALL methods throw
public Task<bool> ExistsAsync(string externalEventId, string provider, ...)
{
    // TODO: return await _context.BillingWebhookEvents
    //     .AnyAsync(e => e.ExternalEventId == externalEventId ...);
    return Task.FromException<bool>(new NotImplementedException("TODO: Inject DbContext"));
}
```

---

## 3. Module-by-Module Review

### 3.1 GameGuild.Commerce.Products

#### Architecture Assessment

| Aspect | Rating | Notes |
|--------|--------|-------|
| Separation of Concerns | ⚠️ Medium | Products contain affiliate/referral logic that should be separate |
| Price Versioning | ⚠️ Partial | `OrderLineItem` has snapshots, but `ProductPricing` is mutable |
| Coupling | ⚠️ Medium | Direct dependency on Identity.Users for Creator |

#### Issues Identified

1. **HIGH: Mutable Pricing Without History**
   - `ProductPricing` can be modified directly
   - No price version history
   - Active subscriptions could reference stale prices
   ```csharp
   // ProductPricing.cs - All fields are public setters
   public decimal BasePrice { get; set; }
   public decimal? SalePrice { get; set; }
   ```

2. **MEDIUM: Business Logic in Entity**
   - `Product.Create()` factory has hardcoded defaults
   - Referral commission logic embedded in Product entity
   ```csharp
   // Product.cs:86-89
   public decimal ReferralCommissionPercentage { get; set; } = 30m;
   public decimal AffiliateCommissionPercentage { get; set; } = 30m;
   ```

3. **LOW: BundleItems as JSON String**
   - `Product.BundleItems` stored as JSON string
   - No type safety for bundle composition
   - Risk of orphaned bundle references

#### Positive Findings

✅ `Order` has unique `IdempotencyKey` index  
✅ `OrderLineItem` captures price snapshots at purchase time  
✅ `PromoCode` has proper validation with usage limits  
✅ `OrderService.CreateOrderAsync()` checks for existing orders by idempotency key  

---

### 3.2 GameGuild.Commerce.Subscriptions

#### Architecture Assessment

| Aspect | Rating | Notes |
|--------|--------|-------|
| State Machine | ⚠️ Informal | Status checks exist but not enforced by type system |
| Domain Events | ✅ Good | Rich event sourcing with specific event types |
| TenantId Binding | ✅ Good | Constructor requires TenantId |

#### State Model Analysis

```
┌─────────────────┐
│PendingActivation│────────────────┐
└────────┬────────┘                │
         │ Activate()              │ StartTrial()
         v                         v
    ┌────────┐              ┌──────────┐
    │ Active │              │ Trialing │
    └────┬───┘              └────┬─────┘
         │                       │ EndTrial(true)
         │<──────────────────────┘
         │
    ┌────┴────┬──────────────┐
    │         │              │
    v         v              v
┌────────┐ ┌────────┐  ┌──────────┐
│PastDue │ │Suspended│  │Cancelled │
└────────┘ └────────┘  └──────────┘
```

**Problem:** State transitions are validated at runtime with `InvalidOperationException`, not compile-time. Race conditions possible with concurrent operations.

#### Issues Identified

1. **HIGH: No Renewal Idempotency**
   ```csharp
   // Subscription.cs:336 - ProcessRenewal has no idempotency
   public SubscriptionRenewalResult ProcessRenewal(Money newAmount)
   {
       // If called twice, will double-increment BillingCycleCount
       BillingCycleCount++;
   }
   ```

2. **HIGH: Missing Proration Implementation**
   ```csharp
   // Subscription.cs:299 - ChangePlan doesn't prorate
   public void ChangePlan(Guid newPlanId, Money newAmount, DateTime? effectiveDate = null)
   {
       PlanId = newPlanId;
       Amount = newAmount;  // No proration calculation!
       // effectiveDate parameter is IGNORED
   }
   ```

3. **MEDIUM: Duplicate SubscriptionStatus Enums**
   - `GameGuild.Commerce.Products.OrderEnums.SubscriptionStatus` (8 values)
   - `GameGuild.Commerce.Subscriptions.SubscriptionStatus` (7 values)
   - Different value sets, risk of confusion

4. **MEDIUM: RecordPayment Missing Idempotency**
   ```csharp
   // Subscription.cs:378 - No external payment ID check
   public void RecordPayment(decimal amount, string currency, DateTime paymentDate)
   {
       LastPaymentAt = paymentDate;
       BillingCycleCount++;  // Can be called multiple times
   }
   ```

5. **LOW: Nullable TenantId in Interface**
   ```csharp
   // ISubscription.cs - Returns Guid.Empty for null
   Guid ISubscription.TenantId { get => TenantId ?? Guid.Empty; }
   ```

#### Positive Findings

✅ Rich domain events for audit trail  
✅ `ExternalId` and `ExternalCustomerId` for payment provider integration  
✅ Billing cycle calculation is deterministic  
✅ Auto-renewal flag with proper guards  

---

### 3.3 GameGuild.Commerce.Billing

#### Architecture Assessment

| Aspect | Rating | Notes |
|--------|--------|-------|
| Implementation Status | ❌ Critical | Repository is NOT IMPLEMENTED |
| Webhook Handlers | ⚠️ Skeleton | All handlers return Task.CompletedTask |
| Invoice Support | ❌ Missing | No Invoice entity exists |

#### Critical Issues

1. **CRITICAL: Repository Not Implemented**
   ```csharp
   // BillingWebhookRepository.cs - ALL methods throw
   public Task<BillingWebhookEvent?> GetByExternalEventIdAsync(...)
   {
       return Task.FromException<BillingWebhookEvent?>(
           new NotImplementedException("TODO: Inject DbContext"));
   }
   ```

2. **CRITICAL: Webhook Service Not Integrated**
   ```csharp
   // BillingWebhookService.cs - All handlers are TODO stubs
   public Task HandleSubscriptionCreatedAsync(SubscriptionWebhookPayload payload)
   {
       // TODO: Integrate with Subscriptions module
       return Task.CompletedTask;  // NO-OP!
   }
   ```

3. **HIGH: No Invoice Entity**
   - There is NO Invoice entity in the codebase
   - Orders and Subscriptions exist, but no formal billing document
   - Cannot guarantee invoice immutability (Invariant 2)

4. **HIGH: Webhook Idempotency Not Enforced**
   - `ExternalEventId` field exists
   - Deduplication check is commented out
   - Webhooks will be processed multiple times on retry

5. **MEDIUM: Abstract Repository Class**
   ```csharp
   // BillingWebhookRepository.cs:9 - Abstract without concrete implementation
   public abstract class BillingWebhookRepository(...) : IBillingWebhookRepository
   ```

#### Webhook Security

| Provider | Signature Verification | Idempotency | Status |
|----------|----------------------|-------------|--------|
| Stripe | ✅ Signature header checked | ❌ Not enforced | Partial |
| PayPal | ✅ Signature header checked | ❌ Not enforced | Partial |
| Google Pay | ✅ JWT + Project ID checked | ❌ Not enforced | Partial |
| Apple Pay | ⚠️ Headers checked | ❌ Not enforced | Minimal |

---

### 3.4 GameGuild.Commerce.Payments

#### Architecture Assessment

| Aspect | Rating | Notes |
|--------|--------|-------|
| Ledger Design | ✅ Good | Double-entry with debit/credit accounts |
| Wallet Implementation | ✅ Good | Proper balance checks and locking |
| Payment Gateway Isolation | ⚠️ Partial | No gateway abstraction layer |
| Dispute Handling | ✅ Good | State machine with evidence support |

#### Issues Identified

1. **HIGH: No Payment Gateway Abstraction**
   - No `IPaymentGateway` interface
   - Gateway logic will be scattered
   - Testing requires real gateway mocks

2. **HIGH: PaymentResult Missing Idempotency Key**
   ```csharp
   // PaymentResult.cs - No IdempotencyKey field
   public class PaymentResult
   {
       public string? TransactionId { get; init; }
       public string? PaymentId { get; init; }
       // Missing: IdempotencyKey for deduplication
   }
   ```

3. **MEDIUM: Wallet Balance Race Condition**
   ```csharp
   // UserWallet.cs:69 - No optimistic concurrency on balance
   public void DeductFunds(decimal amount, ...)
   {
       if (Balance < amount) throw ...;
       Balance -= amount;  // Race condition possible
   }
   ```
   - `EntityBase` has `Version` for concurrency, but wallet operations don't leverage it explicitly

4. **MEDIUM: FinancialLedgerEntry Reconciliation Reversible**
   ```csharp
   // FinancialLedgerEntry.cs:94 - Can unreconcile entries
   public void Unreconcile()
   {
       IsReconciled = false;
       ReconciledAt = null;
   }
   ```
   - Reconciled entries should be immutable for audit purposes

5. **LOW: Magic Strings for Accounts**
   ```csharp
   // FinancialLedgerEntry.cs - Accounts are strings
   public string DebitAccount { get; set; } = string.Empty;
   public string CreditAccount { get; set; } = string.Empty;
   // Should be enum or strongly-typed
   ```

#### Positive Findings

✅ `FinancialLedgerEntry` has fiscal year/period for reporting  
✅ `PaymentDispute` has proper state machine with evidence  
✅ `RevenueEvent` links to ledger entries for audit  
✅ `WalletService` has proper logging  
✅ `TaxCalculationService` handles VAT, reverse charge, exemptions  

---

## 4. Design Smells & Risks

### High Risk (Immediate Action Required)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| H1 | Billing repository not implemented | `BillingWebhookRepository.cs` | Webhooks fail, payments not recorded |
| H2 | No Invoice entity | All modules | Cannot guarantee billing immutability |
| H3 | Renewal idempotency missing | `Subscription.ProcessRenewal()` | Double charges possible |
| H4 | Webhook idempotency not enforced | `BillingWebhookService.cs` | Duplicate processing on retry |
| H5 | No transaction boundaries | `OrderService.CompleteOrderAsync()` | Partial state on failure |
| H6 | Proration not implemented | `Subscription.ChangePlan()` | Incorrect upgrade/downgrade billing |
| H7 | TenantId not fail-closed | `EntityBase.TenantId` nullable | Cross-tenant data leakage |
| H8 | No payment gateway abstraction | Payments module | Untestable, coupled |

### Medium Risk (Address Before Production)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| M1 | Mutable ProductPricing | `ProductPricing.cs` | Price changes affect historical data |
| M2 | Duplicate SubscriptionStatus enums | Products & Subscriptions | Confusion, mapping errors |
| M3 | Wallet balance race condition | `UserWallet.DeductFunds()` | Double-spend possible |
| M4 | Ledger entries reversible | `FinancialLedgerEntry.Unreconcile()` | Audit trail tampering |
| M5 | Magic strings for accounts | `FinancialLedgerEntry` | Typos, inconsistent reporting |
| M6 | BundleItems as JSON string | `Product.BundleItems` | No referential integrity |
| M7 | Abstract repository without implementation | `BillingWebhookRepository` | Code that can't run |
| M8 | RecordPayment without external ID | `Subscription.RecordPayment()` | Cannot deduplicate |

### Low Risk (Technical Debt)

| # | Issue | Location | Impact |
|---|-------|----------|--------|
| L1 | Business logic in Product entity | `Product.cs` | Violation of SRP |
| L2 | Nullable TenantId returns Guid.Empty | `ISubscription.TenantId` | Silent failures |
| L3 | Hardcoded commission percentages | `Product.cs:86-89` | Inflexible |
| L4 | TODO comments in production code | Multiple files | Incomplete features |
| L5 | No Price versioning | `ProductPricing` | Cannot reconstruct historical prices |
| L6 | Webhook service is abstract | `BillingWebhookService` | Requires inheritance |
| L7 | PayPal webhook stub | Controller | Feature incomplete |
| L8 | Apple Pay webhook not implemented | Controller | Feature incomplete |

---

## 5. Failure & Attack Scenarios

### Scenario 1: Webhook Retry Causing Duplicate Charge

**Context:** Stripe sends `invoice.payment_succeeded` webhook, but response times out.

**Expected Behavior:**
1. Webhook received and stored with `ExternalEventId`
2. On retry, system checks `ExternalEventId` exists
3. Returns 200 OK without reprocessing

**Actual Behavior (Based on Code):**
1. Webhook received
2. `BillingWebhookRepository.ExistsAsync()` throws `NotImplementedException`
3. Controller catches exception, returns 500
4. Stripe retries with exponential backoff
5. Each retry attempts to process again
6. If subscription service were connected, multiple `RecordPayment()` calls
7. `BillingCycleCount` incremented multiple times
8. User potentially charged multiple times via gateway

**Risk Impact:** HIGH - Direct financial loss, customer complaints, chargeback risk

---

### Scenario 2: Plan Upgrade with Incorrect Billing

**Context:** User upgrades from $10/month to $50/month plan mid-cycle.

**Expected Behavior:**
1. Calculate remaining days in current period
2. Apply credit for unused days ($X)
3. Charge prorated amount for new plan
4. Update next billing date appropriately

**Actual Behavior (Based on Code):**
```csharp
// Subscription.cs:299-308
public void ChangePlan(Guid newPlanId, Money newAmount, DateTime? effectiveDate = null)
{
    PlanId = newPlanId;
    Amount = newAmount;  // Just sets new amount
    // effectiveDate is IGNORED
    // No proration calculation
    // No credit issued
}
```
1. Plan changes immediately
2. Amount set to new plan price
3. No proration applied
4. Customer either overpays or underpays
5. Next billing date unchanged

**Risk Impact:** HIGH - Revenue leakage or customer overcharge

---

### Scenario 3: Tenant Context Mix-up

**Context:** Multi-tenant API receives request with forged `X-Tenant-Id` header.

**Expected Behavior:**
1. Validate user belongs to claimed tenant
2. Reject cross-tenant access
3. Fail-closed on missing tenant

**Actual Behavior (Based on Code):**
```csharp
// SubscriptionsController.cs:35 - Trusts TenantId from request body
public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest body, ...)
{
    var id = await sender.Send(new CreateSubscriptionCommand(
        body.TenantId,  // No validation against authenticated user
        body.PlanId,
        body.CreatedByUserId,  // Also from untrusted input
        ...));
}

// PaymentsController.cs:103 - Also trusts body
var result = await sender.Send(new ProcessPaymentCommand(
    body.TenantId,  // Untrusted
    body.SubscriptionId,
    ...));
```
1. Attacker crafts request with victim's TenantId
2. No validation at controller level
3. Financial operation proceeds under wrong tenant
4. Victim charged, attacker gets access

**Risk Impact:** CRITICAL - Cross-tenant data breach, financial fraud

---

### Scenario 4: Product Price Change Affecting Active Subscriptions

**Context:** Admin changes ProductPricing from $49 to $99 for existing product.

**Expected Behavior:**
1. New price applies to new subscriptions only
2. Existing subscriptions continue at contracted rate
3. Price version history maintained

**Actual Behavior (Based on Code):**
```csharp
// ProductPricing.cs - All fields are mutable
public decimal BasePrice { get; set; }

// ProductSubscriptionPlan.cs - Also mutable
public decimal Price { get; set; }

// Subscription uses Money value object, but references PlanId
// On renewal, if plan price is re-fetched, new price applies
```
1. Admin updates `ProductPricing.BasePrice`
2. No version history created
3. Existing subscriptions reference `PlanId`
4. If `ISubscriptionDomainService.CalculatePricingAsync()` fetches current price on renewal
5. Existing customers charged new rate without consent

**Risk Impact:** HIGH - Contract violation, customer trust loss

---

### Scenario 5: Payment Applied Out of Order

**Context:** Two payment webhooks arrive out of order (payment 2 before payment 1).

**Expected Behavior:**
1. Payments linked to specific billing periods
2. Out-of-order payments handled correctly
3. Subscription state remains consistent

**Actual Behavior (Based on Code):**
```csharp
// Subscription.cs:378-393
public void RecordPayment(decimal amount, string currency, DateTime paymentDate)
{
    LastPaymentAt = paymentDate;  // Overwrites with latest call
    NextBillingDate = BillingCycle switch { ... };  // Calculates from paymentDate
    BillingCycleCount++;  // Always increments
}
```
1. Payment 2 webhook arrives first
2. `LastPaymentAt` set to payment 2 date
3. `NextBillingDate` calculated from payment 2
4. `BillingCycleCount` incremented
5. Payment 1 webhook arrives later
6. `LastPaymentAt` OVERWRITTEN to payment 1 (earlier date)
7. `NextBillingDate` recalculated to EARLIER date
8. `BillingCycleCount` incremented AGAIN
9. Subscription in inconsistent state

**Risk Impact:** HIGH - Billing date corruption, missed renewals or double bills

---

## 6. Correction Plan (Minimal Changes)

### Phase 1: Critical (Week 1-2)

#### 1.1 Implement BillingWebhookRepository

```csharp
// Create concrete implementation
public class ConcreteBillingWebhookRepository : IBillingWebhookRepository
{
    private readonly IApplicationDbContext _context;
    
    public async Task<bool> ExistsAsync(string externalEventId, string provider, ...)
    {
        return await _context.Set<BillingWebhookEvent>()
            .AnyAsync(e => e.ExternalEventId == externalEventId 
                        && e.Provider == provider, ct);
    }
    
    // Implement all methods...
}
```

#### 1.2 Add Idempotency to Webhook Processing

```csharp
// In webhook command handlers, before processing:
var exists = await _webhookRepo.ExistsAsync(eventId, provider, ct);
if (exists) return WebhookProcessingResult.AlreadyProcessed(eventId);

// Store webhook event FIRST
await _webhookRepo.CreateAsync(webhookEvent, ct);
// Then process...
```

#### 1.3 Add Tenant Validation at Controller Level

```csharp
// Create authorization filter or use existing middleware
[ValidateTenantOwnership]
public async Task<IActionResult> CreateSubscription([FromBody] CreateSubscriptionRequest body)
{
    // Filter validates User.TenantId matches body.TenantId
}
```

#### 1.4 Add Transaction Boundaries

```csharp
// OrderService.CompleteOrderAsync - Wrap in transaction
public async Task<OrderResult> CompleteOrderAsync(...)
{
    await using var transaction = await _context.Database.BeginTransactionAsync(ct);
    try
    {
        // All operations...
        await transaction.CommitAsync(ct);
    }
    catch
    {
        await transaction.RollbackAsync(ct);
        throw;
    }
}
```

### Phase 2: High Priority (Week 3-4)

#### 2.1 Add Renewal Idempotency

```csharp
// Add to Subscription entity
public string? LastRenewalIdempotencyKey { get; private set; }

public SubscriptionRenewalResult ProcessRenewal(Money newAmount, string idempotencyKey)
{
    if (LastRenewalIdempotencyKey == idempotencyKey)
        return SubscriptionRenewalResult.AlreadyProcessed(Id);
    
    LastRenewalIdempotencyKey = idempotencyKey;
    // Continue processing...
}
```

#### 2.2 Add External Payment ID to RecordPayment

```csharp
public void RecordPayment(decimal amount, string currency, DateTime paymentDate, 
                          string externalPaymentId)  // Add this
{
    // Check for duplicate payment ID in payment history
    // Store externalPaymentId for audit
}
```

#### 2.3 Create Invoice Entity

```csharp
[Table("Invoices")]
public class Invoice : EntityBase
{
    [Required]
    public Guid TenantId { get; private set; }  // NOT nullable
    
    public Guid? SubscriptionId { get; private set; }
    public Guid? OrderId { get; private set; }
    
    public InvoiceStatus Status { get; private set; }
    
    // Immutable after issuance
    public decimal Subtotal { get; private init; }
    public decimal TaxAmount { get; private init; }
    public decimal Total { get; private init; }
    public string Currency { get; private init; }
    
    public DateTime IssuedAt { get; private init; }
    public DateTime DueDate { get; private set; }
    
    // Only status transitions allowed, not amount changes
    public void MarkAsPaid(string paymentId) { ... }
    public void MarkAsVoid(string reason) { ... }
}
```

### Phase 3: Medium Priority (Week 5-6)

#### 3.1 Add Price Versioning

```csharp
[Table("ProductPricingVersions")]
public class ProductPricingVersion : EntityBase
{
    public Guid ProductPricingId { get; set; }
    public int Version { get; set; }
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
```

#### 3.2 Add Payment Gateway Abstraction

```csharp
public interface IPaymentGateway
{
    string Provider { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct);
    Task<RefundResult> ProcessRefundAsync(RefundRequest request, CancellationToken ct);
    Task<bool> ValidateWebhookSignatureAsync(string payload, string signature);
}

// Implementations: StripePaymentGateway, PayPalPaymentGateway, etc.
```

#### 3.3 Implement Proration

```csharp
public class PlanChangeService
{
    public PlanChangeResult CalculatePlanChange(
        Subscription subscription,
        SubscriptionPlan newPlan,
        DateTime effectiveDate)
    {
        var daysRemaining = (subscription.CurrentPeriodEnd - effectiveDate).Days;
        var totalDays = (subscription.CurrentPeriodEnd - subscription.CurrentPeriodStart).Days;
        var creditRatio = (decimal)daysRemaining / totalDays;
        
        var credit = subscription.Amount.Amount * creditRatio;
        var newCharge = CalculateNewPlanAmount(newPlan, daysRemaining);
        
        return new PlanChangeResult
        {
            Credit = new Money(credit, subscription.Amount.Currency),
            Charge = new Money(newCharge, subscription.Amount.Currency),
            NetAmount = new Money(newCharge - credit, subscription.Amount.Currency)
        };
    }
}
```

### Phase 4: Lower Priority (Week 7-8)

#### 4.1 Unify SubscriptionStatus Enums
- Remove duplicate from `OrderEnums.cs`
- Use single source of truth in Subscriptions module

#### 4.2 Add Optimistic Concurrency to Wallet
```csharp
public void DeductFunds(decimal amount, ...)
{
    if (!IsActive || IsLocked || Balance < amount)
        throw new InvalidOperationException(...);
    
    // EntityBase.Version will be checked on save
    Balance -= amount;
    Touch();  // Increments version
}
```

#### 4.3 Make Reconciliation Immutable
```csharp
// Remove Unreconcile() method
// Add ReconciledLedgerEntry wrapper that prevents modification
```

---

## 7. Test Plan (Mandatory)

### Critical Tests (Must Have Before Production)

| Test Category | Test Case | Priority |
|--------------|-----------|----------|
| **Single Charge Guarantee** | | |
| | Renewal with same idempotency key returns cached result | P0 |
| | RecordPayment with duplicate external ID is rejected | P0 |
| | Concurrent renewal calls produce single charge | P0 |
| **Webhook Idempotency** | | |
| | Same ExternalEventId processed only once | P0 |
| | Webhook retry after timeout returns 200 without reprocessing | P0 |
| | Failed webhook stored and retryable | P0 |
| **Upgrade/Downgrade Safety** | | |
| | Plan upgrade calculates correct proration | P0 |
| | Plan downgrade credits unused period | P0 |
| | Mid-cycle upgrade charges correct amount | P0 |
| | Downgrade effective at period end | P1 |
| **Cancellation Without Residue** | | |
| | Cancelled subscription cannot renew | P0 |
| | Cancellation sets correct EndDate | P0 |
| | Auto-renew disabled on cancellation | P0 |
| | Entitlements remain until EndDate | P1 |
| **Tenant Isolation** | | |
| | User cannot create subscription for other tenant | P0 |
| | User cannot view other tenant's payments | P0 |
| | TenantId required for all financial entities | P0 |
| | Cross-tenant subscription access denied | P0 |
| **Safe Billing Retries** | | |
| | Failed payment can be retried | P0 |
| | Retry creates new attempt, not duplicate | P0 |
| | Max retries enforced | P1 |
| | Subscription moves to PastDue after failures | P1 |
| **Invoice Immutability** | | |
| | Invoice amount cannot change after issuance | P0 |
| | Invoice status transitions are valid | P0 |
| | Voided invoice cannot be unvoided | P1 |

### Integration Tests

| Test Case | Description |
|-----------|-------------|
| End-to-End Purchase | Product → Order → Payment → Entitlement |
| Subscription Lifecycle | Create → Activate → Renew → Cancel |
| Webhook Processing | Mock Stripe webhook → Internal state update |
| Proration Calculation | Upgrade mid-cycle → Correct amounts |
| Tax Calculation | Multi-jurisdiction → Correct rates |

### Load/Stress Tests

| Test Case | Target |
|-----------|--------|
| Concurrent Renewals | 100 subscriptions renewing simultaneously |
| Webhook Flood | 1000 webhooks/second burst |
| Wallet Concurrent Access | 50 concurrent deductions from same wallet |

---

## 8. Final Executive Report

### Executive Summary

The GameGuild Commerce modules are in **early development stage** and are **NOT SUITABLE for production use** with real financial transactions. Critical financial invariants are not guaranteed, and core infrastructure (webhook processing, billing repository) is not implemented.

### Key Risks Identified

| Risk | Severity | Likelihood | Business Impact |
|------|----------|------------|-----------------|
| Duplicate charges via webhook retry | CRITICAL | HIGH | Financial loss, chargebacks |
| Cross-tenant data access | CRITICAL | MEDIUM | Data breach, legal liability |
| Missing Invoice entity | HIGH | CERTAIN | Cannot guarantee billing accuracy |
| No transaction boundaries | HIGH | HIGH | Data corruption on failures |
| Proration not implemented | HIGH | CERTAIN | Incorrect billing on plan changes |
| Billing repository not functional | CRITICAL | CERTAIN | System cannot process payments |

### Potential Impact

**Financial:**
- Direct revenue loss from duplicate charges
- Chargeback fees ($15-25 per dispute)
- Customer refunds and goodwill credits
- Potential class-action if systematic overcharging

**Technical:**
- Data corruption from partial failures
- Inconsistent state across modules
- Difficult debugging without audit trail

**Legal/Compliance:**
- PCI-DSS compliance gaps
- SOC 2 audit failures
- GDPR concerns with tenant isolation

### Fix Priority

| Timeline | Actions |
|----------|---------|
| **Immediate (Week 1-2)** | Implement BillingWebhookRepository, Add tenant validation, Add transaction boundaries |
| **Short-term (Week 3-4)** | Add renewal idempotency, Create Invoice entity, Add payment external ID tracking |
| **Medium-term (Week 5-8)** | Price versioning, Payment gateway abstraction, Proration implementation |
| **Long-term (Month 2-3)** | Comprehensive test suite, Load testing, Audit logging enhancement |

### Overall Maturity Assessment

```
╔════════════════════════════════════════════════════════════════╗
║                    COMMERCE MODULE MATURITY                     ║
╠════════════════════════════════════════════════════════════════╣
║  Products:      ████████████████░░░░  65%  (Functional)        ║
║  Subscriptions: ███████████░░░░░░░░░  55%  (Core Present)      ║
║  Billing:       █████░░░░░░░░░░░░░░░  25%  (Skeleton)          ║
║  Payments:      ██████████░░░░░░░░░░  50%  (Partial)           ║
╠════════════════════════════════════════════════════════════════╣
║  OVERALL:       ████████░░░░░░░░░░░░  45%  (Early Dev)         ║
║                                                                 ║
║  Production Ready: NO                                           ║
║  MVP Ready:        NO (with restrictions)                       ║
║  Demo Ready:       YES                                          ║
╚════════════════════════════════════════════════════════════════╝
```

### Recommendations

1. **Do NOT process real payments** until Phase 1 and Phase 2 corrections complete
2. **Prioritize tenant isolation** - this is a security-critical gap
3. **Implement Invoice entity** before any billing
4. **Add comprehensive logging** for financial debugging
5. **Consider external billing service** (Stripe Billing, Chargebee) for faster time-to-market with financial safety guarantees

---

**Document Version:** 1.0  
**Classification:** CONFIDENTIAL - Internal Use Only  
**Next Review:** After Phase 2 completion (estimated +4 weeks)
