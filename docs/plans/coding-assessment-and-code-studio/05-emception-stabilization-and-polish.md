# Part 05. Emception integration stabilization and polishing

Status: planned; execution waits for accepted parts 03 and 04.
Updated: 2026-11-24. Scheduling decisions: CS-ADR-043/050.

## Invariants and boundaries

1. Prove the basic official coding/grading journey in part 04 first. This phase
   cannot absorb an unresolved core correctness, privacy, authorization,
   frozen-submission, release or worker-ownership failure to make part 04 green.
2. Emception remains an independent tool. Preserve its IDE, public exports,
   presets, defaults, toolchains and consumers, including
   `@game-guild/emception-ui`. No removal, decomposition or grading-specific
   restriction of that product is authorized.
3. Register every Emception-related finding here, but repair it in its actual
   owner module. A defect in owned composition is not automatically an SDK bug.
   An upstream change needs a separate, recorded, exact-file authorization from
   the responsible owner before implementation. Existing narrow authorizations
   do not extend to new files or artifact recipes by implication.
4. Coding owns technical contracts; grading owns academic policy and results;
   the host owns identity, persistence and official commands. Local diagnostics
   never become official grades. No new compiler, judge or private SDK import.
5. Keep failed reports and independent oracles. Unsupported capability means
   explicitly unavailable, not fallback execution, a zero score or hidden error.
6. Preserve published revisions, submissions and historical migrations. No
   schema/platform/dependency change is anticipated. A demonstrated need stops
   the affected row at its existing ownership/schema gate.

The [Meta-Harness governance](./03-coding-surface/06-refoundation/GOVERNANCE.md)
and root/app `AGENTS.md` apply, including Stop & Ask and Builder/Auditor review.
Every row follows specification -> contract/schema -> independent RED -> minimum
implementation -> terminal quality gate. Validate contracts before behavior.

## Directory topology and dependency graph

| Path | Responsibility / boundary |
| --- | --- |
| `05-emception-stabilization/` | phase specification, finding dispositions and execution state |
| `03-coding-surface/` and `04-end-to-end-validation/` | retained source evidence and their own phase progress; not competing polish backlogs |
| `packages/features/coding/` | pure coding contracts and native source composition |
| `packages/features/grading-adapter-code/` | pure coding-to-generic-grading translation |
| `packages/features/coding-surface/` | owned local workbench and public runtime integration |
| `apps/web/` and owned coding API modules | host composition and authoritative assessment integration |
| `tools/emception/` | independent upstream; read-only until a specific repair is authorized |
| `packages/infrastructure/ui-emception/` | preserved UI; regression target, not a refactoring target |

```text
apps/web -> coding-surface -> coding
                         -> grading-adapter-code -> coding + grading
                         -> supported Emception public APIs
emception-ui -> supported Emception public APIs
CodeAdapter -> generic assessment ports + authoritative worker boundary
authorized grading worker -> public runtime + shared pure native composition
```

No arrow reverses into the host. Generic grading and platform modules never
import coding/UI/Emception. Existing duplication is reviewed, not automatically
consolidated; update the [duplication register](./DUPLICATION-REGISTER.md).

## Entry and non-goals

- Part 03 has its required UX/runtime acceptance; part 04 has actual official
  `function` and `standard-io` evidence, including wrong/error/access controls,
  review, release and learner refresh. Local-only proofs do not satisfy entry.
- Named scheduling exception (CS-ADR-050): the user deferred investigation of
  global preference offline errors to this phase. Carry the nonpassing C3
  report explicitly through parts 03/04's core handoff, rather than requiring
  its repair before 5.0 can investigate it. This permits independent core
  continuation, not clean C3/final acceptance or a broader grading/security
  waiver. Triage/ownership is 5.0; any approved consumer fix and unchanged C3
  regression is 5.3; reconcile closure at 5.4. No global-code edit is authorized
  merely by this scheduling decision.
- Carry the reconciled commit, source/artifact hashes, offered capability matrix
  and all limitations from the part 04 handoff into the finding register.
- This phase fixes and polishes the retained scope. It is not language expansion,
  Code Studio reconstruction, a visual grading engine, or independent IDE
  deployment. Those remain parts 06/07 or separately approved product work.
- Future capabilities such as compound signatures and per-exercise RAM require
  an explicit implement/defer/reject decision. Register them here without
  pretending that registration approves implementation or removes limitations.

## Execution sequence

| Order | Subplan | Gate ID | Required result |
| --- | --- | --- | --- |
| 5.0 | [Findings and owner triage](./05-emception-stabilization/00-findings-and-owner-triage.md) | `CODE-POL-00` | complete inventory, reproducible controls, repair ownership and approved scope |
| 5.1 | [Execution correctness and capabilities](./05-emception-stabilization/01-execution-correctness-and-capability-contracts.md) | `CODE-POL-01` | native-error semantics and capability contracts validated without regressions |
| 5.2 | [Graphics lifecycle and cleanup](./05-emception-stabilization/02-graphics-lifecycle-and-cleanup.md) | `CODE-POL-02` | original graphics/lifecycle oracles pass for the retained scope |
| 5.3 | [Resources and preserved consumers](./05-emception-stabilization/03-resource-stability-and-preserved-consumers.md) | `CODE-POL-03` | repeated-execution proof, compatibility and affected UX acceptance |
| 5.4 | [Acceptance and language handoff](./05-emception-stabilization/04-acceptance-and-language-handoff.md) | `CODE-POL-04` | core revalidation, reviewed dispositions and explicit authorization to enter part 06 |

Work one atomic row at a time. A blocked upstream repair leaves its gate open;
independent diagnosis may continue, but no final phase completion is inferred.
Use existing declared libraries and harnesses. New test cases are planned
deliverables, not evidence until they run with nonempty required selections.

## Finding and memory ownership

- [FINDINGS.md](./05-emception-stabilization/FINDINGS.md) is the single mutable
  register for post-04 Emception-related debt, including owned integration
  findings, capability gaps, unverified combinations and resolved regressions.
  Preserve existing IDs and link the original evidence; never delete a failure.
- [state.md](./05-emception-stabilization/state.md) owns only this phase's active
  row, allowed files, commands/exits, owner decisions and exact next action.
  Parts 03/04 keep their implementation ledgers. Do not copy their live status.
- Architectural decisions remain in the shared
  [DECISIONS.md](./03-coding-surface/DECISIONS.md). Link the applicable ADR from
  each repair. CS-ADR-043 supersedes only roadmap numbering/hand-off ownership,
  not historical test results or accepted compatibility constraints.
- Every new finding from parts 03/04 is linked here before their acceptance.
  Critical defects still block the phase where discovered; this is tracking,
  not permission to defer them. Reconcile inventory again at 5.0 and 5.4.

## Quality and exit gates

Apply [QUALITY-GATES.md](./05-emception-stabilization/QUALITY-GATES.md).
Record exit codes, exact IDs, failing and passing evidence, artifact/source
identity, resource measurements and separate owner/human acceptance.

Exit requires all confirmed defects in retained/offered behavior repaired with
their unchanged oracles, applicable terminal gates at exit 0, no missing core
proof, and approval of affected instructor/learner and preserved-consumer UX.
An optional extension may remain explicitly unavailable with a reviewed
disposition and follow-up owner; it is never counted as a fixed defect.
Unresolved safety defects cannot be relabeled optional to close the phase.

The integrated parts 01-04 PR remains unchanged. Corrections from this phase
receive separately scoped, validated PRs to `develop`, coordinated with the
Emception owner when upstream files are affected. Each language still receives
its own part 06 PR. No PR, release or IDE publication is executed by this plan.
