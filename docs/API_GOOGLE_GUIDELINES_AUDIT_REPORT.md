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
| URL Versioning Inconsistencies | 6 different patterns | P0 - Critical |
| Custom Action Syntax Violations | 12+ endpoints | P0 - Critical |
| Missing Standard CRUD Methods | 20+ operations | P1 - High |
| Pagination Pattern Inconsistencies | 4 different patterns | P1 - High |
| Response Format Inconsistencies | 4+ patterns | P2 - Medium |
| Field Naming Convention (PascalCase vs snake_case) | All endpoints | P3 - Low |

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

### Current State (CRITICAL INCONSISTENCY)

The API uses **6 different URL patterns**, violating Google's recommendation for consistent resource naming:

| Pattern | Example | Controllers |
|---------|---------|-------------|
| `v1/...` | `v1/auth/sign-up` | AuthController, MfaController, SessionController |
| `api/v1/...` | `api/v1/payments` | PaymentsController, TaxController, WalletController |
| `api/auth/...` | `api/auth/api-keys` | ApiKeyController, WebAuthnController |
| `api/...` | `api/products` | ProductsController, PromoCodesController, EntitlementsController |
| Root level | `/health`, `/ready`, `/live` | HealthController |
| Mixed | Various | KeyRotationController |

### Recommended Standard

All endpoints should follow: `api/v{version}/{resource}`

**Exception:** Health probes (`/health`, `/ready`, `/live`) may remain at root level per Kubernetes conventions.

### Migration Plan

```
BEFORE                              AFTER
──────                              ─────
/api/auth/api-keys              →   /api/v1/auth/api-keys
/api/products                   →   /api/v1/products
/api/promo-codes                →   /api/v1/promo-codes
/api/entitlements               →   /api/v1/entitlements
/v1/auth/sign-up                →   /api/v1/auth/sign-up
/api/auth/webauthn              →   /api/v1/auth/webauthn
/api/auth/keys                  →   /api/v1/auth/keys
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

1. **Missing version prefix** - Should be `/api/v1/auth/api-keys`
2. **Custom action syntax** - `POST .../revoke` should be `POST .../{keyId}:revoke`
3. **Missing standard methods** - No Get, Update, Delete operations

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/auth/api-keys/{keyId}/revoke` | `POST /api/v1/auth/api-keys/{keyId}:revoke` | Custom action syntax |
| P0 | `POST /api/auth/api-keys` | `POST /api/v1/auth/api-keys` | Version prefix |
| P0 | `GET /api/auth/api-keys` | `GET /api/v1/auth/api-keys` | Version prefix |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/auth/api-keys/{keyId}` | Get single API key by ID | P1 |
| PATCH | `/api/v1/auth/api-keys/{keyId}` | Update API key (name, scopes, expiration) | P1 |
| DELETE | `/api/v1/auth/api-keys/{keyId}` | Delete API key (hard delete) | P2 |
| HEAD | `/api/v1/auth/api-keys/{keyId}` | Check if API key exists | P3 |
| POST | `/api/v1/auth/api-keys/{keyId}:rotate` | Rotate API key secret | P2 |

---

## 3. Authentication Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/auth/sign-up` | Register new user | ⚠️ Missing api prefix |
| POST | `/v1/auth/sign-in` | Sign in with email/password | ⚠️ Missing api prefix |
| POST | `/v1/auth/google` | Sign in with Google | ⚠️ Missing api prefix |
| GET | `/v1/auth/github/sign-in` | Initiate GitHub OAuth | ⚠️ Missing api prefix |
| POST | `/v1/auth/refresh` | Refresh access token | ⚠️ Missing api prefix |
| POST | `/v1/auth/revoke` | Revoke refresh token | ⚠️ Missing api prefix |
| POST | `/v1/auth/web3/challenge` | Web3 auth challenge | ⚠️ Missing api prefix |
| POST | `/v1/auth/send-email-verification` | Send verification email | ❌ Violation |

### Violations

1. **Missing `api/` prefix** - All endpoints should be `/api/v1/auth/...`
2. **Verb in URL** - `/send-email-verification` should be custom action
3. **Inconsistent OAuth patterns** - Google uses POST, GitHub uses GET

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /v1/auth/sign-up` | `POST /api/v1/auth:sign-up` | API prefix + custom action |
| P0 | `POST /v1/auth/sign-in` | `POST /api/v1/auth:sign-in` | API prefix + custom action |
| P0 | `POST /v1/auth/google` | `POST /api/v1/auth:sign-in-google` | API prefix + explicit action |
| P0 | `GET /v1/auth/github/sign-in` | `GET /api/v1/auth/github:authorize` | Standard OAuth naming |
| P0 | `POST /v1/auth/refresh` | `POST /api/v1/auth/tokens:refresh` | Resource-oriented |
| P0 | `POST /v1/auth/revoke` | `POST /api/v1/auth/tokens:revoke` | Resource-oriented |
| P0 | `POST /v1/auth/web3/challenge` | `POST /api/v1/auth/web3:challenge` | Custom action syntax |
| P1 | `POST /v1/auth/send-email-verification` | `POST /api/v1/auth/email:send-verification` | Resource + action |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| POST | `/api/v1/auth/email:verify` | Verify email with token | P0 |
| POST | `/api/v1/auth/password:reset-request` | Request password reset | P1 |
| POST | `/api/v1/auth/password:reset` | Complete password reset | P1 |
| POST | `/api/v1/auth/password:change` | Change password (authenticated) | P1 |
| GET | `/api/v1/auth/github:callback` | GitHub OAuth callback | P0 |
| POST | `/api/v1/auth/web3:verify` | Verify Web3 signature | P0 |

---

## 4. MFA Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/auth/mfa/configuration` | Get MFA configuration | ⚠️ Missing api prefix |
| POST | `/v1/auth/mfa/setup/totp` | Initiate TOTP setup | ❌ Violation |
| POST | `/v1/auth/mfa/setup/totp/complete` | Complete TOTP setup | ❌ Violation |
| POST | `/v1/auth/mfa/verify` | Verify MFA code | ⚠️ Missing api prefix |
| POST | `/v1/auth/mfa/backup-codes/regenerate` | Regenerate backup codes | ❌ Violation |
| POST | `/v1/auth/mfa/disable` | Disable MFA | ❌ Violation |

### Violations

1. **Missing `api/` prefix** - All endpoints need `/api/v1/`
2. **Path-based actions** - Should use colon syntax for custom actions
3. **Nested resources** - `setup/totp/complete` is too deeply nested

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /v1/auth/mfa/configuration` | `GET /api/v1/auth/mfa` | API prefix + resource naming |
| P0 | `POST /v1/auth/mfa/setup/totp` | `POST /api/v1/auth/mfa/totp:setup` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/setup/totp/complete` | `POST /api/v1/auth/mfa/totp:complete` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/verify` | `POST /api/v1/auth/mfa:verify` | API prefix |
| P0 | `POST /v1/auth/mfa/backup-codes/regenerate` | `POST /api/v1/auth/mfa/backup-codes:regenerate` | Custom action syntax |
| P0 | `POST /v1/auth/mfa/disable` | `POST /api/v1/auth/mfa:disable` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/auth/mfa/backup-codes` | Get backup codes (masked) | P1 |
| POST | `/api/v1/auth/mfa/sms:setup` | Setup SMS-based MFA | P2 |
| POST | `/api/v1/auth/mfa/sms:complete` | Complete SMS MFA setup | P2 |
| GET | `/api/v1/auth/mfa/methods` | List available MFA methods | P2 |

---

## 5. Session Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/auth/sessions` | Get active sessions | ⚠️ Missing api prefix |
| GET | `/v1/auth/sessions/security-analysis` | Get security analysis | ⚠️ Missing api prefix |
| DELETE | `/v1/auth/sessions/{sessionId}` | Terminate session | ⚠️ Missing api prefix |
| POST | `/v1/auth/sessions:terminate-others` | Terminate other sessions | ✅ Correct syntax |
| POST | `/v1/auth/sessions:terminate-all` | Terminate all sessions | ✅ Correct syntax |
| POST | `/v1/auth/sessions:refresh` | Refresh session | ✅ Correct syntax |
| GET | `/v1/auth/sessions/trusted-devices` | Get trusted devices | ⚠️ Missing api prefix |
| POST | `/v1/auth/sessions/trusted-devices` | Trust device | ⚠️ Missing api prefix |
| DELETE | `/v1/auth/sessions/trusted-devices/{deviceId}` | Revoke device trust | ⚠️ Missing api prefix |

### Violations

1. **Missing `api/` prefix** - All endpoints need `/api/v1/`
2. **Nested resource path** - `security-analysis` should be custom action

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /v1/auth/sessions` | `GET /api/v1/auth/sessions` | API prefix |
| P0 | `GET /v1/auth/sessions/security-analysis` | `GET /api/v1/auth/sessions:analyze-security` | Custom action |
| P0 | `DELETE /v1/auth/sessions/{sessionId}` | `DELETE /api/v1/auth/sessions/{sessionId}` | API prefix |
| P0 | `POST /v1/auth/sessions:terminate-others` | `POST /api/v1/auth/sessions:terminate-others` | API prefix |
| P0 | `POST /v1/auth/sessions:terminate-all` | `POST /api/v1/auth/sessions:terminate-all` | API prefix |
| P0 | `POST /v1/auth/sessions:refresh` | `POST /api/v1/auth/sessions:refresh` | API prefix |
| P0 | `GET /v1/auth/sessions/trusted-devices` | `GET /api/v1/auth/trusted-devices` | Separate resource |
| P0 | `POST /v1/auth/sessions/trusted-devices` | `POST /api/v1/auth/trusted-devices` | Separate resource |
| P0 | `DELETE /v1/auth/sessions/trusted-devices/{deviceId}` | `DELETE /api/v1/auth/trusted-devices/{deviceId}` | Separate resource |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/auth/sessions/{sessionId}` | Get single session details | P1 |
| HEAD | `/api/v1/auth/sessions/{sessionId}` | Check if session exists | P2 |
| GET | `/api/v1/auth/trusted-devices/{deviceId}` | Get single trusted device | P2 |
| PATCH | `/api/v1/auth/trusted-devices/{deviceId}` | Update trusted device name | P3 |

---

## 6. Billing Webhooks Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/billing/webhooks/google-pay` | Google Pay webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/apple-pay` | Apple Pay webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/stripe` | Stripe webhook | ✅ Correct |
| POST | `/v1/billing/webhooks/paypal` | PayPal webhook | ✅ Correct |
| GET | `/v1/billing/webhooks/events/{eventId}` | Get webhook event | ⚠️ Missing api prefix |
| PATCH | `/v1/billing/webhooks/events/{eventId}/retry` | Retry webhook | ❌ Violation |

### Violations

1. **Missing `api/` prefix** - All endpoints need `/api/v1/`
2. **Path-based action** - `/retry` should be `:retry`

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /v1/billing/webhooks/google-pay` | `POST /api/v1/billing/webhooks/google-pay` | API prefix |
| P0 | `POST /v1/billing/webhooks/apple-pay` | `POST /api/v1/billing/webhooks/apple-pay` | API prefix |
| P0 | `POST /v1/billing/webhooks/stripe` | `POST /api/v1/billing/webhooks/stripe` | API prefix |
| P0 | `POST /v1/billing/webhooks/paypal` | `POST /api/v1/billing/webhooks/paypal` | API prefix |
| P0 | `GET /v1/billing/webhooks/events/{eventId}` | `GET /api/v1/billing/webhook-events/{eventId}` | API prefix + resource |
| P0 | `PATCH /v1/billing/webhooks/events/{eventId}/retry` | `POST /api/v1/billing/webhook-events/{eventId}:retry` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/billing/webhook-events` | List all webhook events | P1 |
| POST | `/api/v1/billing/webhooks:test` | Test webhook configuration | P2 |
| GET | `/api/v1/billing/webhooks/configurations` | List webhook configurations | P2 |

---

## 7. Entitlements Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/entitlements/check/{productId}` | Check entitlement | ❌ Violation |
| POST | `/api/entitlements/check-multiple` | Check multiple | ❌ Violation |
| GET | `/api/entitlements/my-entitlements` | Get user's entitlements | ❌ Violation |
| GET | `/api/entitlements/user/{userId}` | Get user entitlements | ⚠️ Missing version |
| POST | `/api/entitlements/grant` | Grant entitlement | ❌ Violation |
| POST | `/api/entitlements/revoke` | Revoke entitlement | ❌ Violation |
| GET | `/api/entitlements/expiring` | Get expiring entitlements | ⚠️ Missing version |

### Violations

1. **Missing version prefix** - All endpoints need `/api/v1/`
2. **Verb in URL** - `check`, `grant`, `revoke` should be custom actions
3. **Non-resource paths** - `my-entitlements`, `check-multiple` are not resource-oriented
4. **Missing standard CRUD** - No Get by ID, Update, Delete

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/entitlements/check/{productId}` | `GET /api/v1/entitlements:check?productId={productId}` | Custom action + query param |
| P0 | `POST /api/entitlements/check-multiple` | `POST /api/v1/entitlements:check-batch` | Custom action syntax |
| P0 | `GET /api/entitlements/my-entitlements` | `GET /api/v1/users/me/entitlements` | Resource-oriented |
| P0 | `GET /api/entitlements/user/{userId}` | `GET /api/v1/users/{userId}/entitlements` | Resource-oriented |
| P0 | `POST /api/entitlements/grant` | `POST /api/v1/entitlements` | Standard Create |
| P0 | `POST /api/entitlements/revoke` | `POST /api/v1/entitlements/{entitlementId}:revoke` | Custom action syntax |
| P1 | `GET /api/entitlements/expiring` | `GET /api/v1/entitlements?status=expiring` | Query parameter |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/entitlements` | List all entitlements | P0 |
| GET | `/api/v1/entitlements/{entitlementId}` | Get single entitlement | P0 |
| PATCH | `/api/v1/entitlements/{entitlementId}` | Update entitlement | P1 |
| DELETE | `/api/v1/entitlements/{entitlementId}` | Delete entitlement | P1 |
| HEAD | `/api/v1/entitlements/{entitlementId}` | Check entitlement exists | P2 |
| POST | `/api/v1/entitlements/{entitlementId}:extend` | Extend entitlement period | P2 |
| POST | `/api/v1/entitlements/{entitlementId}:transfer` | Transfer to another user | P3 |

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
| GET | `/api/auth/keys/active` | Get active keys | ⚠️ Missing version |
| GET | `/api/auth/keys/valid` | Get valid keys | ⚠️ Missing version |
| POST | `/api/auth/keys/rotate` | Rotate keys | ❌ Violation |
| POST | `/api/auth/keys/cleanup` | Cleanup old keys | ❌ Violation |

### Violations

1. **Missing version prefix** - All endpoints need `/api/v1/`
2. **Path-based actions** - `rotate`, `cleanup` should use colon syntax
3. **Ambiguous resource naming** - `keys` is too generic

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/auth/keys/active` | `GET /api/v1/auth/signing-keys?status=active` | Version + resource + query |
| P0 | `GET /api/auth/keys/valid` | `GET /api/v1/auth/signing-keys?status=valid` | Version + resource + query |
| P0 | `POST /api/auth/keys/rotate` | `POST /api/v1/auth/signing-keys:rotate` | Custom action syntax |
| P0 | `POST /api/auth/keys/cleanup` | `POST /api/v1/auth/signing-keys:cleanup` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/auth/signing-keys` | List all signing keys | P1 |
| GET | `/api/v1/auth/signing-keys/{keyId}` | Get single signing key | P1 |
| DELETE | `/api/v1/auth/signing-keys/{keyId}` | Revoke signing key | P2 |

---

## 10. Payments Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/v1/payments` | List payments | ✅ Correct |
| POST | `/api/v1/payments` | Create payment | ✅ Correct |
| GET | `/api/v1/payments/canceled` | List canceled payments | ❌ Violation |
| GET | `/api/v1/payments/failed` | List failed payments | ❌ Violation |
| GET | `/api/v1/payments/overdue` | List overdue payments | ❌ Violation |
| GET | `/api/v1/payments/refunded` | List refunded payments | ❌ Violation |
| GET | `/api/v1/payments/scheduled` | List scheduled payments | ❌ Violation |
| GET | `/api/v1/payments/{paymentId}` | Get payment | ✅ Correct |
| PATCH | `/api/v1/payments/{paymentId}/cancel` | Cancel payment | ❌ Violation |
| PATCH | `/api/v1/payments/{paymentId}/refund` | Refund payment | ❌ Violation |
| PATCH | `/api/v1/payments/{paymentId}/retry` | Retry payment | ❌ Violation |

### Violations

1. **Path-based status filters** - Should be query parameters
2. **Path-based actions** - Should use colon syntax
3. **Wrong HTTP method** - Actions should be POST, not PATCH

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/v1/payments/canceled` | `GET /api/v1/payments?status=canceled` | Query parameter |
| P0 | `GET /api/v1/payments/failed` | `GET /api/v1/payments?status=failed` | Query parameter |
| P0 | `GET /api/v1/payments/overdue` | `GET /api/v1/payments?status=overdue` | Query parameter |
| P0 | `GET /api/v1/payments/refunded` | `GET /api/v1/payments?status=refunded` | Query parameter |
| P0 | `GET /api/v1/payments/scheduled` | `GET /api/v1/payments?status=scheduled` | Query parameter |
| P0 | `PATCH /api/v1/payments/{paymentId}/cancel` | `POST /api/v1/payments/{paymentId}:cancel` | Custom action syntax |
| P0 | `PATCH /api/v1/payments/{paymentId}/refund` | `POST /api/v1/payments/{paymentId}:refund` | Custom action syntax |
| P0 | `PATCH /api/v1/payments/{paymentId}/retry` | `POST /api/v1/payments/{paymentId}:retry` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/api/v1/payments/{paymentId}` | Update payment metadata | P2 |
| DELETE | `/api/v1/payments/{paymentId}` | Void payment | P2 |
| HEAD | `/api/v1/payments/{paymentId}` | Check payment exists | P3 |
| POST | `/api/v1/payments/{paymentId}:capture` | Capture authorized payment | P1 |
| POST | `/api/v1/payments/{paymentId}:void` | Void authorized payment | P1 |
| GET | `/api/v1/payments/{paymentId}/refunds` | Get payment refunds | P2 |
| POST | `/api/v1/payments:batch-process` | Batch process payments | P3 |

---

## 11. Products Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/products/{productId}` | Get product | ⚠️ Missing version |
| PUT | `/api/products/{productId}` | Update product | ⚠️ Missing version |
| DELETE | `/api/products/{productId}` | Delete product | ⚠️ Missing version |
| GET | `/api/products` | List products | ⚠️ Missing version |
| POST | `/api/products` | Create product | ⚠️ Missing version |

### Violations

1. **Missing version prefix** - All endpoints need `/api/v1/`
2. **Missing PATCH** - Should support partial updates

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/products/{productId}` | `GET /api/v1/products/{productId}` | Version prefix |
| P0 | `PUT /api/products/{productId}` | `PUT /api/v1/products/{productId}` | Version prefix |
| P0 | `DELETE /api/products/{productId}` | `DELETE /api/v1/products/{productId}` | Version prefix |
| P0 | `GET /api/products` | `GET /api/v1/products` | Version prefix |
| P0 | `POST /api/products` | `POST /api/v1/products` | Version prefix |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/api/v1/products/{productId}` | Partial update product | P1 |
| HEAD | `/api/v1/products/{productId}` | Check product exists | P2 |
| POST | `/api/v1/products/{productId}:activate` | Activate product | P2 |
| POST | `/api/v1/products/{productId}:deactivate` | Deactivate product | P2 |
| POST | `/api/v1/products/{productId}:archive` | Archive product | P2 |
| GET | `/api/v1/products/{productId}/pricing` | Get product pricing | P2 |
| POST | `/api/v1/products:batch-create` | Batch create products | P3 |

---

## 12. PromoCodes Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/api/promo-codes` | List promo codes | ⚠️ Missing version |
| POST | `/api/promo-codes` | Create promo code | ⚠️ Missing version |
| GET | `/api/promo-codes/active` | List active codes | ❌ Violation |
| GET | `/api/promo-codes/{id}` | Get promo code | ⚠️ Missing version |
| PUT | `/api/promo-codes/{id}` | Update promo code | ⚠️ Missing version |
| DELETE | `/api/promo-codes/{id}` | Delete promo code | ⚠️ Missing version |
| POST | `/api/promo-codes/validate` | Validate code | ❌ Violation |
| POST | `/api/promo-codes/apply` | Apply code | ❌ Violation |

### Violations

1. **Missing version prefix** - All endpoints need `/api/v1/`
2. **Path-based status filter** - `/active` should be query parameter
3. **Path-based actions** - `validate`, `apply` should use colon syntax

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/promo-codes` | `GET /api/v1/promo-codes` | Version prefix |
| P0 | `POST /api/promo-codes` | `POST /api/v1/promo-codes` | Version prefix |
| P0 | `GET /api/promo-codes/active` | `GET /api/v1/promo-codes?status=active` | Query parameter |
| P0 | `GET /api/promo-codes/{id}` | `GET /api/v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P0 | `PUT /api/promo-codes/{id}` | `PUT /api/v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P0 | `DELETE /api/promo-codes/{id}` | `DELETE /api/v1/promo-codes/{promoCodeId}` | Version + consistent naming |
| P0 | `POST /api/promo-codes/validate` | `POST /api/v1/promo-codes:validate` | Custom action syntax |
| P0 | `POST /api/promo-codes/apply` | `POST /api/v1/promo-codes:apply` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/api/v1/promo-codes/{promoCodeId}` | Partial update promo code | P1 |
| HEAD | `/api/v1/promo-codes/{promoCodeId}` | Check promo code exists | P2 |
| POST | `/api/v1/promo-codes/{promoCodeId}:activate` | Activate promo code | P2 |
| POST | `/api/v1/promo-codes/{promoCodeId}:deactivate` | Deactivate promo code | P2 |
| GET | `/api/v1/promo-codes/{promoCodeId}/usage` | Get promo code usage stats | P2 |
| GET | `/api/v1/promo-codes/by-code/{code}` | Get promo code by code string | P1 |

---

## 13. Resources Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| GET | `/v1/resources/usage-by-type/{usageType}` | Get usage by type | ⚠️ Missing api prefix |
| POST | `/v1/resources:archive` | Archive old records | ✅ Correct syntax |

### Violations

1. **Missing `api/` prefix** - Needs `/api/v1/`
2. **Path parameter for type** - Could be query parameter

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /v1/resources/usage-by-type/{usageType}` | `GET /api/v1/resources/usage?type={usageType}` | API prefix + query param |
| P0 | `POST /v1/resources:archive` | `POST /api/v1/resources:archive` | API prefix |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/resources/usage` | Get aggregated usage summary | P1 |
| GET | `/api/v1/resources/usage-trends` | Get usage trends over time | P2 |
| POST | `/api/v1/resources:cleanup` | Cleanup orphaned resources | P2 |

---

## 14. ServiceAccounts Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/v1/oauth/token` | Get OAuth token | ✅ Correct |
| POST | `/api/v1/service-accounts` | Create service account | ✅ Correct |
| GET | `/api/v1/service-accounts/{id}` | Get service account | ✅ Correct |
| DELETE | `/api/v1/service-accounts/{id}` | Delete service account | ✅ Correct |
| GET | `/api/v1/service-accounts/tenant/{tenantId}` | Get by tenant | ❌ Violation |
| POST | `/api/v1/service-accounts/{id}/rotate-secret` | Rotate secret | ❌ Violation |
| POST | `/api/v1/service-accounts/{id}/unlock` | Unlock account | ❌ Violation |
| POST | `/api/v1/service-accounts/{id}/deactivate` | Deactivate | ❌ Violation |
| POST | `/api/v1/service-accounts/{id}/reactivate` | Reactivate | ❌ Violation |
| PUT | `/api/v1/service-accounts/{id}/scopes` | Update scopes | ⚠️ Inconsistent |

### Violations

1. **Path-based actions** - Should use colon syntax
2. **Tenant filter in path** - Should be query parameter
3. **Inconsistent ID naming** - Should be `serviceAccountId`

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `GET /api/v1/service-accounts/tenant/{tenantId}` | `GET /api/v1/service-accounts?tenantId={tenantId}` | Query parameter |
| P0 | `POST /api/v1/service-accounts/{id}/rotate-secret` | `POST /api/v1/service-accounts/{serviceAccountId}:rotate-secret` | Custom action syntax |
| P0 | `POST /api/v1/service-accounts/{id}/unlock` | `POST /api/v1/service-accounts/{serviceAccountId}:unlock` | Custom action syntax |
| P0 | `POST /api/v1/service-accounts/{id}/deactivate` | `POST /api/v1/service-accounts/{serviceAccountId}:deactivate` | Custom action syntax |
| P0 | `POST /api/v1/service-accounts/{id}/reactivate` | `POST /api/v1/service-accounts/{serviceAccountId}:reactivate` | Custom action syntax |
| P1 | `PUT /api/v1/service-accounts/{id}/scopes` | `PATCH /api/v1/service-accounts/{serviceAccountId}/scopes` | Consistent naming + PATCH |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/service-accounts` | List all service accounts | P0 |
| PATCH | `/api/v1/service-accounts/{serviceAccountId}` | Partial update service account | P1 |
| HEAD | `/api/v1/service-accounts/{serviceAccountId}` | Check service account exists | P2 |
| POST | `/api/v1/service-accounts/{serviceAccountId}:lock` | Lock service account | P2 |
| GET | `/api/v1/service-accounts/{serviceAccountId}/audit-log` | Get audit log | P2 |

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
| P0 | `GET /api/v1/subscriptions/tenant/{tenantId}` | `GET /api/v1/subscriptions?tenantId={tenantId}` | Query parameter |
| P0 | `GET /api/v1/subscriptions/tenant/{tenantId}/active` | `GET /api/v1/subscriptions?tenantId={tenantId}&status=active` | Query parameter |
| P0 | `GET /api/v1/subscriptions/plan/{planId}` | `GET /api/v1/subscriptions?planId={planId}` | Query parameter |
| P0 | `GET /api/v1/subscriptions/status/{status}` | `GET /api/v1/subscriptions?status={status}` | Query parameter |
| P1 | `GET /api/v1/subscriptions/metrics` | `GET /api/v1/subscriptions:get-metrics` or keep | Custom action or resource |
| P0 | `GET /api/v1/subscriptions/expiring` | `GET /api/v1/subscriptions?status=expiring` | Query parameter |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PATCH | `/api/v1/subscriptions/{subscriptionId}` | Partial update subscription | P1 |
| PUT | `/api/v1/subscriptions/{subscriptionId}` | Full update subscription | P2 |
| DELETE | `/api/v1/subscriptions/{subscriptionId}` | Delete subscription | P2 |
| POST | `/api/v1/subscriptions/{subscriptionId}:pause` | Pause subscription | P2 |
| POST | `/api/v1/subscriptions/{subscriptionId}:resume` | Resume subscription | P2 |
| GET | `/api/v1/subscriptions/{subscriptionId}/invoices` | Get subscription invoices | P2 |

---

## 16. SubscriptionPlans Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/v1/subscription-plans` | Create plan | ⚠️ Missing api prefix |
| GET | `/v1/subscription-plans` | List plans | ⚠️ Missing api prefix |
| GET | `/v1/subscription-plans/featured` | Get featured | ❌ Violation |
| GET | `/v1/subscription-plans/search` | Search plans | ❌ Violation |
| GET | `/v1/subscription-plans/price-range` | Get by price range | ❌ Violation |
| GET | `/v1/subscription-plans/compare` | Compare plans | ❌ Violation |
| HEAD | `/v1/subscription-plans/{planId}` | Check exists | ⚠️ Missing api prefix |
| GET | `/v1/subscription-plans/{planId}` | Get plan | ⚠️ Missing api prefix |
| DELETE | `/v1/subscription-plans/{planId}` | Delete plan | ⚠️ Missing api prefix |
| GET | `/v1/subscription-plans/slug/{slug}` | Get by slug | ❌ Violation |
| GET | `/v1/subscription-plans/{planId}/usage` | Get usage stats | ✅ Correct |
| GET | `/v1/subscription-plans/{planId}/suggest-upgrades` | Suggest upgrades | ✅ Correct |
| GET | `/v1/subscription-plans/{planId}/pricing` | Get pricing | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/pricing` | Update pricing | ✅ Correct |
| GET | `/v1/subscription-plans/{planId}/validate-limits` | Validate limits | ❌ Violation |
| PATCH | `/v1/subscription-plans/{planId}/details` | Update details | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/limits` | Update limits | ✅ Correct |
| PATCH | `/v1/subscription-plans/{planId}/features` | Update features | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:activate` | Activate | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:deactivate` | Deactivate | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:featured` | Set featured | ✅ Correct |
| POST | `/v1/subscription-plans/{planId}:external-id` | Set external ID | ✅ Correct |

### Violations

1. **Missing `api/` prefix** - All endpoints need `/api/v1/`
2. **Path-based filters** - `featured`, `price-range` should be query parameters
3. **Search as path** - Should be query on collection
4. **Compare as path** - Should be custom action

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /v1/subscription-plans` | `POST /api/v1/subscription-plans` | API prefix |
| P0 | `GET /v1/subscription-plans` | `GET /api/v1/subscription-plans` | API prefix |
| P0 | `GET /v1/subscription-plans/featured` | `GET /api/v1/subscription-plans?featured=true` | Query parameter |
| P0 | `GET /v1/subscription-plans/search` | `GET /api/v1/subscription-plans?q={searchTerm}` | Query parameter |
| P0 | `GET /v1/subscription-plans/price-range` | `GET /api/v1/subscription-plans?minPrice={min}&maxPrice={max}` | Query parameters |
| P0 | `GET /v1/subscription-plans/compare` | `POST /api/v1/subscription-plans:compare` | Custom action |
| P0 | `GET /v1/subscription-plans/slug/{slug}` | `GET /api/v1/subscription-plans?slug={slug}` | Query parameter |
| P1 | `GET /v1/subscription-plans/{planId}/validate-limits` | `POST /api/v1/subscription-plans/{planId}:validate-limits` | Custom action syntax |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| PUT | `/api/v1/subscription-plans/{planId}` | Full update plan | P1 |
| POST | `/api/v1/subscription-plans/{planId}:archive` | Archive plan | P2 |
| POST | `/api/v1/subscription-plans/{planId}:clone` | Clone plan | P2 |

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

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/v1/tax/calculate` | `POST /api/v1/taxes:calculate` | Plural + custom action |
| P1 | `GET /api/v1/tax/jurisdictions` | `GET /api/v1/tax-jurisdictions` | Separate resource |
| P1 | `GET /api/v1/tax/rules` | `GET /api/v1/tax-rules` | Separate resource |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/tax-jurisdictions/{jurisdictionId}` | Get single jurisdiction | P2 |
| POST | `/api/v1/tax-jurisdictions` | Create jurisdiction | P2 |
| PATCH | `/api/v1/tax-jurisdictions/{jurisdictionId}` | Update jurisdiction | P2 |
| DELETE | `/api/v1/tax-jurisdictions/{jurisdictionId}` | Delete jurisdiction | P2 |
| GET | `/api/v1/tax-rules/{ruleId}` | Get single rule | P2 |
| POST | `/api/v1/tax-rules` | Create rule | P2 |
| PATCH | `/api/v1/tax-rules/{ruleId}` | Update rule | P2 |
| DELETE | `/api/v1/tax-rules/{ruleId}` | Delete rule | P2 |
| POST | `/api/v1/taxes:validate-exemption` | Validate tax exemption | P2 |

---

## 18. Tenants Endpoints

### Current Endpoints

All tenant endpoints follow **excellent Google API patterns** with proper colon syntax for custom actions. Main issues are:

1. **Missing `api/` prefix** - All `/v1/tenants/...` should be `/api/v1/tenants/...`
2. **Metadata/Settings/Quotas endpoints** have mixed versioning

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /v1/tenants` | `POST /api/v1/tenants` | API prefix |
| P0 | `GET /v1/tenants` | `GET /api/v1/tenants` | API prefix |
| P0 | All `/v1/tenants/*` | `All /api/v1/tenants/*` | API prefix |
| P0 | `GET /api/v1/tenants/{id}/metadata` | `GET /api/v1/tenants/{tenantId}/metadata` | Consistent ID naming |
| P0 | `GET /api/v1/tenants/{id}/settings` | `GET /api/v1/tenants/{tenantId}/settings` | Consistent ID naming |

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| POST | `/api/v1/tenants:validate` | Validate tenant data before creation | P3 |
| GET | `/api/v1/tenants/{tenantId}/audit-log` | Get tenant audit log | P2 |

---

## 19. Users Endpoints

### Current Endpoints

All user endpoints follow **excellent Google API patterns** with proper colon syntax for custom actions. Main issues are:

1. **Missing `api/` prefix** - All `/v1/users/...` should be `/api/v1/users/...`

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /v1/users` | `POST /api/v1/users` | API prefix |
| P0 | `GET /v1/users` | `GET /api/v1/users` | API prefix |
| P0 | All `/v1/users/*` | All `/api/v1/users/*` | API prefix |

### Missing Endpoints (Optional)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/users/me` | Get current user | P1 |
| PATCH | `/api/v1/users/me` | Update current user | P1 |
| GET | `/api/v1/users/me/permissions` | Get current user permissions | P2 |
| POST | `/api/v1/users/{userId}:impersonate` | Impersonate user (admin) | P3 |

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
| P0 | `POST /api/v1/wallet/create` | `POST /api/v1/wallets` | Standard Create |
| P0 | `GET /api/v1/wallet/{userId}` | `GET /api/v1/users/{userId}/wallet` | Resource-oriented |
| P0 | `GET /api/v1/wallet/{userId}/balance` | `GET /api/v1/users/{userId}/wallet/balance` | Resource-oriented |
| P0 | `POST /api/v1/wallet/add-funds` | `POST /api/v1/wallets/{walletId}:add-funds` | Custom action syntax |
| P0 | `POST /api/v1/wallet/deduct-funds` | `POST /api/v1/wallets/{walletId}:deduct-funds` | Custom action syntax |
| P0 | `POST /api/v1/wallet/transfer` | `POST /api/v1/wallets/{walletId}:transfer` | Custom action syntax |
| P0 | `POST /api/v1/wallet/{userId}/lock` | `POST /api/v1/wallets/{walletId}:lock` | Custom action syntax |
| P0 | `POST /api/v1/wallet/{userId}/unlock` | `POST /api/v1/wallets/{walletId}:unlock` | Custom action syntax |
| P0 | `GET /api/v1/wallet/{userId}/transactions` | `GET /api/v1/wallets/{walletId}/transactions` | Resource-oriented |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/wallets` | List all wallets (admin) | P1 |
| GET | `/api/v1/wallets/{walletId}` | Get wallet by ID | P0 |
| PATCH | `/api/v1/wallets/{walletId}` | Update wallet settings | P2 |
| DELETE | `/api/v1/wallets/{walletId}` | Close wallet | P2 |
| HEAD | `/api/v1/wallets/{walletId}` | Check wallet exists | P3 |
| POST | `/api/v1/wallets/{walletId}:freeze` | Freeze wallet (security) | P2 |
| POST | `/api/v1/wallets/{walletId}:unfreeze` | Unfreeze wallet | P2 |
| GET | `/api/v1/wallets/{walletId}/audit-log` | Get wallet audit log | P2 |

---

## 21. WebAuthn Endpoints

### Current Endpoints

| Method | Path | Description | Status |
|--------|------|-------------|--------|
| POST | `/api/auth/webauthn/register/begin` | Begin registration | ❌ Violation |
| POST | `/api/auth/webauthn/register/complete` | Complete registration | ❌ Violation |
| POST | `/api/auth/webauthn/authenticate/begin` | Begin auth | ❌ Violation |
| POST | `/api/auth/webauthn/authenticate/complete` | Complete auth | ❌ Violation |
| GET | `/api/auth/webauthn/credentials` | List credentials | ⚠️ Missing version |
| DELETE | `/api/auth/webauthn/credentials/{credentialId}` | Delete credential | ⚠️ Missing version |
| PATCH | `/api/auth/webauthn/credentials/{credentialId}` | Update credential | ⚠️ Missing version |
| GET | `/api/auth/webauthn/status` | Get WebAuthn status | ⚠️ Missing version |

### Violations

1. **Missing version prefix** - All endpoints need `/api/v1/`
2. **Path-based actions** - `begin`, `complete` should use colon syntax
3. **Deeply nested paths** - `register/begin` should be `registration:begin`

### Required Fixes

| Priority | Current | Fixed | Reason |
|----------|---------|-------|--------|
| P0 | `POST /api/auth/webauthn/register/begin` | `POST /api/v1/auth/webauthn/registration:begin` | Version + custom action |
| P0 | `POST /api/auth/webauthn/register/complete` | `POST /api/v1/auth/webauthn/registration:complete` | Version + custom action |
| P0 | `POST /api/auth/webauthn/authenticate/begin` | `POST /api/v1/auth/webauthn/authentication:begin` | Version + custom action |
| P0 | `POST /api/auth/webauthn/authenticate/complete` | `POST /api/v1/auth/webauthn/authentication:complete` | Version + custom action |
| P0 | `GET /api/auth/webauthn/credentials` | `GET /api/v1/auth/webauthn/credentials` | Version prefix |
| P0 | `DELETE /api/auth/webauthn/credentials/{credentialId}` | `DELETE /api/v1/auth/webauthn/credentials/{credentialId}` | Version prefix |
| P0 | `PATCH /api/auth/webauthn/credentials/{credentialId}` | `PATCH /api/v1/auth/webauthn/credentials/{credentialId}` | Version prefix |
| P0 | `GET /api/auth/webauthn/status` | `GET /api/v1/auth/webauthn` | Version + simplify |

### Missing Endpoints (Must Add)

| Method | Path | Description | Priority |
|--------|------|-------------|----------|
| GET | `/api/v1/auth/webauthn/credentials/{credentialId}` | Get single credential | P1 |
| HEAD | `/api/v1/auth/webauthn/credentials/{credentialId}` | Check credential exists | P2 |
| POST | `/api/v1/auth/webauthn/credentials/{credentialId}:verify` | Verify credential | P2 |

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

1. **Standardize URL versioning** - Add `api/` prefix to all endpoints
2. **Fix custom action syntax** - Convert `/verb` to `:verb`
3. **Add missing Get-by-ID endpoints** - ApiKeys, Sessions, Entitlements

### Phase 2: High Priority (P1) - Week 3-4

1. **Standardize pagination** - Implement cursor-based pagination
2. **Add missing CRUD operations** - Update, Delete where applicable
3. **Standardize error responses** - Implement RFC 7807

### Phase 3: Medium Priority (P2) - Week 5-6

1. **Convert path filters to query parameters**
2. **Add HEAD methods for existence checks**
3. **Implement batch operations**

### Phase 4: Low Priority (P3) - Week 7-8

1. **Field naming convention review** (breaking change analysis)
2. **Add optional enhancement endpoints**
3. **Documentation updates**

---

## Appendix A: Complete Endpoint Migration Table

| Current Path | New Path | Priority | Notes |
|--------------|----------|----------|-------|
| `POST /api/auth/api-keys/{keyId}/revoke` | `POST /api/v1/auth/api-keys/{keyId}:revoke` | P0 | Custom action |
| `POST /v1/auth/sign-up` | `POST /api/v1/auth:sign-up` | P0 | API prefix |
| `POST /v1/auth/mfa/setup/totp` | `POST /api/v1/auth/mfa/totp:setup` | P0 | Custom action |
| `GET /api/entitlements/check/{productId}` | `GET /api/v1/entitlements:check?productId=X` | P0 | Query param |
| `PATCH /api/v1/payments/{id}/cancel` | `POST /api/v1/payments/{id}:cancel` | P0 | Custom action |
| `POST /api/v1/wallet/create` | `POST /api/v1/wallets` | P0 | Standard create |
| `POST /api/auth/webauthn/register/begin` | `POST /api/v1/auth/webauthn/registration:begin` | P0 | Custom action |

*See individual sections for complete migration details.*

---

## Appendix B: Summary Statistics

| Metric | Count |
|--------|-------|
| Total Endpoints Analyzed | 200+ |
| Versioning Violations | 50+ endpoints |
| Custom Action Syntax Violations | 30+ endpoints |
| Missing Standard Methods | 50+ operations |
| Path Filter Violations | 15+ endpoints |
| Missing Endpoints to Add | 60+ operations |

---

**Report prepared for:** GameGuild Development Team  
**Next review date:** Q2 2026 (after Phase 4 completion)
