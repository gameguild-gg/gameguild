# Application recovery support and deployment boundaries

This document describes the application's recovery interfaces and the boundary
between application verification and an operator's deployment responsibilities.
The maintainer scope review on issue #1310 and PR #1300 supersedes the previous
production-drill proposals in this file.

## Repository responsibilities

GameGuild provides [local Docker Compose services](../../compose.yaml), an
[environment template](../../.env.example), and [local setup instructions](../../README.md).
Deployers choose how to build on that local setup, including a single machine,
Coolify or Kubernetes. The application does not require a particular deployment
platform, storage layout or operational toolchain.

Production backup topology, restore procedures, retention, recovery objectives
(RPO/RTO), deployment gates and drill schedules belong to each deployment's
operator. Their absence from this public application repository is expected and
does not establish a missing implementation or an application release blocker.
This repository makes no finding about the presence or effectiveness of a
particular deployment's backups.

## Application verification evidence

- [Migration rehearsal](migration-rehearsal.md) and its
  [disposable-database script](../../scripts/devops/migration-rehearsal.sh) exercise
  application migration contracts, rollback behavior and data-loss risks on the
  recorded revision. Operators choose their own production migration and recovery
  procedures.
- The [load-smoke harness](../../apps/api/tests/GameGuild.LoadSmoke/) exercises the
  real application host against disposable test infrastructure. Its regression
  thresholds describe that test workload; production capacity and service
  objectives require the operator's own evidence.
- [Authentication flows](../auth/flows/) and the application security tests describe
  authorization, credential handling, signing-key APIs and account recovery.
  These are application contracts that remain subject to bug and security fixes.
- [Outbox cleanup](outbox-cleanup.md) documents application data-lifecycle behavior.
  Retry, dead-letter and replay defects should be established against the actual
  application revision and its tests.

### Public application contract references

- Signing-key rotation is a protected application operation. Its current
  [controller](../../apps/api/Source/Modules/GameGuild.Identity.Authentication/Controllers/KeyRotationController.cs)
  and [service/worker registration](../../apps/api/Source/Modules/GameGuild.Identity.Authentication/Data/Extensions/DependencyInjection.cs)
  define the implemented lifecycle. The earlier wiring finding [#1189](https://github.com/gameguild-gg/gameguild/issues/1189)
  is closed; do not carry its historical unwired-state claim into a current review.
- Dead-letter inspection and explicit replay are exposed by the protected
  [event administration controller](../../apps/api/Source/GameGuild.API/Core/Controllers/EventTransportAdminController.cs).
  The [outbox transport reference](../sharedkernel/03-outbox-core.md) records its
  application model; [#1313](https://github.com/gameguild-gg/gameguild/issues/1313) tracks
  event-transport findings. Replay correctness remains an application concern.
- Webhook secret rotation and overlap validation are implemented by the
  [billing webhook controller](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Controllers/BillingWebhooksController.cs)
  and [signing-secret provider](../../apps/api/Source/Modules/GameGuild.Commerce.Billing/Services/WebhookSigningSecretProvider.cs).
  Operators choose how to provision and protect their deployment's secrets.

Historical rehearsal results describe their recorded test environment and commit.
They do not certify a production deployment or establish its operational readiness.

## Handling deployment-specific material

Organization-specific designs, recovery commands, topology, access details,
secrets and incident evidence belong in the operator's private operational
repository and documentation. Keep them out of public application issues, pull
requests and attachments.

When triaging an operational report, first identify whether the failure is in an
application contract or in the operator's chosen deployment. Consult the relevant
private operational sources with appropriate access before drawing conclusions
about an organization's infrastructure. The application repository does not
prescribe how operators deploy, back up or restore their systems.
