# Prototype review contract

Frozen before UXP-06 implementation on 2026-11-20. This checklist covers design
fixtures, not production capability status. The matching required IDs in the
test selector must reject missing/duplicate cases as well as empty selections.
Record actual results in the shared implementation ledger.

| Slice  | Required browser oracles                                                                                                                                                      | Acceptance relationship                               |
| ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------- |
| UXP-01 | `UXP-01-entry-lifecycle`                                                                                                                                                      | incomplete draft, task accessibility, bounded harness |
| UXP-02 | `UXP-02-blank-function-typed-cases`, `UXP-02-io-empty-output-and-environment`                                                                                                 | CS-REF-01/02/06/08, UX-F01/F05                        |
| UXP-03 | `UXP-03-reference-wrong-and-stale`, `UXP-03-faithful-redacted-independent-preview`                                                                                            | CS-REF-05/09/19, UX-F03                               |
| UXP-04 | `UXP-04-diagnose-correct-submit-frozen`, `UXP-04-custom-compile-cancel-stale`                                                                                                 | CS-REF-03/04/13/14/15/16, UX-F02                      |
| UXP-05 | `UXP-05-save-conflict-recovery-and-lossless-cancel`, `UXP-05-host-layout-keyboard-and-mobile`                                                                                 | CS-REF-07/08/10/11/17, UX-F04/F06                     |
| UXP-06 | `UXP-06-scalar-types-and-capability-guards`, `UXP-06-modal-reset-and-save-in-flight`, `UXP-06-capability-specimens-and-captures`, `UXP-06-immutable-support-and-command-port` | adversarial cross-check of all six findings           |
| UXP-07 | `UXP-07-author-file-workbench`, `UXP-07-learner-buffers-and-readonly`, `UXP-07-editor-responsive-and-keyboard` | requested editor fidelity; CS-REF-07/08/12/14/17 |

UXP-07 was specified and added to the required selector before its behavior
on 2026-11-20. The earlier six-slice evidence remains historical, not a waiver
for the added editor/file workflows. Native editing, coloring, line numbers
and tabs represent the intended Monaco-backed composition without loading it.

Additional Node oracles must reject malformed/unscoped recovery, fixture/ID
injection and omissions from a selected slice. Unknown source/case combinations
produce an unrecorded state, never an invented result. Preview cannot submit;
read-only support files cannot be changed through a typed event.

Inspect captures at 1600x1000, 1280x720 (256/320 px host panels) and 390x844.
Check selected-case visibility, labels, keyboard focus, stable source edits,
command reachability, per-file positions/buffers, close-tab versus delete,
path collisions, explicit read-only support and distinct canvas/terminal/diagnostic panels. Canvas
pixels and the recorded echo demonstrate placement/interactions only.

Builder and same-agent Auditor record findings separately. Terminal success
does not approve usability, compound-value scope, real execution, actual
durability or grading. The human walkthrough in 01a remains required. No human
acceptance or production cutover may be inferred from this checklist.
