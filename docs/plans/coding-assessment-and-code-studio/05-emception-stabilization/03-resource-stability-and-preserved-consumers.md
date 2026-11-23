# 5.3. Repeated execution, resources and preserved consumers

Status: planned. Gate: `CODE-POL-03`. Depends on `CODE-POL-02`.
Inherit the [coordinator](../05-emception-stabilization-and-polish.md).

## Atomic execution

| Slice | Specification | Contract / schema | Independent test oracle | Minimum implementation boundary | Terminal quality gate |
| --- | --- | --- | --- | --- | --- |
| `POL03-A` | a live workbench can repeatedly execute, cancel, switch and unmount safely | one execution owner; pending teardown barrier; frozen context keys; independent resource measurement contract | five actual native cycles in the same live consumer after warmup; at most one active compiler and zero retained workers; bounded memory/listeners; input cancellation and stale callbacks remain negative controls | existing runtime/workbench harness and actual owning lifecycle module; upstream changes need approval | G-POL-STABILITY + G-POL-CONSUMERS + affected G-POL-UPSTREAM |
| `POL03-B` | corrections preserve the independent Emception tool and usable owned UX | public compatibility/defaults, read-only/privacy boundaries and reviewed duplication dispositions | current IDE/UI/workspace/browser checks pass; real desktop/mobile instructor/learner tasks show no clipped controls, lost code or resource regression | focused repair and evidence only; no automatic consolidation or IDE replacement | G-POL-CONSUMERS + G-POL-UPSTREAM + G-POL-DOC; separate human task review |

## Resource proof

Resolve `CS-RUNTIME-REPEATED-EXECUTION`, `CS-RUNTIME-WORKBENCH-OWNERSHIP` and
`CS-EMCEPTION-DUPLICATION-REVIEW` from [FINDINGS.md](./FINDINGS.md).
`POL03-B` also owns the owner-reviewed disposition/cleanup of
`CS-EMCEPTION-STRICT-LINT`; pre-existing warnings are not a clean strict gate.
Five offline edits, one context replacement or cold contexts per control are
not five repeated native executions. Add the missing exact sequence to the
existing harness; retain its existing oracles and previous failed attempts.

Freeze warmed JS/DOM/listener/RSS measurements and thresholds before repair.
Distinguish collectible compiler memory from active ownership leaks, and do
not claim physical memory from blindly summed shared-process RSS. Missing
metrics cannot count as zero. Resource stops are nonpassing evidence.

Use the existing OS-friendly policy: reserve at least two CPU cores where
available and available RAM of at least max(4 GiB, 15% of host RAM), watch memory
pressure/swap/OOM, and run one heavy job/browser/compiler at a time. Do not
allocate all available RAM/swap or use the old 14 GB grant as a target. Stop
only owned jobs on sustained pressure. Never alter `dev:fast` or kill unrelated
developer processes to accommodate this proof.

## Compatibility and UX

Run independent tool and consumer suites against the corrected sources and
matching actual artifacts. Code copies may remain intentionally divergent;
review the duplication register with the Emception owner, not a generic DRY
refactor. Exercise tab/file operations, offline editing, terminal/canvas,
repeated navigation, focus, keyboard and narrow viewports in affected surfaces.
New assistance/file-preparation features are separately approved product work,
not prerequisites invented for this repair.

## Exit

Repeated-execution oracles and preserved-consumer commands return 0, with
resource evidence and no required skips. The Auditor verifies source/artifact
identity and unchanged negative controls. Human task acceptance stays separate.
Any actual worker leak or false official outcome blocks the retained flow
immediately; no performance-only classification may conceal a safety defect.
