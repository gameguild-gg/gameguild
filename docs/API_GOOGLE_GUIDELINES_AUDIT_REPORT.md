# GameGuild API Endpoints - Google API Design Guidelines Audit Report

**Date:** January 16, 2026  
**Scope:** All REST API endpoints across Authentication, Commerce, Identity, and Resources modules  
**Reference:** [Google API Design Guide](https://cloud.google.com/apis/design)

---

## Executive Summary

This report analyzes GameGuild API endpoints against Google API Design Guidelines, identifying violations, inconsistencies, and missing endpoints. The audit covers **35+ controllers** with **200+ endpoints** across multiple modules.

### Key Findings

| Category | Issues Found | Priority |
|----------|--------------|----------|
| Custom Action Syntax Violations | 12+ endpoints | P0 - Critical |
| Missing Standard CRUD Methods | 20+ operations | P1 - High |
| Path-based Filters (should be query params) | 15+ endpoints | P1 - High |
| Pagination Pattern Inconsistencies | 4 different patterns | P1 - High |
| Response Format Inconsistencies | 4+ patterns | P2 - Medium |
| Field Naming Convention (PascalCase vs snake_case) | All endpoints | P3 - Low |

> **Note:** URL base path (`/api/v1/` vs `/v1/`) is **not a violation**. The `api/` prefix can be configured globally via reverse proxy, API gateway, or subdomain (e.g., `api.domain.com/v1/...`). This report focuses on resource naming and action patterns.

---

## Table of Contents

1. [URL Versioning Analysis](#1-url-versioning-analysis)
2. [ApiKey Endpoints](#2-apikey-endpoints)
3. [Authentication Endpoints](#3-authentication-endpoints)
4. [MFA Endpoints](#4-mfa-endpoints)
5. [Session Endpoints](#5-session-endpoints)
6. [Billing Webhooks Endpoints](#6-billing-webhooks-endpoints)
7. [Entitlements Endpoints](#7-entitlements-endpoints)
8. [Health Endpoints](#8-health-endpoints)
9. [KeyRotation Endpoints](#9-keyrotation-endpoints)
10. [Payments Endpoints](#10-payments-endpoints)
11. [Products Endpoints](#11-products-endpoints)
12. [PromoCodes Endpoints](#12-promocodes-endpoints)
13. [Resources Endpoints](#13-resources-endpoints)
14. [ServiceAccounts Endpoints](#14-serviceaccounts-endpoints)
15. [Subscriptions Endpoints](#15-subscriptions-endpoints)
16. [SubscriptionPlans Endpoints](#16-subscriptionplans-endpoints)
17. [Taxes Endpoints](#17-taxes-endpoints)
18. [Tenants Endpoints](#18-tenants-endpoints)
19. [Users Endpoints](#19-users-endpoints)
20. [Wallets Endpoints](#20-wallets-endpoints)
21. [WebAuthn Endpoints](#21-webauthn-endpoints)
22. [Pagination Standardization](#22-pagination-standardization)
23. [Error Response Standardization](#23-error-response-standardization)
24. [Implementation Roadmap](#24-implementation-roadmap)

---

## 1. URL Versioning Analysis

### Current State

The API uses different URL patterns for versioning:

| Pattern | Example | Controllers |
|---------|---------|-------------|
| `v1/...` | `v1/auth/sign-up` | AuthController, MfaController, SessionController |
| `api/v1/...` | `api/v1/payments` | PaymentsController, TaxController, WalletController |
| `api/auth/...` | `api/auth/api-keys` | ApiKeyController, WebAuthnController |
| `api/...` | `api/products` | ProductsController, PromoCodesController, EntitlementsController |
| Root level | `/health`, `/ready`, `/live` | HealthController |

### Configuration Strategy

> **Decision:** The `api/` prefix is **NOT** required in route definitions. It can be configured at the infrastructure level:
> - **Option A:** API Gateway/Reverse Proxy adds `/api` prefix
> - **Option B:** Use subdomain `api.domain.com` (recommended for production)
> - **Option C:** Global route prefix in ASP.NET Core

### Recommended Standard

All endpoints should follow: `v{version}/{resource}` (e.g., `v1/users`)

**Exception:** Health probes (`/health`, `/ready`, `/live`) remain at root level per Kubernetes conventions.

### Version Prefix Standardization

Endpoints currently missing version prefix should be updated:

```
BEFORE                              AFTER
──────                              ─────
/api/auth/api-keys              →   /v1/auth/api-keys
/api/products                   →   /v1/products
/api/promo-codes                →   /v1/promo-codes
/api/entitlements               →   /v1/entitlements
/api/auth/webauthn              →   /v1/auth/webauthn
/api/auth/keys                  →   /v1/auth/keys
```

---

## 2. ApiKey Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/auth/api-keys` | Create API key | ⚠️ Needs versioning |
| GET | `/api/auth/api-keys` | List API keys | ⚠️ Needs versioning |
| POST | `/api/auth/api-keys/{keyId}/revoke` | Revoke API key | ❌ Violation |

### Violations

1. **Missing version prefix** - Should include `v1` (e.g., `/v1/auth/api-keys`)
2. **Custom action syntax** - `POST .../revoke` should be `POST .../{keyId}:revoke`
3. **Missing standard methods** - No Get, Update, Delete operations

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/auth/api-keys/{keyId}/revoke` | `POST /v1/auth/api-keys/{keyId}:revoke` | Custom action syntax |
| P1 | `POST /api/auth/api-keys` | `POST /v1/auth/api-keys` | Version prefix |
| P1 | `GET /api/auth/api-keys` | `GET /v1/auth/api-keys` | Version prefix |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/auth/api-keys/{keyId}` | Get single API key by ID | P1 |
| PATCH | `/v1/auth/api-keys/{keyId}` | Update API key (name, scopes, expiration) | P1 |
| DELETE | `/v1/auth/api-keys/{keyId}` | Delete API key (hard delete) | P2 |
| HEAD | `/v1/auth/api-keys/{keyId}` | Check if API key exists | P3 |
| POST | `/v1/auth/api-keys/{keyId}:rotate` | Rotate API key secret | P2 |

---

## 3. Authentication Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/auth/sign-up` | Register new user | ✅ OK |
| POST | `/v1/auth/sign-in` | Sign in with email/password | ✅ OK |
| POST | `/v1/auth/google` | Sign in with Google | ⚠️ Naming |
| GET | `/v1/auth/github/sign-in` | Initiate GitHub OAuth | ⚠️ Naming |
| POST | `/v1/auth/refresh` | Refresh access token | ⚠️ Resource-oriented |
| POST | `/v1/auth/revoke` | Revoke refresh token | ⚠️ Resource-oriented |
| POST | `/v1/auth/web3/challenge` | Web3 auth challenge | ✅ OK |
| POST | `/v1/auth/send-email-verification` | Send verification email | ❌ Violation |

### Violations

1. **Verb in URL** - `/send-email-verification` should be custom action with colon syntax
2. **Inconsistent OAuth patterns** - Google uses POST, GitHub uses GET
3. **Token operations** - Should be resource-oriented (`/tokens:refresh`)

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `POST /v1/auth/google` | `POST /v1/auth:sign-in-google` | Explicit action naming |
| P1 | `GET /v1/auth/github/sign-in` | `GET /v1/auth/github:authorize` | Standard OAuth naming |
| P1 | `POST /v1/auth/refresh` | `POST /v1/auth/tokens:refresh` | Resource-oriented |
| P1 | `POST /v1/auth/revoke` | `POST /v1/auth/tokens:revoke` | Resource-oriented |
| P0 | `POST /v1/auth/send-email-verification` | `POST /v1/auth/email:send-verification` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| POST | `/v1/auth/email:verify` | Verify email with token | P0 |
| POST | `/v1/auth/password:reset-request` | Request password reset | P1 |
| POST | `/v1/auth/password:reset` | Complete password reset | P1 |
| POST | `/v1/auth/password:change` | Change password (authenticated) | P1 |
| GET | `/v1/auth/github:callback` | GitHub OAuth callback | P0 |
| POST | `/v1/auth/web3:verify` | Verify Web3 signature | P0 |

---

## 4. MFA Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/auth/mfa/configuration` | Get MFA configuration | ⚠️ Resource naming |
| POST | `/v1/auth/mfa/setup/totp` | Initiate TOTP setup | ❌ Violation |
| POST | `/v1/auth/mfa/setup/totp/complete` | Complete TOTP setup | ❌ Violation |
| POST | `/v1/auth/mfa/verify` | Verify MFA code | ✅ OK |
| POST | `/v1/auth/mfa/backup-codes/regenerate` | Regenerate backup codes | ❌ Violation |
| POST | `/v1/auth/mfa/disable` | Disable MFA | ❌ Violation |

### Violations

1. **Path-based actions** - Should use colon syntax for custom actions
2. **Nested resources** - `setup/totp/complete` is too deeply nested
3. **Configuration naming** - Should be simpler resource path

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /v1/auth/mfa/configuration` | `GET /v1/auth/mfa` | Simpler resource naming |
| P0 | `POST /v1/auth/mfa/setup/totp` | `POST /v1/auth/mfa/totp:setup` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/setup/totp/complete` | `POST /v1/auth/mfa/totp:complete` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/backup-codes/regenerate` | `POST /v1/auth/mfa/backup-codes:regenerate` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/disable` | `POST /v1/auth/mfa:disable` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/auth/mfa/backup-codes` | Get backup codes (masked) | P1 |
| POST | `/v1/auth/mfa/sms:setup` | Setup SMS-based MFA | P2 |
| POST | `/v1/auth/mfa/sms:complete` | Complete SMS MFA setup | P2 |
| GET | `/v1/auth/mfa/methods` | List available MFA methods | P2 |

---

## 5. Session Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/auth/sessions` | Get active sessions | ✅ OK |
| GET | `/v1/auth/sessions/security-analysis` | Get security analysis | ⚠️ Should be action |
| DELETE | `/v1/auth/sessions/{sessionId}` | Terminate session | ✅ OK |
| POST | `/v1/auth/sessions:terminate-others` | Terminate other sessions | ✅ Correct syntax |
| POST | `/v1/auth/sessions:terminate-all` | Terminate all sessions | ✅ Correct syntax |
| POST | `/v1/auth/sessions:refresh` | Refresh session | ✅ Correct syntax |
| GET | `/v1/auth/sessions/trusted-devices` | Get trusted devices | ⚠️ Nested resource |
| POST | `/v1/auth/sessions/trusted-devices` | Trust device | ⚠️ Nested resource |
| DELETE | `/v1/auth/sessions/trusted-devices/{deviceId}` | Revoke device trust | ⚠️ Nested resource |

### Violations

1. **Nested resource path** - `security-analysis` should be custom action
2. **Trusted devices** - Should be separate top-level resource under auth

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /v1/auth/sessions/security-analysis` | `GET /v1/auth/sessions:analyze-security` | Custom action |
| P2 | `GET /v1/auth/sessions/trusted-devices` | `GET /v1/auth/trusted-devices` | Separate resource |
| P2 | `POST /v1/auth/sessions/trusted-devices` | `POST /v1/auth/trusted-devices` | Separate resource |
| P2 | `DELETE /v1/auth/sessions/trusted-devices/{deviceId}` | `DELETE /v1/auth/trusted-devices/{deviceId}` | Separate resource |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/auth/sessions/{sessionId}` | Get single session details | P1 |
| HEAD | `/v1/auth/sessions/{sessionId}` | Check if session exists | P2 |
| GET | `/v1/auth/trusted-devices/{deviceId}` | Get single trusted device | P2 |
| PATCH | `/v1/auth/trusted-devices/{deviceId}` | Update trusted device name | P3 |

---

## 6. Billing Webhooks Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/billing/webhooks/google-pay` | Google Pay webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/apple-pay` | Apple Pay webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/stripe` | Stripe webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/paypal` | PayPal webhook | ✅ Correct |
| GET | `/v1/billing/webhooks/events/{eventId}` | Get webhook event | ⚠️ Resource naming |
| PATCH | `/v1/billing/webhooks/events/{eventId}/retry` | Retry webhook | ❌ Violation |

### Violations

1. **Nested resource path** - `webhooks/events` should be `webhook-events`
2. **Path-based action** - `/retry` should be `:retry`
3. **Wrong HTTP method** - Retry should be POST, not PATCH

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /v1/billing/webhooks/events/{eventId}` | `GET /v1/billing/webhook-events/{eventId}` | Simpler resource path |
| P0 | `PATCH /v1/billing/webhooks/events/{eventId}/retry` | `POST /v1/billing/webhook-events/{eventId}:retry` | Custom action syntax + POST |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/billing/webhook-events` | List all webhook events | P1 |
| POST | `/v1/billing/webhooks:test` | Test webhook configuration | P2 |
| GET | `/v1/billing/webhook-configurations` | List webhook configurations | P2 |

---

## 7. Entitlements Endpoints ✅ DONE

### ~~Current Endpoints~~ FIXED

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| ~~GET~~ | ~~`/api/entitlements/check/{productId}`~~ | ~~Check entitlement~~ | ✅ `GET /v1/entitlements:check?productId={productId}` |
| ~~POST~~ | ~~`/api/entitlements/check-multiple`~~ | ~~Check multiple~~ | ✅ `POST /v1/entitlements:check-batch` |
| ~~GET~~ | ~~`/api/entitlements/my-entitlements`~~ | ~~Get user's entitlements~~ | ✅ `GET /v1/users/me/entitlements` |
| ~~GET~~ | ~~`/api/entitlements/user/{userId}`~~ | ~~Get user entitlements~~ | ✅ `GET /v1/users/{userId}/entitlements` |
| ~~POST~~ | ~~`/api/entitlements/grant`~~ | ~~Grant entitlement~~ | ✅ `POST /v1/entitlements` |
| ~~POST~~ | ~~`/api/entitlements/revoke`~~ | ~~Revoke entitlement~~ | ✅ `POST /v1/entitlements/{entitlementId}:revoke` |
| ~~GET~~ | ~~`/api/entitlements/expiring`~~ | ~~Get expiring entitlements~~ | ✅ `GET /v1/entitlements?status=expiring` |

### ~~Violations~~ FIXED

1. ~~**Missing version prefix** - All endpoints need `v1`~~ ✅ FIXED
2. ~~**Verb in URL** - `check`, `grant`, `revoke` should be custom actions~~ ✅ FIXED
3. ~~**Non-resource paths** - `my-entitlements`, `check-multiple` are not resource-oriented~~ ✅ FIXED
4. **Missing standard CRUD** - No Get by ID, Update, Delete (Optional, not implemented)

### ~~Required Fixes~~ FIXED

| Priority | Current | Fixed | Reason | Status |
|----------|---------|-------|--------|--------|
| ~~P0~~ | ~~`GET /api/entitlements/check/{productId}`~~ | `GET /v1/entitlements:check?productId={productId}` | Custom action + query param | ✅ DONE |
| ~~P0~~ | ~~`POST /api/entitlements/check-multiple`~~ | `POST /v1/entitlements:check-batch` | Custom action syntax | ✅ DONE |
| ~~P0~~ | ~~`GET /api/entitlements/my-entitlements`~~ | `GET /v1/users/me/entitlements` | Resource-oriented | ✅ DONE |
| ~~P0~~ | ~~`GET /api/entitlements/user/{userId}`~~ | `GET /v1/users/{userId}/entitlements` | Resource-oriented | ✅ DONE |
| ~~P0~~ | ~~`POST /api/entitlements/grant`~~ | `POST /v1/entitlements` | Standard Create | ✅ DONE |
| ~~P0~~ | ~~`POST /api/entitlements/revoke`~~ | `POST /v1/entitlements/{entitlementId}:revoke` | Custom action syntax | ✅ DONE |
| ~~P1~~ | ~~`GET /api/entitlements/expiring`~~ | `GET /v1/entitlements?status=expiring` | Query parameter | ✅ DONE |

**Changes Applied:**
- [EntitlementsController.cs](../apps/api/Source/Modules/GameGuild.Commerce.Products/Controllers/EntitlementsController.cs): Added `ApiVersion("1.0")`, route updated to `v{version}/entitlements`, added list endpoint with status filter, colon syntax for `:check` and `:check-batch`, standard POST for create, `:revoke` action
- [UserEntitlementsController.cs](../apps/api/Source/Modules/GameGuild.Commerce.Products/Controllers/UserEntitlementsController.cs): NEW - handles `GET /v1/users/me/entitlements` and `GET /v1/users/{userId}/entitlements`

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/entitlements/{entitlementId}` | Get single entitlement | P0 |
| PATCH | `/v1/entitlements/{entitlementId}` | Update entitlement | P1 |
| DELETE | `/v1/entitlements/{entitlementId}` | Delete entitlement | P1 |
| HEAD | `/v1/entitlements/{entitlementId}` | Check entitlement exists | P2 |
| POST | `/v1/entitlements/{entitlementId}:extend` | Extend entitlement period | P2 |
| POST | `/v1/entitlements/{entitlementId}:transfer` | Transfer to another user | P3 |

---

## 8. Health Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/health` | Comprehensive health check | ✅ Acceptable |
| GET | `/ready` | Readiness probe | ✅ Acceptable |
| GET | `/live` | Liveness probe | ✅ Acceptable |

### Assessment

Health endpoints at root level are **acceptable per Kubernetes conventions**. No changes required.

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/health/dependencies` | Detailed dependency health | P3 |
| GET | `/metrics` | Prometheus metrics endpoint | P3 |
| GET | `/info` | Application info (version, build) | P3 |

---

## 9. KeyRotation Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/auth/keys/active` | Get active keys | ⚠️ Needs version |
| GET | `/api/auth/keys/valid` | Get valid keys | ⚠️ Needs version |
| POST | `/api/auth/keys/rotate` | Rotate keys | ❌ Violation |
| POST | `/api/auth/keys/cleanup` | Cleanup old keys | ❌ Violation |

### Violations

1. **Missing version prefix** - All endpoints need `v1`
2. **Path-based actions** - `rotate`, `cleanup` should use colon syntax
3. **Ambiguous resource naming** - `keys` is too generic
4. **Path-based status filter** - `/active`, `/valid` should be query params

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /api/auth/keys/active` | `GET /v1/auth/signing-keys?status=active` | Version + resource + query |
| P1 | `GET /api/auth/keys/valid` | `GET /v1/auth/signing-keys?status=valid` | Version + resource + query |
| P0 | `POST /api/auth/keys/rotate` | `POST /v1/auth/signing-keys:rotate` | Custom action syntax |
| P0 | `POST /api/auth/keys/cleanup` | `POST /v1/auth/signing-keys:cleanup` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/auth/signing-keys` | List all signing keys | P1 |
| GET | `/v1/auth/signing-keys/{keyId}` | Get single signing key | P1 |
| DELETE | `/v1/auth/signing-keys/{keyId}` | Revoke signing key | P2 |

---

## 10. Payments Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/v1/payments` | List payments with filtering | ✅ FIXED - Supports `status` query param (pending, completed, failed, cancelled, refunded, scheduled, overdue) |
| POST | `/api/v1/payments` | Create payment | ✅ Correct |
| GET | `/api/v1/payments/{paymentId}` | Get payment | ✅ Correct |
| POST | `/api/v1/payments/{paymentId}:cancel` | Cancel payment | ✅ FIXED - Custom action syntax |
| POST | `/api/v1/payments/{paymentId}:refund` | Refund payment | ✅ FIXED - Custom action syntax |
| POST | `/api/v1/payments/{paymentId}:retry` | Retry payment | ✅ FIXED - Custom action syntax |

### ~~Violations~~ FIXED (January 2026)

All violations in this section have been resolved:

1. ~~**Path-based status filters** - Should be query parameters~~ ✅ FIXED - Removed `/canceled`, `/failed`, `/overdue`, `/refunded`, `/scheduled` endpoints
2. ~~**Path-based actions** - Should use colon syntax~~ ✅ FIXED
3. ~~**Wrong HTTP method** - Actions should be POST, not PATCH~~ ✅ FIXED

### ~~Required Fixes~~ COMPLETED

| Priority | Current | Fixed | Status |
|----------|---------|-------|--------|
| P1 | ~~`GET /api/v1/payments/canceled`~~ | `GET /v1/payments?status=canceled` | ✅ DONE |
| P1 | ~~`GET /api/v1/payments/failed`~~ | `GET /v1/payments?status=failed` | ✅ DONE |
| P1 | ~~`GET /api/v1/payments/overdue`~~ | `GET /v1/payments?status=overdue` | ✅ DONE |
| P1 | ~~`GET /api/v1/payments/refunded`~~ | `GET /v1/payments?status=refunded` | ✅ DONE |
| P1 | ~~`GET /api/v1/payments/scheduled`~~ | `GET /v1/payments?status=scheduled` | ✅ DONE |
| P0 | ~~`PATCH /api/v1/payments/{paymentId}/cancel`~~ | `POST /v1/payments/{paymentId}:cancel` | ✅ DONE |
| P0 | ~~`PATCH /api/v1/payments/{paymentId}/refund`~~ | `POST /v1/payments/{paymentId}:refund` | ✅ DONE |
| P0 | ~~`PATCH /api/v1/payments/{paymentId}/retry`~~ | `POST /v1/payments/{paymentId}:retry` | ✅ DONE |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/v1/payments/{paymentId}` | Update payment metadata | P2 |
| DELETE | `/v1/payments/{paymentId}` | Void payment | P2 |
| HEAD | `/v1/payments/{paymentId}` | Check payment exists | P3 |
| POST | `/v1/payments/{paymentId}:capture` | Capture authorized payment | P1 |
| POST | `/v1/payments/{paymentId}:void` | Void authorized payment | P1 |
| GET | `/v1/payments/{paymentId}/refunds` | Get payment refunds | P2 |
| POST | `/v1/payments:batch-process` | Batch process payments | P3 |

---

## 11. Products Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/products/{productId}` | Get product | ⚠️ Needs version |
| PUT | `/api/products/{productId}` | Update product | ⚠️ Needs version |
| DELETE | `/api/products/{productId}` | Delete product | ⚠️ Needs version |
| GET | `/api/products` | List products | ⚠️ Needs version |
| POST | `/api/products` | Create product | ⚠️ Needs version |

### Violations

1. **Missing version prefix** - All endpoints need `v1`
2. **Missing PATCH** - Should support partial updates

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /api/products/{productId}` | `GET /v1/products/{productId}` | Version prefix |
| P1 | `PUT /api/products/{productId}` | `PUT /v1/products/{productId}` | Version prefix |
| P1 | `DELETE /api/products/{productId}` | `DELETE /v1/products/{productId}` | Version prefix |
| P1 | `GET /api/products` | `GET /v1/products` | Version prefix |
| P1 | `POST /api/products` | `POST /v1/products` | Version prefix |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/v1/products/{productId}` | Partial update product | P1 |
| HEAD | `/v1/products/{productId}` | Check product exists | P2 |
| POST | `/v1/products/{productId}:activate` | Activate product | P2 |
| POST | `/v1/products/{productId}:deactivate` | Deactivate product | P2 |
| POST | `/v1/products/{productId}:archive` | Archive product | P2 |
| GET | `/v1/products/{productId}/pricing` | Get product pricing | P2 |
| POST | `/v1/products:batch-create` | Batch create products | P3 |

---

## 12. PromoCodes Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/promo-codes` | List promo codes | ⚠️ Needs version |
| POST | `/api/promo-codes` | Create promo code | ⚠️ Needs version |
| GET | `/api/promo-codes/active` | List active codes | ❌ Violation |
| GET | `/api/promo-codes/{id}` | Get promo code | ⚠️ Needs version |
| PUT | `/api/promo-codes/{id}` | Update promo code | ⚠️ Needs version |
| DELETE | `/api/promo-codes/{id}` | Delete promo code | ⚠️ Needs version |
| POST | `/api/promo-codes/validate` | Validate code | ❌ Violation |
| POST | `/api/promo-codes/apply` | Apply code | ❌ Violation |

### Violations

1. **Missing version prefix** - All endpoints need `v1`
2. **Path-based status filter** - `/active` should be query parameter
3. **Path-based actions** - `validate`, `apply` should use colon syntax

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /api/promo-codes` | `GET /v1/promo-codes` | Version prefix |
| P1 | `POST /api/promo-codes` | `POST /v1/promo-codes` | Version prefix |
| P0 | `GET /api/promo-codes/active` | `GET /v1/promo-codes?status=active` | Query parameter |
| P1 | `GET /api/promo-codes/{id}` | `GET /v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P1 | `PUT /api/promo-codes/{id}` | `PUT /v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P1 | `DELETE /api/promo-codes/{id}` | `DELETE /v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P0 | `POST /api/promo-codes/validate` | `POST /v1/promo-codes:validate` | Custom action syntax |
| P0 | `POST /api/promo-codes/apply` | `POST /v1/promo-codes:apply` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/v1/promo-codes/{promoCodeId}` | Partial update promo code | P1 |
| HEAD | `/v1/promo-codes/{promoCodeId}` | Check promo code exists | P2 |
| POST | `/v1/promo-codes/{promoCodeId}:activate` | Activate promo code | P2 |
| POST | `/v1/promo-codes/{promoCodeId}:deactivate` | Deactivate promo code | P2 |
| GET | `/v1/promo-codes/{promoCodeId}/usage` | Get promo code usage stats | P2 |
| GET | `/v1/promo-codes/by-code/{code}` | Get promo code by code string | P1 |

---

## 13. Resources Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/resources/usage-by-type/{usageType}` | Get usage by type | ⚠️ Path param |
| POST | `/v1/resources:archive` | Archive old records | ✅ Correct syntax |

### Violations

1. **Path parameter for type** - Could be query parameter for flexibility

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P2 | `GET /v1/resources/usage-by-type/{usageType}` | `GET /v1/resources/usage?type={usageType}` | Query param |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/resources/usage` | Get aggregated usage summary | P1 |
| GET | `/v1/resources/usage-trends` | Get usage trends over time | P2 |
| POST | `/v1/resources:cleanup` | Cleanup orphaned resources | P2 |

---

## 14. ServiceAccounts Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/oauth/token` | Get OAuth token | ✅ FIXED - Versioned |
| POST | `/v1/auth/service-accounts` | Create service account | ✅ FIXED - Under auth |
| GET | `/v1/auth/service-accounts` | List service accounts with optional tenantId filter | ✅ FIXED - Added with query param |
| GET | `/v1/auth/service-accounts/{serviceAccountId}` | Get service account | ✅ FIXED - Consistent ID naming |
| DELETE | `/v1/auth/service-accounts/{serviceAccountId}` | Delete service account | ✅ FIXED - Consistent ID naming |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:rotate-secret` | Rotate secret | ✅ FIXED - Colon syntax |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:unlock` | Unlock account | ✅ FIXED - Colon syntax |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:deactivate` | Deactivate | ✅ FIXED - Colon syntax |
| POST | `/v1/auth/service-accounts/{serviceAccountId}:reactivate` | Reactivate | ✅ FIXED - Colon syntax |
| PATCH | `/v1/auth/service-accounts/{serviceAccountId}/scopes` | Update scopes | ✅ FIXED - PATCH + consistent naming |

### ~~Violations~~ FIXED (January 2026)

All violations in this section have been resolved:

1. ~~**Path-based actions** - Should use colon syntax~~ ✅ FIXED
2. ~~**Tenant filter in path** - Should be query parameter~~ ✅ FIXED - Consolidated to list endpoint
3. ~~**Inconsistent ID naming** - Should be `serviceAccountId`~~ ✅ FIXED
4. ~~**Missing list endpoint** - No way to list all service accounts~~ ✅ FIXED - Added GET with tenantId query param
5. ~~**Route path** - Should be under `/v1/auth/`~~ ✅ FIXED

### ~~Required Fixes~~ COMPLETED

| Priority | Current | Fixed | Status |
|----------|---------|-------|--------|
| P1 | ~~`GET /api/v1/service-accounts/tenant/{tenantId}`~~ | `GET /v1/auth/service-accounts?tenantId={tenantId}` | ✅ DONE |
| P0 | ~~`POST /api/v1/service-accounts/{id}/rotate-secret`~~ | `POST /v1/auth/service-accounts/{serviceAccountId}:rotate-secret` | ✅ DONE |
| P0 | ~~`POST /api/v1/service-accounts/{id}/unlock`~~ | `POST /v1/auth/service-accounts/{serviceAccountId}:unlock` | ✅ DONE |
| P0 | ~~`POST /api/v1/service-accounts/{id}/deactivate`~~ | `POST /v1/auth/service-accounts/{serviceAccountId}:deactivate` | ✅ DONE |
| P0 | ~~`POST /api/v1/service-accounts/{id}/reactivate`~~ | `POST /v1/auth/service-accounts/{serviceAccountId}:reactivate` | ✅ DONE |
| P1 | ~~`PUT /api/v1/service-accounts/{id}/scopes`~~ | `PATCH /v1/auth/service-accounts/{serviceAccountId}/scopes` | ✅ DONE |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/service-accounts` | List all service accounts | P0 |
| PATCH | `/v1/service-accounts/{serviceAccountId}` | Partial update service account | P1 |
| HEAD | `/v1/service-accounts/{serviceAccountId}` | Check service account exists | P2 |
| POST | `/v1/service-accounts/{serviceAccountId}:lock` | Lock service account | P2 |
| GET | `/v1/service-accounts/{serviceAccountId}/audit-log` | Get audit log | P2 |

---

## 15. Subscriptions Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/v1/subscriptions` | Create subscription | ✅ Correct |
| GET | `/api/v1/subscriptions` | List subscriptions | ✅ Correct |
| GET | `/api/v1/subscriptions/tenant/{tenantId}` | Get by tenant | ❌ Violation |
| GET | `/api/v1/subscriptions/tenant/{tenantId}/active` | Get active for tenant | ❌ Violation |
| GET | `/api/v1/subscriptions/plan/{planId}` | Get by plan | ❌ Violation |
| GET | `/api/v1/subscriptions/status/{status}` | Get by status | ❌ Violation |
| GET | `/api/v1/subscriptions/metrics` | Get metrics | ⚠️ Path-based |
| GET | `/api/v1/subscriptions/expiring` | Get expiring | ❌ Violation |
| HEAD | `/api/v1/subscriptions/{subscriptionId}` | Check exists | ✅ Correct |
| GET | `/api/v1/subscriptions/{subscriptionId}` | Get subscription | ✅ Correct |
| GET | `/api/v1/subscriptions/{subscriptionId}/usage` | Get usage | ✅ Correct |
| GET | `/api/v1/subscriptions/{subscriptionId}/billing-history` | Get billing history | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:activate` | Activate | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:start-trial` | Start trial | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:end-trial` | End trial | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:cancel` | Cancel | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:suspend` | Suspend | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:reactivate` | Reactivate | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:upgrade` | Upgrade | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:downgrade` | Downgrade | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:renew` | Renew | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:auto-renew` | Set auto-renew | ✅ Correct |
| POST | `/api/v1/subscriptions/{subscriptionId}:external-ids` | Set external IDs | ✅ Correct |

### Violations

1. **Path-based filters** - Tenant, plan, status filters should be query parameters
2. **Path-based status** - `expiring` should be query parameter

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P1 | `GET /api/v1/subscriptions/tenant/{tenantId}` | `GET /v1/subscriptions?tenantId={tenantId}` | Query parameter |
| P1 | `GET /api/v1/subscriptions/tenant/{tenantId}/active` | `GET /v1/subscriptions?tenantId={tenantId}&status=active` | Query parameter |
| P1 | `GET /api/v1/subscriptions/plan/{planId}` | `GET /v1/subscriptions?planId={planId}` | Query parameter |
| P1 | `GET /api/v1/subscriptions/status/{status}` | `GET /v1/subscriptions?status={status}` | Query parameter |
| P2 | `GET /api/v1/subscriptions/metrics` | `GET /v1/subscriptions:get-metrics` or keep | Custom action or resource |
| P1 | `GET /api/v1/subscriptions/expiring` | `GET /v1/subscriptions?status=expiring` | Query parameter |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/v1/subscriptions/{subscriptionId}` | Partial update subscription | P1 |
| PUT | `/v1/subscriptions/{subscriptionId}` | Full update subscription | P2 |
| DELETE | `/v1/subscriptions/{subscriptionId}` | Delete subscription | P2 |
| POST | `/v1/subscriptions/{subscriptionId}:pause` | Pause subscription | P2 |
| POST | `/v1/subscriptions/{subscriptionId}:resume` | Resume subscription | P2 |
| GET | `/v1/subscriptions/{subscriptionId}/invoices` | Get subscription invoices | P2 |

---

## 16. SubscriptionPlans Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/subscription-plans` | Create plan | ✅ OK |
| GET | `/v1/subscription-plans` | List plans with filtering | ✅ FIXED - Now supports `featured`, `q`, `slug`, `minPrice`, `maxPrice` query params |
| POST | `/v1/subscription-plans:compare` | Compare plans | ✅ FIXED - Custom action syntax |
| HEAD | `/v1/subscription-plans/{planId}` | Check exists | ✅ OK |
| GET | `/v1/subscription-plans/{planId}` | Get plan | ✅ OK |
| DELETE | `/v1/subscription-plans/{planId}` | Delete plan | ✅ OK |
| GET | `/v1/subscription-plans/{planId}/usage` | Get usage stats | ✅ Correct |
| GET | `/v1/subscription-plans/{planId}/suggest-upgrades` | Suggest upgrades | ✅ Correct |
| GET | `/v1/subscription-plans/{planId}/pricing` | Get pricing | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/pricing` | Update pricing | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:validate-limits` | Validate limits | ✅ FIXED - Custom action syntax |
| PATCH | `/v1/subscription-plans/{planId}/details` | Update details | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/limits` | Update limits | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/features` | Update features | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:activate` | Activate | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:deactivate` | Deactivate | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:featured` | Set featured | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:external-id` | Set external ID | ✅ Correct |

### ~~Violations~~ FIXED (January 2026)

All violations in this section have been resolved:

1. ~~**Path-based filters** - `featured`, `price-range` should be query parameters~~ ✅ FIXED
2. ~~**Search as path** - Should be query on collection~~ ✅ FIXED
3. ~~**Compare as path** - Should be custom action~~ ✅ FIXED
4. ~~**Validate as GET** - Should be POST custom action~~ ✅ FIXED

### ~~Required Fixes~~ COMPLETED

| Priority | Current | Fixed | Status |
|----------|---------|-------|--------|
| P1 | ~~`GET /v1/subscription-plans/featured`~~ | `GET /v1/subscription-plans?featured=true` | ✅ DONE |
| P1 | ~~`GET /v1/subscription-plans/search`~~ | `GET /v1/subscription-plans?q={searchTerm}` | ✅ DONE |
| P1 | ~~`GET /v1/subscription-plans/price-range`~~ | `GET /v1/subscription-plans?minPrice={min}&maxPrice={max}` | ✅ DONE |
| P0 | ~~`GET /v1/subscription-plans/compare`~~ | `POST /v1/subscription-plans:compare` | ✅ DONE |
| P1 | ~~`GET /v1/subscription-plans/slug/{slug}`~~ | `GET /v1/subscription-plans?slug={slug}` | ✅ DONE |
| P0 | ~~`GET /v1/subscription-plans/{planId}/validate-limits`~~ | `POST /v1/subscription-plans/{planId}:validate-limits` | ✅ DONE |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PUT | `/v1/subscription-plans/{planId}` | Full update plan | P1 |
| POST | `/v1/subscription-plans/{planId}:archive` | Archive plan | P2 |
| POST | `/v1/subscription-plans/{planId}:clone` | Clone plan | P2 |

---

## 17. Taxes Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/v1/tax/calculate` | Calculate tax | ❌ Violation |
| GET | `/api/v1/tax/jurisdictions` | Get jurisdictions | ⚠️ Singular resource |
| GET | `/api/v1/tax/rules` | Get rules | ⚠️ Singular resource |

### Violations

1. **Singular resource name** - Should be `taxes` (plural)
2. **Path-based action** - `calculate` should use custom action syntax
3. **Nested resources** - `jurisdictions` and `rules` should be separate resources

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/v1/tax/calculate` | `POST /v1/taxes:calculate` | Plural + custom action |
| P1 | `GET /api/v1/tax/jurisdictions` | `GET /v1/tax-jurisdictions` | Separate resource |
| P1 | `GET /api/v1/tax/rules` | `GET /v1/tax-rules` | Separate resource |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/tax-jurisdictions/{jurisdictionId}` | Get single jurisdiction | P2 |
| POST | `/v1/tax-jurisdictions` | Create jurisdiction | P2 |
| PATCH | `/v1/tax-jurisdictions/{jurisdictionId}` | Update jurisdiction | P2 |
| DELETE | `/v1/tax-jurisdictions/{jurisdictionId}` | Delete jurisdiction | P2 |
| GET | `/v1/tax-rules/{ruleId}` | Get single rule | P2 |
| POST | `/v1/tax-rules` | Create rule | P2 |
| PATCH | `/v1/tax-rules/{ruleId}` | Update rule | P2 |
| DELETE | `/v1/tax-rules/{ruleId}` | Delete rule | P2 |
| POST | `/v1/taxes:validate-exemption` | Validate tax exemption | P2 |

---

## 18. Tenants Endpoints ✅ DONE

### Current Endpoints

All tenant endpoints follow **excellent Google API patterns** with proper colon syntax for custom actions.

### ~~Minor Issues~~ FIXED

1. ~~**Inconsistent ID naming** - Some use `{id}`, others use `{tenantId}` - should standardize to `{tenantId}`~~ ✅ FIXED

### ~~Required Fixes~~ FIXED

| Priority | Current | Fixed | Reason | Status |
|----------|---------|-------|--------|--------|
| ~~P2~~ | ~~`GET /api/v1/tenants/{id}/metadata`~~ | `GET /v1/tenants/{tenantId}/metadata` | Consistent ID naming | ✅ DONE |
| ~~P2~~ | ~~`GET /api/v1/tenants/{id}/settings`~~ | `GET /v1/tenants/{tenantId}/settings` | Consistent ID naming | ✅ DONE |

**Changes Applied:**
- [TenantMetadataController.cs](../apps/api/Source/Modules/GameGuild.Identity.Tenants/Controllers/TenantMetadataController.cs): Route updated from `api/v{version}/tenants/{id}/metadata` to `v{version}/tenants/{tenantId}/metadata`, all method parameters changed from `id` to `tenantId` (8 methods)
- [TenantSettingsController.cs](../apps/api/Source/Modules/GameGuild.Identity.Tenants/Controllers/TenantSettingsController.cs): Route updated from `api/v{version}/tenants/{id}/settings` to `v{version}/tenants/{tenantId}/settings`, all method parameters changed from `id` to `tenantId` (9 methods)

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| POST | `/v1/tenants:validate` | Validate tenant data before creation | P3 |
| GET | `/v1/tenants/{tenantId}/audit-log` | Get tenant audit log | P2 |

---

## 19. Users Endpoints

### Current Endpoints

All user endpoints follow **excellent Google API patterns** with proper colon syntax for custom actions. No major violations.

### Assessment

✅ **Well-designed** - Users module is compliant with Google API guidelines.

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/users/me` | Get current user | P1 |
| PATCH | `/v1/users/me` | Update current user | P1 |
| GET | `/v1/users/me/permissions` | Get current user permissions | P2 |
| POST | `/v1/users/{userId}:impersonate` | Impersonate user (admin) | P3 |

---

## 20. Wallets Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/v1/wallet/create` | Create wallet | ❌ Violation |
| GET | `/api/v1/wallet/{userId}` | Get wallet | ❌ Violation |
| GET | `/api/v1/wallet/{userId}/balance` | Get balance | ✅ Correct |
| POST | `/api/v1/wallet/add-funds` | Add funds | ❌ Violation |
| POST | `/api/v1/wallet/deduct-funds` | Deduct funds | ❌ Violation |
| POST | `/api/v1/wallet/transfer` | Transfer funds | ❌ Violation |
| POST | `/api/v1/wallet/{userId}/lock` | Lock wallet | ❌ Violation |
| POST | `/api/v1/wallet/{userId}/unlock` | Unlock wallet | ❌ Violation |
| GET | `/api/v1/wallet/{userId}/transactions` | Get transactions | ✅ Correct |

### Violations

1. **Singular resource name** - Should be `wallets` (plural)
2. **Path-based actions** - Should use colon syntax
3. **User ID as wallet ID** - Should be wallet resource with walletId

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/v1/wallet/create` | `POST /v1/wallets` | Standard Create |
| P0 | `GET /api/v1/wallet/{userId}` | `GET /v1/users/{userId}/wallet` | Resource-oriented |
| P0 | `GET /api/v1/wallet/{userId}/balance` | `GET /v1/users/{userId}/wallet/balance` | Resource-oriented |
| P0 | `POST /api/v1/wallet/add-funds` | `POST /v1/wallets/{walletId}:add-funds` | Custom action syntax |
| P0 | `POST /api/v1/wallet/deduct-funds` | `POST /v1/wallets/{walletId}:deduct-funds` | Custom action syntax |
| P0 | `POST /api/v1/wallet/transfer` | `POST /v1/wallets/{walletId}:transfer` | Custom action syntax |
| P0 | `POST /api/v1/wallet/{userId}/lock` | `POST /v1/wallets/{walletId}:lock` | Custom action syntax |
| P0 | `POST /api/v1/wallet/{userId}/unlock` | `POST /v1/wallets/{walletId}:unlock` | Custom action syntax |
| P0 | `GET /api/v1/wallet/{userId}/transactions` | `GET /v1/wallets/{walletId}/transactions` | Resource-oriented |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/wallets` | List all wallets (admin) | P1 |
| GET | `/v1/wallets/{walletId}` | Get wallet by ID | P0 |
| PATCH | `/v1/wallets/{walletId}` | Update wallet settings | P2 |
| DELETE | `/v1/wallets/{walletId}` | Close wallet | P2 |
| HEAD | `/v1/wallets/{walletId}` | Check wallet exists | P3 |
| POST | `/v1/wallets/{walletId}:freeze` | Freeze wallet (security) | P2 |
| POST | `/v1/wallets/{walletId}:unfreeze` | Unfreeze wallet | P2 |
| GET | `/v1/wallets/{walletId}/audit-log` | Get wallet audit log | P2 |

---

## 21. WebAuthn Endpoints ✅ DONE

### ~~Current Endpoints~~ FIXED

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| ~~POST~~ | ~~`/api/auth/webauthn/register/begin`~~ | ~~Begin registration~~ | ✅ `POST /v1/auth/webauthn/registration:begin` |
| ~~POST~~ | ~~`/api/auth/webauthn/register/complete`~~ | ~~Complete registration~~ | ✅ `POST /v1/auth/webauthn/registration:complete` |
| ~~POST~~ | ~~`/api/auth/webauthn/authenticate/begin`~~ | ~~Begin auth~~ | ✅ `POST /v1/auth/webauthn/authentication:begin` |
| ~~POST~~ | ~~`/api/auth/webauthn/authenticate/complete`~~ | ~~Complete auth~~ | ✅ `POST /v1/auth/webauthn/authentication:complete` |
| ~~GET~~ | ~~`/api/auth/webauthn/credentials`~~ | ~~List credentials~~ | ✅ `GET /v1/auth/webauthn/credentials` |
| ~~DELETE~~ | ~~`/api/auth/webauthn/credentials/{credentialId}`~~ | ~~Delete credential~~ | ✅ `DELETE /v1/auth/webauthn/credentials/{credentialId}` |
| ~~PATCH~~ | ~~`/api/auth/webauthn/credentials/{credentialId}`~~ | ~~Update credential~~ | ✅ `PATCH /v1/auth/webauthn/credentials/{credentialId}` |
| ~~GET~~ | ~~`/api/auth/webauthn/status`~~ | ~~Get WebAuthn status~~ | ✅ `GET /v1/auth/webauthn` |

### ~~Violations~~ FIXED

1. ~~**Missing version prefix** - All endpoints need `v1`~~ ✅ FIXED
2. ~~**Path-based actions** - `begin`, `complete` should use colon syntax~~ ✅ FIXED
3. ~~**Deeply nested paths** - `register/begin` should be `registration:begin`~~ ✅ FIXED

### ~~Required Fixes~~ FIXED

| Priority | Current | Fixed | Reason | Status |
|----------|---------|-------|--------|--------|
| ~~P0~~ | ~~`POST /api/auth/webauthn/register/begin`~~ | `POST /v1/auth/webauthn/registration:begin` | Custom action | ✅ DONE |
| ~~P0~~ | ~~`POST /api/auth/webauthn/register/complete`~~ | `POST /v1/auth/webauthn/registration:complete` | Custom action | ✅ DONE |
| ~~P0~~ | ~~`POST /api/auth/webauthn/authenticate/begin`~~ | `POST /v1/auth/webauthn/authentication:begin` | Custom action | ✅ DONE |
| ~~P0~~ | ~~`POST /api/auth/webauthn/authenticate/complete`~~ | `POST /v1/auth/webauthn/authentication:complete` | Custom action | ✅ DONE |
| ~~P1~~ | ~~`GET /api/auth/webauthn/credentials`~~ | `GET /v1/auth/webauthn/credentials` | Version prefix | ✅ DONE |
| ~~P1~~ | ~~`DELETE /api/auth/webauthn/credentials/{credentialId}`~~ | `DELETE /v1/auth/webauthn/credentials/{credentialId}` | Version prefix | ✅ DONE |
| ~~P1~~ | ~~`PATCH /api/auth/webauthn/credentials/{credentialId}`~~ | `PATCH /v1/auth/webauthn/credentials/{credentialId}` | Version prefix | ✅ DONE |
| ~~P1~~ | ~~`GET /api/auth/webauthn/status`~~ | `GET /v1/auth/webauthn` | Version + simplify | ✅ DONE |

**Changes Applied:**
- [WebAuthnController.cs](../apps/api/Source/Modules/GameGuild.Identity.Authentication/Controllers/WebAuthnController.cs): Added `ApiVersion("1.0")`, route updated from `api/auth/webauthn` to `v{version}/auth/webauthn`, colon syntax for registration and authentication actions, simplified status endpoint to root GET

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/v1/auth/webauthn/credentials/{credentialId}` | Get single credential | P1 |
| HEAD | `/v1/auth/webauthn/credentials/{credentialId}` | Check credential exists | P2 |
| POST | `/v1/auth/webauthn/credentials/{credentialId}:verify` | Verify credential | P2 |

---

## 22. Pagination Standardization

### Current State (4 Different Patterns)

| Controller | Pattern | Base |
|------------|---------|------|
| PaymentsController | `page`, `pageSize` | 1-based |
| UsersController | `pageToken`, `pageSize` | Cursor |
| ProductsController | `skip`, `take` | Offset |
| WalletController | `offset`, `limit` | Offset |

### Google API Standard

```json
// Request
GET /api/v1/resources?pageSize=20&pageToken=abc123

// Response
{
  "items": [...],
  "nextPageToken": "xyz789",
  "totalSize": 100
}
```

### Recommended Standard for GameGuild

```json
// Request Parameters
pageSize: number (default: 20, max: 100)
pageToken: string (opaque cursor)

// Response Format
{
  "data": [...],
  "pagination": {
    "nextPageToken": "string | null",
    "totalCount": number,
    "hasMore": boolean
  }
}
```

---

## 23. Error Response Standardization

### Current State (Inconsistent)

```csharp
// Pattern 1
return BadRequest(new { error = result.Error });

// Pattern 2
return StatusCode(500, new { message = "Error" });

// Pattern 3
return Problem(detail: "Error", statusCode: 400);
```

### Google API Standard (RFC 7807)

```json
{
  "error": {
    "code": 400,
    "message": "Request validation failed",
    "status": "INVALID_ARGUMENT",
    "details": [
      {
        "@type": "type.googleapis.com/google.rpc.BadRequest",
        "fieldViolations": [
          {
            "field": "email",
            "description": "Invalid email format"
          }
        ]
      }
    ]
  }
}
```

### Recommended Implementation

Create a standard `ApiErrorResponse` class and global exception filter.

---

## 24. Implementation Roadmap

### Phase 1: Critical Fixes (P0) - Week 1-2

1. **Fix custom action syntax** - Convert `/verb` to `:verb` (30+ endpoints)
2. **Add missing Get-by-ID endpoints** - ApiKeys, Sessions, Entitlements
3. **Fix wallet resource structure** - Plural naming, proper resource orientation

### Phase 2: High Priority (P1) - Week 3-4

1. **Standardize pagination** - Implement cursor-based pagination
2. **Add version prefix** - Add `v1` to unversioned endpoints
3. **Convert path filters to query parameters** - Status filters, tenant filters
4. **Add missing CRUD operations** - PATCH, DELETE where applicable

### Phase 3: Medium Priority (P2) - Week 5-6

1. **Standardize error responses** - Implement RFC 7807
2. **Add HEAD methods for existence checks**
3. **Implement batch operations**
4. **Standardize ID naming** - Use `{resourceId}` consistently

### Phase 4: Low Priority (P3) - Week 7-8

1. **Field naming convention review** (breaking change analysis)
2. **Add optional enhancement endpoints**
3. **Documentation updates**

> **Note:** The `api/` prefix can be configured globally at the infrastructure level (API gateway, reverse proxy, or subdomain) and is not tracked as a code change.

---

## Appendix A: Complete Endpoint Migration Table

| Current Path | New Path | Priority | Notes |
|--------------|----------|----------|-------|
| `POST /api/auth/api-keys/{keyId}/revoke` | `POST /v1/auth/api-keys/{keyId}:revoke` | P0 | Custom action |
| `POST /v1/auth/mfa/setup/totp` | `POST /v1/auth/mfa/totp:setup` | P0 | Custom action |
| `GET /api/entitlements/check/{productId}` | `GET /v1/entitlements:check?productId=X` | P0 | Query param |
| `PATCH /api/v1/payments/{id}/cancel` | `POST /v1/payments/{id}:cancel` | P0 | Custom action |
| `POST /api/v1/wallet/create` | `POST /v1/wallets` | P0 | Standard create |
| `POST /api/auth/webauthn/register/begin` | `POST /v1/auth/webauthn/registration:begin` | P0 | Custom action |
| `GET /api/promo-codes/active` | `GET /v1/promo-codes?status=active` | P1 | Query param |
| `GET /api/v1/payments/canceled` | `GET /v1/payments?status=canceled` | P1 | Query param |

*See individual sections for complete migration details.*

---

## Appendix B: Summary Statistics

| Metric | Count |
|--------|-------|
| Total Endpoints Analyzed | 200+ |
| Custom Action Syntax Violations | 30+ endpoints |
| Path-based Filter Violations | 15+ endpoints |
| Missing Standard Methods | 50+ operations |
| Missing Endpoints to Add | 60+ operations |
| Version Prefix Missing | 20+ endpoints |

> **Note:** URL base path (`api/` prefix) violations are **not counted** as they can be configured at infrastructure level.

---

**Report prepared for:** GameGuild Development Team  
**Next review date:** Q2 2026 (after Phase 4 completion)
