# Certificate anchoring acceptance ceremony — issue #224 (owner runbook)

Status: **runbook for the repository owner.** Everything in this document is a
manual, owner-performed step. No agent session executes any part of it: keys are
generated outside agent sessions, secrets are injected as environment variables,
and protected RPC/signing references never enter issues, PRs, chats or source.

The entire pipeline this ceremony activates already exists on `develop`:

| Stage | Component |
| --- | --- |
| On-chain registry | `contracts/certificate-anchoring/src/CertificateAnchorRegistry.sol` (`anchor(bytes32)`, `revoke(bytes32)`, `getAnchor(bytes32)`) |
| Canonical PII-free digest | `BlockchainCertificateCanonicalizer` (SHA-256 of `{id, issued-at, issuer, recipient-hash, version}`) |
| Transactional intent staging | `CertificateEvmEvidenceProducer` → certificate outbox events `CertificateEvmAnchorRequestedV1` / `CertificateEvmRevocationRequestedV1` |
| Durable journal (PostgreSQL) | `EvmCertificateJournal` + `EvmCertificateJournalDbContext` (operations, attempts, nonces, events) |
| Publication worker | `EvmCertificatePublicationWorker` (persist-before-send, nonce reservation, fee-capped replacement, reorg reconciliation) |
| Signer | `EvmCertificateSoftwareSigner` reading only its allowlisted environment variable via `EvmCertificateEnvironmentKeySource` |
| RPC transport | `EvmCertificateRpcTransport` (bounded, fixed-method JSON-RPC over HTTPS; loopback/private/reserved endpoints rejected at production registration by `EvmCertificateRpcEndpointPolicy`) |
| On-chain verification | `EvmCertificateProofVerifier` (EIP-1898 pinned receipts/events/state at a finalized or confirmation-qualified block) |
| Evidence API | `GET /api/certificates/{id}/evm` and `GET /api/certificates/{id}/evm/proof` (`CertificateEvmEvidenceController`) |

Failure of any anchoring step never blocks certificate issuance or revocation;
the journal keeps failures visible for this ceremony to observe.

## 0. Invariants (read first)

1. **Never** place a private key, funded key file, or credential-bearing RPC URL
   in source, appsettings, issues, PR descriptions or agent chats. Configuration
   keys reference an environment variable *name* only.
2. The only on-chain payload is the 32-byte SHA-256 digest of the canonical
   PII-free certificate payload. Recipient names, user/tenant ids and revocation
   reasons never leave the database.
3. Sepolia (chain `11155111`) must use `FinalityMode: Finalized`. There is no
   confirmation-count fallback on Sepolia.
4. There is no `DeployedAtBlock` setting by design: deployment identity is bound
   by the pinned **SHA-256 of the deployed runtime code** plus the finalized-block
   read policy, which is strictly stronger than a deployment height (no block
   before deployment can carry the pinned code, and unfinalized history is never
   accepted as evidence).
5. Production hosts cannot target loopback/private/reserved RPC endpoints
   (`EvmCertificateRpcEndpointPolicy`, enforced in `AddEvmCertificateVerification`).
   The loopback Anvil/TLS fixtures are CI-only and construct transports directly.
6. No mainnet deployment, no payment, no W3C credential signing is part of this
   ceremony or the implementation.

## 1. Prerequisites

- A public Sepolia HTTPS JSON-RPC endpoint you control or trust (endpoint host
  must be public — not localhost, not an internal/private address).
- Foundry pinned exactly as the hosted contract workflow
  (`.github/workflows/certificate-anchoring-contract.yml`): Foundry `v1.8.5`,
  Solidity `0.8.37`, Paris EVM target. Use the same checkout of this repository
  for deployment so the deployed bytecode matches the reviewed source.
- Sepolia ETH for three addresses (test amounts only; anchor/revoke spend is a
  few centimes at the documented caps).
- The GameGuild API host configured with its three connection strings
  (`DefaultConnection`, `AuthenticationDb`, `MigrationConnection`) and
  DataProtection certificate environment variables.

## 2. Key generation — outside any agent session

Perform on hardware you control, in a shell no agent shares. Three keys, three
roles (the contract enforces owner/signer separation; administration does not
grant signing):

1. **Deployer/Admin key** — deploys the registry and later rotates signers.
2. **Issuer signer key** — the operational key the API host uses for
   `anchor`/`revoke`. This is the only key the application ever references.
3. (Optional) **Recovery admin key** — cold standby for ownership transfer.

Example (Foundry; any equivalent offline generator is fine):

```sh
cast wallet new   # repeat per role; record address + private key separately
```

Keep each private key in your secret store. The issuer signer key is the only
one that becomes an environment variable on the API host, under the exact name
you will configure as `BlockchainCertificates:Evm:PrivateKeyEnvironmentVariable`
(template default: `GAMEGUILD_CERTIFICATE_TESTNET_KEY`).

Fund all three addresses with test ETH from a Sepolia faucet.

## 3. Contract deployment and identity recording

From your checkout of this repository (do not modify the contract source):

```sh
export SEPOLIA_RPC="<your public HTTPS Sepolia endpoint>"   # never commit this value
ADMIN="<deployer/admin address>"
SIGNER="<issuer signer address>"

# Public issuer identity: 32 bytes, recorded once, immutable on chain.
# Use the same string you will set as BlockchainCertificates:Evm:CanonicalIssuer.
export CANONICAL_ISSUER="gameguild-testnet"
ISSUER_ID=$(cast keccak "$CANONICAL_ISSUER")

forge create contracts/certificate-anchoring/src/CertificateAnchorRegistry.sol:CertificateAnchorRegistry \
  --rpc-url "$SEPOLIA_RPC" --private-key "<ADMIN KEY>" \
  --constructor-args "$ISSUER_ID" "$ADMIN" "$SIGNER" \
  --compiler-version 0.8.37 --via-ir
```

Record, for the evidence pack and the configuration:

```sh
REGISTRY="<deployed address from forge output>"

# SHA-256 of the ACTUAL deployed runtime bytes (not the compiler artifact with
# immutable placeholders, not Ethereum's keccak code hash):
cast code "$REGISTRY" --rpc-url "$SEPOLIA_RPC" | sed 's/^0x//' | tr -d '\n' \
  | python3 -c "import sys,hashlib;print(hashlib.sha256(bytes.fromhex(sys.stdin.read())).hexdigest())"

# Confirm the deployed identity through the contract itself:
cast call "$REGISTRY" "PROTOCOL_VERSION()(uint256)" --rpc-url "$SEPOLIA_RPC"   # must return 1
cast call "$REGISTRY" "issuerId()(bytes32)"      --rpc-url "$SEPOLIA_RPC"      # must return $ISSUER_ID
cast call "$REGISTRY" "authorizedSigners(address)(bool)" "$SIGNER" --rpc-url "$SEPOLIA_RPC"  # must return true
```

## 4. Host configuration and secret injection

In the API host environment (environment variables / secret store — not in any
committed file):

```sh
export GAMEGUILD_CERTIFICATE_TESTNET_KEY="<issuer signer private key>"
```

Configuration (template from `docs/api/blockchain-certificate-anchoring.md`,
now with the real values recorded in step 3):

```json
"BlockchainCertificates": {
  "Provider": "none",
  "Evm": {
    "Enabled": true,
    "Profile": "sepolia-certificates-v1",
    "CanonicalIssuer": "gameguild-testnet",
    "Network": {
      "ChainId": 11155111,
      "RpcEndpoint": "<your public HTTPS Sepolia endpoint>",
      "ContractAddress": "<REGISTRY>",
      "IssuerId": "<0x-prefixed $ISSUER_ID>",
      "ExpectedRuntimeCodeSha256": "<SHA-256 from step 3>",
      "FinalityMode": "Finalized"
    },
    "SignerReference": "certificates-testnet-v1",
    "PrivateKeyEnvironmentVariable": "GAMEGUILD_CERTIFICATE_TESTNET_KEY",
    "GasLimit": 200000,
    "MaximumFeePerGasWei": "30000000000",
    "MaximumTransactionFeeWei": "6000000000000000",
    "InitialFeePerGasWei": "20000000000",
    "InitialPriorityFeePerGasWei": "1000000000"
  }
}
```

Fee fields are **caps, not estimates** — raise them only after checking current
Sepolia base fee. The worker never exceeds an operation's captured caps.

Startup order is enforced: journal migrations/readiness run before any hosted
service (including the publisher). An unreadable or missing journal schema aborts
startup. On first enable, verify the journal tables exist in the
`gameguild.authentication` schema.

## 5. Fire the acceptance

1. **Anchor.** Issue the designated acceptance certificate through the normal
   application flow (course completion). The certificate outbox stages the
   anchor intent in the same database transaction; the publication worker
   reserves a nonce, signs, broadcasts, then confirms the receipt, event and
   registry state at a **finalized** block. Sepolia finality lags by minutes;
   poll until the operation reports `Confirmed`.
2. **Verify via the application.** As the certificate owner (or a system admin):

   ```sh
   curl -H "Authorization: Bearer <token>" \
     "https://<api-host>/api/certificates/<certificate-id>/evm/proof"
   ```

   Expect `status: Observed`, `validity: Valid`, an anchor operation view with
   the transaction hash, and a proof whose evidence carries the transaction
   block, signer, issuer and registry state.
3. **Revoke.** Revoke a second acceptance certificate through the application.
   The revocation intent reuses the original digest/profile; after finalization
   the anchor proof for that certificate must flip to `Revoked` validity
   (`Invalid`), proving finalized revocation semantics end to end.
4. **Independent cross-check.** Compare the application's evidence against a
   direct chain read at the finalized block:

   ```sh
   cast receipt "<anchor tx hash>" --rpc-url "$SEPOLIA_RPC"
   cast block finalized --rpc-url "$SEPOLIA_RPC"
   cast call "$REGISTRY" "getAnchor(bytes32)((address,address,uint256,uint256))" \
     "<certificate digest 0x…>" --block finalized --rpc-url "$SEPOLIA_RPC"
   ```

## 6. Operational recovery drills (required acceptance evidence)

Run each drill once, on test certificates, and record the outcome:

1. **Crash before broadcast.** Issue a certificate and stop the API host before
   the worker broadcasts (immediately after issuance). Restart. Expected: the
   worker reuses the persisted signed attempt and nonce — the same transaction
   hash — and never re-signs a fresh candidate for the unacknowledged send.
2. **Original-profile availability.** Restart the host mid-operation with the
   same profile. Expected: operations resume against the original captured
   identity; any mismatch fails visibly (`NeedsAttention`), never rebinds.
3. **Quarantined nonce gap.** Send one unrelated transaction from the issuer
   signer address outside the application, creating a nonce gap. Expected: the
   affected operations become visible as `NeedsAttention` with a recorded
   failure (or land after replacement within the captured budget); no silent
   identity replacement, no invented receipt.
4. **Discovery after journal loss.** Delete (or restore an older backup of) the
   four journal tables, then restart. Expected: the durable certificate outbox
   still holds the original intents; the consumer re-enqueues them idempotently;
   already-anchored digests are re-verified rather than re-anchored with a new
   transaction.

## 7. Evidence to attach to #224 (the close-out pack)

Post as an issue comment (or linked artifact) on #224 — public values only:

- Chain id, registry `ContractAddress`, `IssuerId` (or the canonical issuer
  string + derivation command), `ExpectedRuntimeCodeSha256`, and the exact
  repository commit deployed.
- The acceptance certificate's PII-free digest, and the digest of the revoked
  certificate.
- Anchor transaction hash + block number + block hash + timestamp; revocation
  transaction hash + block number + block hash. Prefer receipts captured at a
  `finalized` block tag.
- The application's own proof responses (the `/evm` and `/evm/proof` JSON for
  both certificates) showing `Valid`/`Revoked` as expected.
- The four recovery-drill outcomes with the observed journal statuses.
- Tool versions used (Foundry, Solidity, `cast --version`).

Explicitly **exclude**: the RPC endpoint URL, any private key or key file, the
signer env-var *value* (the variable *name* is fine), and any log line that
echoes request URIs. Configuration/exception messages in this pipeline are
already written never to echo those; keep the pack that way.

Acceptance closes #224 only when the anchored and revoked evidence above is
independently reproducible from a public Sepolia endpoint by anyone holding the
certificate digest.
