# 5.0. Findings, baseline and owner triage

Status: planned. Gate: `CODE-POL-00`.
Prerequisite: accepted part 04, not merely passing local execution.
Inherit the [coordinator](../05-emception-stabilization-and-polish.md).
The core handoff must explicitly carry CS-ADR-050's deferred global offline
investigation and the nonpassing C3 report; that exception is not a passing
test or a deferral of core grading/security failures.

## Atomic execution

| Slice | Specification | Contract / schema | Independent test oracle | Minimum implementation boundary | Terminal quality gate |
| --- | --- | --- | --- | --- | --- |
| `POL00-A` | account for every Emception-related finding from parts 03/04 | stable ID, classification, expected/observed result, owner, provenance and linked reproducer | compare source ledgers/gaps/part 04 reports with the register; omitted IDs or unavailable evidence fail the audit | documentation and existing harness fixtures only | G-POL-DOC + G-POL-BASELINE |
| `POL00-B` | separate retained defects from unavailable extensions/product proposals | exact repair scope and owner approval, implement/defer/reject decision, affected consumers and mandatory oracle | validate each negative reproducer against the frozen expectation; no synthetic pass or missing required control | promote necessary minimal reproducers into existing tests; no runtime repair before approved contract | G-POL-CONTRACT + focused named existing-harness RED, recorded in state |

## Required triage

1. Capture reconciled HEAD/worktree and review all source/ABI/manifest identities.
   Preserve original failed evidence. Detect newer upstream fixes by reproducing,
   not by assuming that a develop merge solved them.
2. Reconcile the register, gap inventory, duplication register, capability matrix
   and part 04 reports. Add all new findings before choosing a repair. Confirm
   that already corrected native phase/ABI/budget issues are regressions, not
   automatically reopened defects.
3. Assign coding composition, host, grading worker, SDK or artifact ownership.
   Record a named reviewer and exact permitted files before upstream work.
   The polishing request alone is not approval to change protected paths.
4. Freeze independent expected outcomes and required case IDs. Put reproducible
   minimal synthetic controls into the existing harness when their prior local
   archive is not portable. No dependency installation or new human CLI.
5. Review the unavailable RAM/compound features and optional preparation/file/
   assistance proposals. An explicit defer is valid for a new capability, not
   for a defect in retained behavior. Link any later product plan and owner.
6. Investigate the user-deferred `CS-HOST-OFFLINE-PREFERENCES`: reproduce the
   rejected Server Action during loading/offline/reconnect/unmount, confirm the
   actual caller, and assign global-web ownership before any code edit. Theme,
   accessibility and editor synchronizers are candidate roots, not established
   causes. Record the original C3 failure, exact repair authorization and
   isolated negative controls for handoff to 5.3. Reconcile the dynamic-import
   warning and protected-read 403 separately; no permission relaxation follows
   from the offline deferral.

## Exit

Every source finding maps to a register entry and repair/decision subplan.
Required core failures retain their earlier block. Contracts/fixture checks
pass before implementation; behavioral RED is the expected defect, not a
bootstrap failure. Record exact commands, inventory audit and approvals in
[state.md](./state.md). Missing owner decisions block the dependent repair.
