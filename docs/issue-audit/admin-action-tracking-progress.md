# Admin action tracking implementation progress

Issue: [#150 Admin Action Tracking](https://github.com/gameguild-gg/gameguild/issues/150)

## Implemented in this change

- Persists TamperEvidentAuditLog rows in TamperEvidentAuditLogs.
- Enforces a unique per-tenant sequence and links each event to its predecessor.
- Includes before/after snapshots, actor, session ID, correlation ID, request origin, and action details in the content hash.
- Signs each chain hash with ECDSA/SHA-256. Verification checks the content, sequence, previous-hash link, and signature.
- Retries database write conflicts up to three times and returns a failure when persistence cannot be completed.
- Keeps signing private keys in deployment configuration rather than the application database.

Signing options are read from the AuditSigning configuration section. Provision at least one active signing key through the deployment secret manager; do not commit private key material:

    AuditSigning:
      ActiveKeyId: audit-2026-10
      Keys:
        audit-2026-10:
          PrivateKeyPem: <private key from secret manager>
          PublicKeyPem: <public key>

Keep each retired key's public key available while logs signed by that key remain in the chain. A rotation target must be provisioned before RotateSigningKeyAsync can select it. Update the configured active key as part of the deployment so the selection survives a restart.

Callers must pass minimized, redacted snapshots. The service stores the supplied snapshots as-is. A failed write returns a failure result; privileged-operation handlers still need to decide how to handle that result before this service can provide end-to-end audit guarantees.

## Remaining acceptance criteria

This change does not complete #150. The issue remains open until the product also has:

- instrumented coverage of administrative operations across modules, with callers handling audit-write failures;
- critical-action alerts and real-time notifications;
- approval/change-workflow tracking and privileged-access review/certification;
- administrative reporting, analytics, and anomaly detection;
- identity-management lifecycle integration;
- operational key provisioning and a deployed migration;
- an external immutable/WORM or independently anchored destination that can detect removal of the tail of a database hash chain.

The signed chain makes changes to retained entries detectable, but a database-only chain cannot prove that its final rows were not deleted. Do not describe this storage alone as tamper-proof.
