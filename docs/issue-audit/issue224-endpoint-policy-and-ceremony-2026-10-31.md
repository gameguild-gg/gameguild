# #224: production endpoint rules and owner acceptance ceremony

References #224. Full issue acceptance remains pending the owner-run public-testnet ceremony.

The real EVM anchoring pipeline (registry contract, transactional certificate outbox, durable
PostgreSQL journal, persist-before-send publication worker, allowlisted environment signer, bounded
JSON-RPC transport, EIP-1898 pinned proof verifier and authenticated owner-scoped evidence API) is
delivered on develop via PRs #903, #906, #907, #908, #910, #911, #912 and later improvements; this
increment hardens the last code-level boundary and supplies the missing operator procedure.

Production host activation (`AddCertificateEvmHost`, the only composition path that can enable the
EVM pipeline in an API host) now enforces `EvmCertificateRpcEndpointPolicy` server-side request rules
before any RPC call is made: absolute HTTPS only, the reserved `localhost` name and `*.localhost`
rejected, and address literals classified fail-closed against loopback, RFC 1918 private, link-local
(including cloud instance metadata), RFC 6598 shared, documentation, benchmarking, IETF-reserved,
6to4/NAT64 translation, unique-local, site-local, multicast and broadcast ranges, IPv4-mapped IPv6
forms included. Classification is literal-only at registration with no DNS resolution, preserving
registration's no-network/no-key/no-database contract, and violations surface only as the redacted
invalid-configuration failure. Errors never echo the endpoint or host. The module-level transport
registration intentionally remains reachable by the isolated loopback HTTPS/Anvil interop fixtures,
which exercise the actual publication worker against a controlled-boundary bridge; existing unit
fixtures that use reserved non-resolving DNS placeholders remain valid because they are hostnames,
not address literals.

`docs/api/evm-certificate-anchoring-ceremony.md` records the owner-only acceptance procedure: key
generation outside any agent session with owner/signer role separation, pinned Foundry/Solidity
deployment of the reviewed registry source, public issuer-identity derivation, deployed runtime
SHA-256 capture (distinct from keccak code hash and compiler placeholder output), environment-variable
secret injection under the configured reference, host activation with the captured identity, firing
the acceptance anchor and revocation through the application, independent cross-checks at a finalized
block, four operational recovery drills (crash before broadcast, original-profile availability,
quarantined nonce gap, discovery after journal loss) and the exact redacted evidence pack that closes
#224. The runbook documents why no `DeployedAtBlock` setting exists: deployment identity is bound by
the pinned runtime-code digest plus the finalized-block policy, which subsumes a deployment height.

The anchoring and verification configuration docs now state the public-endpoint requirement, mark the
authenticated status/proof gate delivered, and link the ceremony.

Tests: local non-PG unit definitions covering the policy (public IPv4/IPv6 acceptance; rejection of
localhost names, loopback, private, metadata, documentation, shared, multicast, reserved, mapped and
translation literals; scheme and construction-layer fail-closed behavior; no endpoint echo), host
activation fail-closed for localhost/loopback/private/mapped endpoints with endpoint values absent
from the thrown message, and module transport registration remaining reachable for the loopback
interop fixtures. No schema, HTTP contract, workflow or OpenAPI change is introduced; no secret,
funded key, RPC credential or external transaction is created by this increment, and no agent
session performs any ceremony step.
