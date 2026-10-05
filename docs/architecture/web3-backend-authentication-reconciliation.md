# Web3 backend challenge and authentication — #292

## Retained scope

#292 is the native child of #291 for backend challenge generation, nonce handling,
signature verification and token issuance. Its five approved criteria remain
authoritative. This implementation also repairs the inherited account/session and
plaintext-token defects that prevented a verified signature from establishing a
usable authenticated session.

| Approved criterion | Implementation and focused evidence |
| --- | --- |
| Generate and store a nonce bound to the wallet and its authentication challenge. | `Web3Service` stores random nonces with canonical wallet, configured origin, message and chain bindings in the actual size-bounded host cache. It rejects generation when either binding was not retained. Bounded-cache storage and capacity tests; actual HTTP challenge requests. |
| Validate the wallet address, signature, chain ID and challenge format. | Shape and mixed-case EIP-55 checksum validation; configured chain allowlist; canonical SIWE message/field checks; actual EIP-191 recovered signer. Unit vectors and actual HTTP invalid input, wrong-key, signature/message/nonce/address/network cases. |
| Expire the challenge after five minutes and reject replay. | Trusted `TimeProvider` supplies generation and verification time. Exact five-minute expiry is rejected. Atomic nonce consumption precedes account or credential writes. Actual HTTP expiry, concurrent verification and subsequent replay tests. |
| Issue the authentication token only after successful verification. | The verified provider key resolves a stored account. Status and tenant membership precede the existing hashed-token generator, actual session binding and versioned JWT. Actual bearer/session access, refresh and second-sign-in continuity tests. |
| Cover invalid signatures, expired/reused nonces, and address/network mismatches. | Actual HTTP/PostgreSQL vectors, real cryptographic signatures and unit cases. Denials cannot create wallet identity, session or token rows. |

Execution receipts and publication acceptance are recorded below when verified.
Test definitions alone do not complete a criterion.

## Concurrent develop integration — 2026-10-05

The reviewed Web3 branch incorporates develop's security revision
`45941e7da3f0995b1011ea92127ced0de895b527`. Its log-redaction implementation and
frontend changes are preserved. Six manually edited generated client modules
revealed drift from the unchanged generator. The module generator now emits the
same shorthand syntax for simple array response assertions consistently; public
method types, HTTP contracts and runtime behavior remain unchanged. A generator
regression test and regenerated modules make that formatting reproducible.
Matching-head integration checks and accepted merge are required before closure.

The same develop revision introduced invalid accesses on `unknown`, an invalid
rest parameter and an unparenthesized mix of `??` and `||` in Web dependencies.
The integration repair uses existing Monaco, Lexical and Vega types, a typed
JavaScript compiler export boundary and valid operator grouping. Existing editor
controls and rendering behavior are retained. Web runtime dependencies and the
complete Web typecheck pass locally. Obsolete ESLint suppression counts are only
removed as the corresponding violations disappear; no new suppression or disabled
rule is introduced. The complete integrated .NET/SDK receipts pass 7,810 distinct
cases: Authentication2256/Authorization1667/SharedKernel1389/APIunit1050/
Integration327/SDK1121, including35 new Web3 cases and2 generator regression cases.

The full Economy profile exposed an existing five-minute outer deadline for the
entire API integration suite. Its retained timing receipt records termination at
300seconds with status124, while the same327-case suite passes its independent
API check in8min24s. Both API test projects now use the existing bounded12-minute
API suite deadline; the five-minute individual-test hang guard and all assertions
remain active. A shell regression exercises integration, unit and ordinary project
deadline selection. No test project, coverage requirement or release stage is skipped.

The Testing Lab browser fixture also generated a 51-character reviewer username,
which the existing 50-character API handle limit correctly rejected. Its shorter
fixture prefix preserves the complete unique tag and role, with a guard before
sign-up. Production validation is unchanged. All 64 Economy shell regressions and
23 Testing Lab runner/quality cases pass locally, including five new username cases.

## Account and credential boundaries

The insert-only `web3` external-login provider key is the lowercase verified
address. Subsequent sign-ins resolve the same stored user. A missing linked user,
inactive/suspended/deleted account or unavailable requested tenant receives generic
401 denial. A matching `address@web3.local` identifier never automatically links
an existing email account; wallet possession does not verify an email address.
New wallet accounts have an unverified internal identifier and no password.
Explicit linking and multiple-wallet product flows remain in #291.

The existing JWT generator persists one hashed refresh credential. Web3 no longer
writes an additional plaintext copy. Session creation uses the actual session GUID
and the same token hash; JWT claims carry that session, the stored user version and
resolved tenant/roles. Optional tenant and device-fingerprint inputs reach the
service through the existing command handler. The public request/response routes
and contracts remain compatible.

The actual host command transaction encompasses account, provider link, membership,
token and session writes. A required binding failure cannot return credentials or
record success. A test injects the fault only after a real stored token/session
binding, then verifies rollback of identity and credential rows. The consumed nonce
remains consumed after rollback; a fresh challenge is required to retry.
Cancellation propagates before persistence and through required dependencies.

Expected verification/account denials use the existing sanitized security exception
and HTTP 401 mapping. Known invalid challenge address/chain inputs use the existing
validation exception and HTTP 400 mapping. Real storage failures retain HTTP 500.

## Configuration and deployment contract

`Authentication:Web3:Siwe:Origin` is mandatory. It accepts an HTTPS origin, or an
HTTP loopback origin for local development. Configured allowed chain IDs govern
generation and verification; the existing default is Ethereum chain 1. The statement
is host configured. SIWE fields and byte-for-byte canonical messages are checked
before signer recovery and nonce consumption.

Nonce storage and atomic consumption are process local. Requests that cannot find
the wallet/nonce binding fail closed, including eviction and restart. A deployment
with multiple API processes needs routing affinity for the challenge/verification
pair, or a separately accepted shared nonce store. These tests do not establish
distributed-store, replica-routing or live wallet-provider acceptance. No private
wallet key or raw authentication token is persisted in test receipts.

The implemented signer boundary is EOA EIP-191 with SIWE formatting. Contract-wallet
verification, EIP-712, frontend/provider adapters, explicit linking, multiple wallets,
disconnection, per-address limits, metrics and complete product journeys remain
under #291 and its provider/UI children. #263 retains its remaining original
all-provider lifecycle/audit/retention criteria.

## Executed verification

Fresh final source passes **7,796 distinct .NET/SDK cases**, including **35 new cases**:

| Suite | Passing cases |
| --- | ---: |
| Authentication unit | 2,256 |
| Authorization unit | 1,667 |
| SharedKernel unit | 1,377 |
| API unit, architecture and security | 1,050 |
| Entire API integration suite | 327 |
| SDK | 1,119 |

Integration includes **20 actual Web3 HTTP/PostgreSQL cases** and **15 full-application
OpenAPI HTTP cases**. Neither is counted again in the total. The full solution build
has zero warnings/errors with analyzers enabled. EF reports no pending model change.
The entire exported OpenAPI document equals accepted #695, with 1,297 paths and
1,656 schemas. Fresh SDK typecheck, generation and committed-client consistency pass.

The actual pre-fix HTTP baseline failed challenge generation with 500; unchanged
registered-service diagnostics identified missing cache sizes, plaintext duplicate
refresh storage and absent account/session persistence. Failed intermediate receipts
and the passing preliminary 14-case subset are retained and excluded from final
counts. Logs, TRX/source hashes and `local-proof.json` remain under
`artifacts/test-results/issue-292-web3-20261005/`. Matching-head CI and merge are
pending; #292 stays OPEN until accepted. #291/#263 retain their remaining criteria.

## Standards

The message and checksum boundaries follow [EIP-4361](https://eips.ethereum.org/EIPS/eip-4361)
and [EIP-55](https://eips.ethereum.org/EIPS/eip-55). The existing Nethereum signer and
SIWE libraries remain in use; no new signing implementation or dependency is added.
