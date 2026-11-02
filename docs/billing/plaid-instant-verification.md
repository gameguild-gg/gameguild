# Plaid Instant Bank-Account Verification (issue #414 — optional)

Status: **port delivered, real adapter pending external credentials.**

## What exists

US bank-account payment methods verify through the **micro-deposit challenge**
(two deposits of $0.01–$0.99, 3-business-day deadline, 3-attempt cap) delivered
with issue #1030:

- Entity: `BankAccountVerification` (`apps/api/Source/Modules/GameGuild.Commerce.Payments/Entities/BankAccountVerification.cs`)
- Commands: `InitiateBankAccountVerificationCommand` / `VerifyBankAccountDepositsCommand`
  (`Commands/BankAccountVerification/BankAccountVerificationCommands.cs`)
- Charge gate: unverified bank accounts are excluded from charge candidates
  (`Services/PaymentMethodChargeResolver.cs`)

This document covers the **optional** instant-verification path.

## The port

`IPlaidInstantVerification` (`Services/Plaid/IPlaidInstantVerification.cs`) is the
seam a real Plaid adapter plugs into:

```csharp
public interface IPlaidInstantVerification
{
    bool IsConfigured { get; }
    Task<Result<PlaidLinkToken>> CreateLinkTokenAsync(Guid tenantId, Guid userId, CancellationToken ct = default);
    Task<Result<PlaidInstantBankAccount>> ExchangePublicTokenAsync(string publicToken, CancellationToken ct = default);
}
```

- `CreateLinkTokenAsync` — mints the single-use Link token the web client opens
  the Plaid Link flow with.
- `ExchangePublicTokenAsync` — swaps the public token from the Link success
  callback for the verified account details (bank name, last4, account type).
  A success carries verified-on-connection semantics: no micro-deposit
  challenge is needed for that account.

## Configuration

Bound from `Payments:Plaid` (`PlaidInstantVerificationOptions`), fail-closed:

| Key | Default | Meaning |
| --- | --- | --- |
| `Payments:Plaid:IsEnabled` | `false` | Instant verification availability. |
| `Payments:Plaid:Environment` | `sandbox` | `sandbox` or `production`. |
| `Payments:Plaid:ClientId` | *(empty)* | Plaid API client id; required when enabled. |
| `Payments:Plaid:ClientSecret` | *(empty)* | Plaid API client secret; required when enabled. |
| `Payments:Plaid:ApiBaseUrl` | *(derived)* | Explicit override for tests/proxies. |
| `Payments:Plaid:Products` | `["auth"]` | Link products requested. |

Startup validation fails fast when `IsEnabled` is set without credentials, with
an invalid environment, or with a non-absolute base URL.

## Default behavior without credentials

The registered default is `NoOpPlaidInstantVerification`
(`Services/Plaid/NoOpPlaidInstantVerification.cs`):

- `IsConfigured` returns `false`.
- Every call fails with `PaymentMethods.PlaidNotConfigured`
  (`Error.Problem`) so callers deterministically fall back to the micro-deposit
  flow — a runtime misconfiguration can never surface as a silent success.

`PaymentsModule` registers the port with `TryAddSingleton`, so a host (or test
fixture) that registers a real adapter before the module runs keeps its
implementation.

## Pending external credentials

A real adapter requires Plaid API credentials (client id + secret), which this
repository does not hold. When credentials are provisioned, deliver the adapter
as:

1. An HTTP client implementation of `IPlaidInstantVerification` following the
   `Services/PayPal/IPayPalApiClient.cs` + `PayPalPaymentGateway.cs` pattern
   (typed responses, fail-closed credential checks per call, simulation mode
   for CI).
2. A Link-handshake API surface (create link token → exchange public token →
   register the bank-account payment method as verified), reusing the existing
   `PaymentMethodRegistryService` upsert and writing a
   `BankVerificationCompleted`-equivalent audit row
   (`IRevenueAuditService`, action `BankVerificationInstant`).
3. Tests mirroring `BankAccountVerificationTests` (challenge bounds, audit,
   ownership, charge-gate) against the simulated adapter.

Until then, the micro-deposit flow remains the verification path and this port
exists so that step touches configuration, not domain code.
