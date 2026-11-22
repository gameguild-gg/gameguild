# 3.6.3. Workbench, runtime and capabilities

Status: planned. Gate: `CODE-UX-03`. Depends on `CODE-UX-02`.

## Invariants and boundaries

Apply [GOVERNANCE.md](./GOVERNANCE.md). Allowed changes are the owned surface,
its public-runtime integration, tests and necessary host composition. Emception
source/UI remain preserved; only declared editor/terminal libraries and public
runtime APIs are available. No local diagnostic becomes an official grade.

## Outcome

Provide a shared execution foundation for purpose-built authoring, solving and
inspection views. Reuse the installed editor/terminal libraries and public
Emception APIs. Do not embed the monolithic IDE, rebuild its docking system or
preserve our existing controller-shaped API when a smaller public port is clearer.

## Atomic sequence and terminal gates

Each row completes Specification -> Contract/schema -> Test -> Minimal
implementation -> Quality gate; see [QUALITY-GATES.md](./QUALITY-GATES.md).

| Slice | Specification | Contract / schema | Test oracle | Minimal implementation | Quality gate |
| --- | --- | --- | --- | --- | --- |
| UX03-A | specify runtime asset preparation and supported operations by family/environment/mode | frozen artifact binding, capability resolver inputs and discriminated available/unavailable states | installed assets pass preflight; wrong identity fails; unsupported combinations unavailable; no workers on form navigation; no official commands on local run | backed-up synchronization of published assets, lazy runtime port and basic workbench regions | contract compilation, G-SURFACE + G-PREFLIGHT; CS-REF-21 before real execution |
| UX03-B | distinguish batch I/O, interactive terminal, graphics and diagnostic outcomes | channel/run/result contracts with candidate identity and authorized comparisons | real stdin delivery, visible graphics/input/resize, expected/actual diff and separate compiler/timeout failures | public APIs connected to declared terminal/editor/canvas controls | G-SURFACE + focused G-RUNTIME for CS-REF-10/11/13; mocks insufficient |
| UX03-C | define stop, context switch and bounded resource ownership | cancellation/disposal/stale-callback lifecycle and measurable budgets | one active run; old callbacks cannot overwrite; five-cycle cleanup/memory observations | lifecycle cleanup and stale-result presentation | G-SURFACE + G-RUNTIME for CS-REF-12 + G-REGRESSION; Auditor inspects measurements |

## Work

Before real execution in UX03-A, perform the asset preparation in
[G-PREFLIGHT](./QUALITY-GATES.md). Published 4.5.2 manifests were checked as
compatible; the local schema-1 copies still need refreshing. Preserve old
generated assets before replacement, synchronize the pinned published package
and check the actual local source and served manifests. No source edit,
upstream IDE publication or mandatory local compiler build is needed.
Follow `CS-ADR-024` if artifact availability or changed CI scope differs.

Entry audit: independently reproduce `CS-RUNTIME-NATIVE-ENTRY`,
`CS-RUNTIME-FUNCTION-ABI`, `CS-RUNTIME-GRAPHICS` and
`CS-RUNTIME-COMPILE-FAILURE` in
[`EMCEPTION-CAPABILITY-GAPS.md`](../EMCEPTION-CAPABILITY-GAPS.md).
These WB-B source findings block runtime capability promotion. Forwarded
configuration, a selected preset or a generated harness are not execution
proof. Resolve them through supported public composition; otherwise apply
EMCEPTION-GATE before changing upstream or reducing supported product scope.

The [deferred follow-ups](../IMPLEMENTATION-STATE.md#deferred-corrections-and-instructor-controls---2026-11-22)
record the real audit outcomes, correction order and pending entry/scaffolding
decision. They are intentionally not implemented yet. Their deferral does not
close this gate or turn an unsupported combination into an available one.

1. Implement a tested capability resolver from family requirements, environment
   support, assignment settings and authorized mode. Distinguish a hidden
   optional panel from an unavailable operation and a missing required capability.
   Do not infer the exercise family from a preset or extension.
2. Compose editor, file access, diagnostics, test results, console, interactive
   terminal and graphics as explicit regions. Use existing Monaco, xterm and
   resizable-panel integrations when appropriate; do not add libraries to
   reproduce already available mechanics. Keep long forms out of output regions.
3. Implement distinct operations for running a program, running selected/public
   tests and cancelling execution. The surface emits an official-submit intent
   through the host; no local operation creates academic state. Function
   examples use the supported function harness, not arbitrary main-file execution
   presented as a function verdict.
4. Treat stdout/stderr logs, fixed stdin supplied by a case and interactive
   terminal input as separate channels. Do not display captured ANSI sequences
   as a fake shell. Bound retained output and preserve useful error diagnostics.
5. Integrate graphics through `@gameguild/emception-browser`'s public canvas
   API (`build`, `start`, `buildAndStart`, `stop` as applicable). Verify the
   installed API and artifact, runtime target, source/asset mapping, resize and
   input handling. Leave `@game-guild/emception-ui` and `tools/emception/**`
   unchanged. Graphics is not blocked merely because the selected family is
   `function` or `standard-io`, but every offered combination needs proof.
6. Mount a real graphics target for environments that require one, even if its
   panel is temporarily minimized. Keep logs and diagnostics accessible when
   canvas has focus. Provide a non-graphical status/error explanation; absence
   of rendered pixels is not a successful run.
7. Bind each result to the executed code, candidate/revision, selected cases and
   environment identity. Mark stale results after edits. Show expected/actual
   comparisons only when authorized, and distinguish compilation/runtime errors,
   timeouts, cancellation and executor failure from incorrect answers.
8. Boot execution lazily. Keep at most one active run per surface, reuse runtime
   only inside the same safe context, and dispose workers, subscriptions,
   canvases and terminal sessions on cancellation/unmount/context switch. Late
   callbacks cannot overwrite a newer run. Clear private VFS material before
   switching from teacher validation to redacted learner preview.
9. Register genuine missing public capabilities rather than importing private
   modules or implementing another runner. Runtime graphics support and
   authoritative grading support remain separately reported. Do not claim a
   visual correctness oracle or silently assign zero to unsupported execution.

## Resource validation

Apply the budget recorded in `CODE-UX-01`:

- Editing a statement or case starts no compiler/worker.
- Run, cancel, change task and reopen repeatedly; check active sessions and
  memory plateau after warmup rather than counting only React unmount calls.
- Use one Playwright worker and one owned stack. Reuse a healthy local stack
  through repository-supported mechanisms; never stop an unrelated user stack.
- Do not combine a full web build, API build and browser runtime stress test.
  Limit build concurrency and leave at least two CPU cores available where
  hardware permits. Record heap/RSS limits before raising them.

## Evidence and gate

- `CS-REF-10` through `13` pass with a real supported artifact; mocks alone do
  not prove terminal input, graphics or cancellation.
- Inspect desktop/mobile screenshots and canvas pixels, including resize and
  an observable input/motion change. Verify nonblank content, correct framing
  and loaded assets, not merely the existence of a canvas element.
- Program execution and test execution are visibly distinct and have no
  academic effects. Synthetic error/timeout fixtures prove correct states.
- Supported environment combinations are reflected in the capability matrix;
  no enabled control leads to an unimplemented runtime path.
- Lifecycle/resource evidence meets the recorded budget. Failure stays open
  rather than being resolved by increasing resource usage without investigation.
- Public-import checks and preserved-package regressions show no upstream edit.

An artifact or upstream blocker keeps the affected gate open. Do not introduce
a new exercise family to disguise a missing capability. Next:
[3.6.4](./04-instructor-authoring-and-validation.md).
