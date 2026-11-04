# Products — STRIDE threat model

> Module `GameGuild.Commerce.Products` · Documented against `develop` commit `60100459e6`.
> Families aligned with `01-user-flows.md`: **C1** catalog reads (anonymous), **C2** catalog administration, **C3** pricing & versioning, **C4** promo engine, **C5** entitlements, **C6** cross-module seams (coordinators).

## C1 — Catalog reads (anonymous browse/detail/pricing/availability)

| # | Threat (STRIDE) | Scenario | Mitigation (file:line) or residual |
|---|---|---|---|
| 1.1 | Spoofing | Anonymous claims drafts via `includeUnpublished` | requires authenticated actor (`ProductsController.cs:34, :177`); drafts owner-or-Manage (`:387-393`) |
| 1.2 | Tampering | Query filter injection (unknown category = unfiltered listing) | deterministic empty page (`GetProductsPagedQueryHandler.cs:26-31`) |
| 1.3 | Repudiation | "Who saw this draft" | residual — no read-audit on catalog reads (consistent with other catalog surfaces) |
| 1.4 | Info disclosure | Draft existence oracle via HEAD | owner-gated branch only (`:53-57`); anonymous HEAD proves published existence only |
| 1.5 | Info disclosure | Cross-tenant catalog bleed | paged query tenant-filtered when drafts requested (`:191`); published catalog is intentionally tenant-public |
| 1.6 | DoS | Heavy search scans | `ToLower().Contains` full scan (P7) + no rate limit on anonymous reads — **residual** (global limiter only) |

## C2 — Catalog administration

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 2.1 | Spoofing/Elevation | Creator spoofing via body fields | actor-pinned creator (`ProductsController.cs:207-222, :237-241`) |
| 2.2 | Elevation | Non-creator mutation | `CanMutateAsync` owner-or-Manage (`:395-399`) + typed `RequirePermission` per action (`:204, :251, :282, :312, :327, :342, :359`) |
| 2.3 | Tampering | Lost-update overwriting | `ExpectedVersion` optimistic concurrency on update/patch (`:267, :299`) |
| 2.4 | Repudiation | "I never changed that price" | price history rows (`Entities/ProductPricing.cs:111-131`) + generic operation events per command (`04-event-inventory.md` §1) |
| 2.5 | Tampering | Hard-delete evidence destruction | delete defaults soft (`:362`); hard delete behind explicit flag + purchase-reference check |
| 2.6 | DoS | Batch-create floods | batch endpoint unthrottled, quota event declared (`+N` only on promo bulk) — **residual**: `BatchCreateProductsCommand` carries **no** QuotaImpact (`UseCaseEventContracts.g.cs:14`) |
| 2.7 | Elevation | Commission fields abuse (self-dealing referral %) | commission config entity split (`Entities/ProductCommissionConfig.cs`) + monetize command-layer gate on pricing mutations (proven `MonetizationPermissionHttpTests.cs:222-277`) |

## C3 — Pricing & versioning

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 3.1 | Tampering | Silent price edit (no audit trail) | immutable `ProductPricingVersion` rows, supersede+increment (`Entities/ProductPricing.cs:122-130`); historical lookup `GetVersionAt` (`:146-152`) |
| 3.2 | Tampering | Negative or inverted sale price | guards (`:115-120`) |
| 3.3 | Spoofing | Price set by non-monetizer | endpoint `PricingManage` + command `monetization:monetize` double gate (`02-cqrs-wiring-matrix.md` §1) |
| 3.4 | Info disclosure | Draft prices leaked | pricing include re-checks product visibility (`ProductsController.cs:75-81`) |
| 3.5 | Tampering | Scheduled price rule drift (rule active outside window) | designed: sweep keeps rules in-window + version bump changes cache stamp (`BackgroundServices/PricingSchedulingBackgroundService.cs:11-18`) — **residual P1: sweep never runs, so window enforcement is inert** |
| 3.6 | Elevation | A/B experiment price manipulation for self | sticky bucket keys `(ExperimentId, BucketKey)` unique (`ProductsModule.cs:321`); admin-gated experiments |

## C4 — Promo engine

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 4.1 | Spoofing | Anonymous code farming / brute force | designed CAPTCHA gate + velocity anomaly events (`ValidatePromoCodeCommandHandler.cs:24-36`) — **residual P4: both off in host**; attempt rows still recorded (denials included) |
| 4.2 | Tampering | Double redemption | atomic recorder + `IdempotencyKey` unique (`ProductsModule.cs:232-236`); per-user counters (`:252-256`) |
| 4.3 | Elevation | Eligibility bypass (new-customer code reused) | customer-state resolver (`Services/PromoCustomerStateResolver.cs:29-56`) — **residual P4: resolver null → restricted codes fail closed for everyone** (availability, not integrity) |
| 4.4 | Tampering | Stacking to ~100% discount | stacking rules + caps (`Services/PromoStackingResolver.cs`, `PromoStackingRule` caps `ProductsModule.cs:360-368`) |
| 4.5 | Repudiation | "That code never ran" | validation attempts (denominator), usage rows, first-purchase attribution (`ProductsModule.cs:241-256`) |
| 4.6 | Info disclosure | Campaign CAC/budgets to end users | admin-gated campaign surfaces |
| 4.7 | DoS | Validation-storm table growth | attempts persisted per call — **residual P4: anomaly alerting off**, so storms are invisible until queried |

## C5 — Entitlements

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 5.1 | Elevation | Self-grant access | mutations behind typed permissions; grants tracked with actor + acquisition type (`02-cqrs-wiring-matrix.md` §1) |
| 5.2 | Tampering | Access window manipulation | windows enforced at read (`Entities/UserProduct.cs:98-108`) — cannot be bypassed by stale rows |
| 5.3 | Repudiation | "Access was never granted" | unique `(UserId,ProductId)` row + grant actor |
| 5.4 | Info disclosure | Cross-user entitlement reads | `EntitlementsPermission.Keys.ReadAll` on admin reads; self-scope on `UserEntitlementsController` |
| 5.5 | DoS | Expiring-entitlement scans | parameterized day horizon (`EntitlementsController.cs:44-50`) |

## C6 — Cross-module seams

| # | Threat | Scenario | Mitigation or residual |
|---|---|---|---|
| 6.1 | Tampering | Promo quote manipulated at subscription creation | quote computed by module-owned coordinator against **plan** price (`Subs:/Commands/CreateSubscription/CreateSubscriptionCommandHandler.cs:89-95`) — with P2 the seam fails closed (error), so no manipulation window exists today; re-assess after fix |
| 6.2 | Repudiation | Fraud review vanished (no ticket) | **residual P3**: coordinator null means the review→ticket audit trail silently never materializes — the review exists in the fraud assessment but not in the queue staff watch |
| 6.3 | Info disclosure | Promo discount context leakage across tenants | coordinator carries tenant-scoped quote inputs (`SubscriptionPromoContracts.cs` seam) |
| 6.4 | Tampering | Display price vs billed amount divergence (social-engineering vector: advertise X, charge Y) | payments guard rejects divergent charges (`Payments:/Commands/ProcessPayment/ProcessPaymentCommandHandler.cs:50-53`) — **residual P6: the displayed price is uncontrolled**, a customer-trust threat more than a funds threat |

## Family → gap mapping

| Family | Residual risks live in |
|---|---|
| C1 | P7 (search scale), no rate limit |
| C2 | batch-create quota gap (2.6) |
| C3 | P1 (scheduler inert → window drift) |
| C4 | P4 (captcha/anomaly/eligibility off) |
| C5 | — (clean) |
| C6 | P2 (fail-closed 500), P3 (silent skip), P6 (display divergence) |
