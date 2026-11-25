# Coding surface consumer map

Status: UX04-C/C2 current-contract Learning integration implemented on
2026-11-24; closing terminal audit passed. Both-family actual-host draft save/
reopen now passes. The Code publication correction is implemented and regression-
tested; the changed-source strict full-solution build now passes. Current API
architecture/security passes all 302 selected cases. All eight C3 functional
cases complete, including publication/reopen, but uncaught offline browser
errors keep the error-free gate nonpassing (CS-ADR-049). Human acceptance
remains pending. The user deferred the external/global offline investigation
to part 05 (CS-ADR-050), allowing independent core work with the exception
recorded, without declaring C3 green. The earlier part-3 baseline was
`41932100b2031c0ad78d7c02545a73addc6364b3`; it is not refoundation acceptance.

This map distinguishes grading-owned consumers from the preserved independent
Emception UI. Finding a path does not authorize migrating it. Confirm ownership,
destination and regression evidence for each consumer.

## Current grading consumers

| Flow | Current path | Mode | Dependency | Destination | Status | Required regression |
| --- | --- | --- | --- | --- | --- | --- |
| Instructor tasks | `apps/web/src/components/learning/authoring/lesson-authoring-workspace.tsx` | controlled author | public `CodingAuthoringSurface` and pure document/wire adapters | existing revisioned Learning writer | C2 implemented; all eight functional C3 cases complete; uncaught offline host errors keep acceptance open | incomplete buffers, exact acknowledgements, later edits, conflict/reload, publication, context replacement |
| Console/workspace definition links | `apps/web/src/components/learning/authoring/coding-authoring-route.ts` and the two route reexports | authorized redirect | existing assessment/content queries and locale-aware navigation | canonical workspace content editor | C2 implemented; actual browser redirect pending | inaccessible/missing/unlinked/non-Code records, query failure, encoding, locale, no old form |
| Learner preview | `packages/features/coding-surface/src/authoring-learner-preview.tsx` | isolated learner preview | public-only projection and shared solving surface | one owner in Validate exercise | UX04-B native gate passed; UX05-A2 shared problem/code/case/result composition and scoped closing gates pass; A3 host/human acceptance pending | no private data/storage/academic commands; current draft reflected; personal cases and native observations isolated |
| Learner activity | `apps/web/src/components/learning/coding-activity-client.tsx` | learner | public surface and redacted delivery | existing official submission ports | UX05-A2 shared views and scoped personal execution pass package/consumer and isolated Monaco/WASI proof; actual authenticated host/persona/refoundation acceptance pending | public data, context/run identity, explicit-oracle versus observed output, frozen submission and official acknowledgement |
| Assessment grader | `apps/web/src/components/learning/assessment-grading/assessment-grader.tsx` | grader | public surface and host snapshot | existing authorized review ports | adjacent component regressions pass; full refoundation acceptance pending | immutable submission, authorized evidence/review, non-Code regressions |
| SpeedGrader code panel | `apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/code-grader-panel.tsx` | grader | reuses the assessment grader | preserved host composition | unchanged | Code and non-Code flows remain functional |

The prior seven-file/77-test baseline is historical. Current exact selections,
counts and evidence are recorded in the implementation ledger. The full
official browser cycle is a separate gate requiring its current contract,
personas and exact Toolchain schema-2 artifact; no component pass substitutes.

## UX04-C consumer cutover and test retirement - 2026-11-24

`CS-ADR-048` replaces only owned Learning composition. Source/architecture tests
require both definition pages to reexport the authorized shared route and reject
remaining production imports of the retired forms. No Emception UI/runtime
source, persisted content, submission or historical migration is removed.

Retired components: `coding-definition-editor.tsx`, `standard-test-editor.tsx`,
`functional-test-editor.tsx` and `coding-assignment-preview.tsx`. The three
associated presentation test files contained **35 historical cases**. Their
removal is explicit, not a passing run of those cases and not evidence that all
previous presentation behavior remains a requirement.

| Historical concern | Current replacement oracle / intentional change |
| --- | --- |
| Direct definition upsert, C++ demo on mount and valid-only 30-second save | `lesson-authoring-workspace.test.tsx` and `learning-authoring-document.test.ts`: blank non-demo exercise, incomplete revisioned draft, no write on mount, exact acknowledgement, conflict lock and explicit publication; the old save protocol is intentionally retired |
| Source edits, file policy/rename, learner-created files and protected code | `authoring-surface.test.tsx`, `authoring-model.test.ts` and domain draft tests: offline controlled files, explicit policy, protected replacements bound by ID, lossless cancelled transformations; no IDE-controller polling or sync requirement |
| Standard I/O expectations and function parameter/result fields | `authoring-surface.test.tsx`, domain draft/compiler tests, `coding-publication.test.ts`: family-specific typed values, complete groups, missing versus empty output, explicit weights and private buckets; no historical coercions or mixed-family fallback |
| Negative weights, malformed identifiers, empty cases and definition size | domain draft/schema/compiler tests plus publication negatives: incomplete input remains editable/saveable, executable publication fails with field errors; aggregate UTF-8 and file-count budgets remain tested |
| Concurrent save/API failure | host tests and pure exact-ack tests: single writer, wrong-body/scope/revision rejection, failed reload remains locked, latest local backup, later edits preserved, no write racing publication |
| Missing content link and Back routing | `coding-authoring-route.test.ts`: fail closed before canonical redirect; existing Learning curriculum navigation supplies the return path |
| Full-definition author-test preview | `authoring-validation.test.tsx` and UX04-B native evidence: protected exercise checks are separate from a redacted learner preview; no duplicate full/private editor mounted by the host |
| Allegro preset seeds, graphics execution and binary asset editing | not promoted by this cutover. Current capabilities remain explicitly bounded; binary content is represented by the domain, but these retired presentation tests do not prove a binary authoring workflow or graphics runtime acceptance. Existing post-04 findings and separate runtime gates remain authoritative |

At C2, the historical `coding-definition-authoring-browser-e2e.mjs` still
described an older dashboard route/direct-PUT/demo UI and supplied no current
acceptance. C3 now replaces that flow with typed local-host authoring checks,
using the canonical editor, existing revisioned authoring API and a non-admin
course owner. Actual outcomes belong in the ledger: `host-authoring-S5Rmh9`
passes both draft/reopen cases and fails publication because draft base 3 does
not match published content version 2. The subsequent owned Code participant
fix removes the intermediate save; its final 194 cases pass, plus 541 selected
neighbor/security cases. API build attempts were resource-stopped, so that old
failure is not rewritten as success. A subsequent user-authorized OS-managed
full-solution build passes with zero warnings/errors; current host/test DLL
copies match and all 302 selected architecture/security tests pass. Report
`host-authoring-AvKD2P` proves actual publication/reopen for both families,
conflict/retry/context and inspected layout captures, but correctly exits 1
for uncaught Server Action fetch failures during offline testing. Those errors
must be diagnosed within the approved owner boundary, not filtered away.
Investigation is explicitly deferred to part 05 by CS-ADR-050; independent
core work may continue with that exception recorded. Current C3 and human
acceptance remain open. Do not resurrect the old UI,
weaken an oracle, reuse old binaries or replace instructor behavior with
SystemAdmin.

## Refoundation inventory - 2026-11-19

Baseline: `723eeeb84589cfb23b0def630ee46ead02890acc`. The validation states
above are historical implementation evidence, not acceptance of the new UX.
None of these consumers was cut over to the refounded product in `CODE-UX-01`.
The table is a historical inventory of that date; the current C2 dispositions
above supersede its old host-form entries, not its preserved upstream boundaries.

| Consumer / shared boundary | Observed current behavior | Refoundation disposition |
| --- | --- | --- |
| `lesson-authoring-workspace.tsx` | already offers a wide Code container; owns title, outer draft and publication | preserve host ownership and available space; supply the new authoring draft ports |
| `coding-definition-editor.tsx` | valid-only direct upsert, 30-second autosave; language/files/tests inserted into workbench extension slots | replace owned composition with the five teacher tasks; converge on existing Learning draft/concurrency services |
| `standard-test-editor.tsx`, `functional-test-editor.tsx`, visibility controls | concrete validation/field behavior in the host | retain useful rules; move family-specific authoring to the package without retaining console forms |
| `coding-assignment-preview.tsx` | `author-test` receives a full definition | distinguish private validation from genuinely redacted learner preview |
| `coding-activity-client.tsx` | obtains frozen runtime delivery; maintains host submission keys and local namespace | keep official commands and contextual identity; replace solving composition and freeze retry payload |
| `assessment-grader.tsx` and SpeedGrader | server snapshot/submitted files and authorized review commands | retain frozen-source ownership; integrate read-only review presentation |
| `coding-surface-bridge.ts` | converts PascalCase content and rejects a missing explicit type | evolve with owned contracts; incomplete draft is a separate model |
| `authoring-persistence.ts` in coding-surface | new tested session/port; no runtime, storage or host imports | bind only to authenticated authoring; exact save ack, conflict lock, scoped memory recovery and disposal |
| `runtime-session.ts` / `runtime-types.ts` in coding-surface | lazy public runtime acquisition, exact metadata preflight and owned worker/canvas disposal | CS-ADR-042 wires textual execution through the owned workbench coordinator; editor-only file access and submission capture remain offline; no academic authority |
| `runtime-capabilities.ts` / `runtime-capability-types.ts` in coding-surface | explicit family/environment/frozen identity, authorized mode, read-only/file policy and canvas target | CS-ADR-042 wires supported text actions; prior verified scalar native mapper enables WASI function tests; graphics/RAM/unverified bindings remain unavailable in this UI |
| `workbench-session.ts`, `preview-runner.ts`, `editor-files.ts` and `program-output.tsx` | one local operation, immutable snapshots, cancellation/drain, stale results, exact public mapper, editor-only controller and separate terminal channels | package UI integration, not UX04 task views or official grading; learner deltas alone may use scoped local storage; four host compositions add context identity/invalidation; no SDK-shaped API |
| `graphics-session.ts` / `graphics-session-types.ts` and lazy runtime whenDisposed in coding-surface | UX03-B local diagnostic canvas session, immutable public source/candidate, lazy boot, bounded channels/time, explicit stop/disposal completion | not wired into production; five real SDL cycles prove pixels/input/framing, but EMC-GAP-003 keeps cleanup acceptance open; the paired browser harness is test orchestration, not another runtime or human fixture CLI |
| `coding/src/native-function-support.ts`, surface `execution-plan.ts` and configured official grading wrapper | shared pure native scalar call/startup/ABI composition; C++ consumes the unchanged public harness, C emits its existing report grammar | CS-ADR-036; generated files outside submitted workspace, public/private scopes and cleanup tested; production real scalar/receipt parity remains required; no core, IDE or retained UI edit |
| `learning-draft-persistence.ts` in the web host | new tested bridge through existing Learning actions; not installed in the production editor | lesson coordinator supplies revision-bound metadata and owns the only save loop; no direct published upsert |
| `learning-authoring-document.ts` in the web host | C1 tested read-only current-draft preparation, complete-candidate wire compilation and exact lesson/revision/body acknowledgement checks | C2 retains the single lesson writer; user explicitly rejected legacy adoption, old-editor fallback and migration UI for disposable implementation test data |
| `authoring-surface.tsx`, typed draft views/helpers and offline Monaco in coding-surface | UX04-A controlled Problem/Environment/Starter/Tests over the existing incomplete draft; no runtime or host import | new task views have local contract/unit/browser proof; UX04-B native proof passes; C2 installs Learning composition and consolidates the definition routes; the C3 backend fix passes units but changed-host publication remains unproven |
| `authoring-validation.tsx`, `exercise-preparation.ts` and `exercise-validation.ts` | UX04-B protected reference/incorrect source copies, candidate-bound checks and strict learner-only preview through the shared solving component | exact evidence/status in the current ledger; local checks use no academic commands or author storage; host persistence/form migration remains UX04-C; upstream runtime/IDE/UI unchanged |
| Coding JSON DTOs, `coding-surface-bridge.ts` and both backend learner projections | WB-B adds explicit frozen environment/limits/entry/toolchain and inverse draft publication; legacy absence remains absent; references excluded from learner/official student execution | consume these tested ports in the later production cutover; current client/backend closing evidence is recorded separately in the ledger |
| `tools/emception/grading/browser.ts` | consumes explicit time/output/native configuration and now rejects missing/invalid public phase; real current-source worker controls 12/12 pass | CS-ADR-028/034 limit worker/public metadata changes; compiler/harness/IDE/UI preserved; scoped stdio proof is not function/graphics conformance; configurable RAM unavailable |
| public ToolResult, browser facade and existing emception/testing engine | authorized optional terminal phase identifies compile/link/run; old results remain assignable; reported build failure cannot satisfy program assertions | no new executor or copied comparator; full SDK/worker suites and real controls recorded in the ledger; unchanged upstream lint debt and normal exact-source CI remain required |
| `coding-cycle-browser-e2e.mjs` | historical mixed stdio/function diagnostic cycle | canonical fixture imported with explicit `ExerciseType`; compatible local assets now pass preflight; full cycle execution remains pending; no mandatory runtime build (`CS-ADR-024`) |

Detailed reuse/replace/remove reasons and the ownership trace are in
[`PRODUCT-MODEL.md`](./06-refoundation/PRODUCT-MODEL.md) and `CS-ADR-014`.
On 2026-11-21, an authorized read-only local inventory counted two coding
definitions, neither with Problem, and no coding drafts, revisions or
submissions. No IDs/content were exported, and no database write, bulk rewrite
or reset occurred. Remote usage remains unknown. The single ledger records
the inspected volume, count scope and preservation decision before the
additive metadata change.

## Consumidores preservados

| Fluxo | Path atual | Motivo para preservar | Teste de regressao | Estado |
| --- | --- | --- | --- | --- |
| package preservado e seus testes | `packages/infrastructure/ui-emception/**` | produto Emception existente, fora do ownership de grading | `pnpm --filter @game-guild/emception-ui test` | preservar |
| aliases e suporte do ciclo antigo | `apps/web/next.config.ts` e scripts `coding-cycle-*` | ainda protegem o package preservado e nao pertencem ao novo contrato | `pnpm --filter @game-guild/web test:browser:coding-cycle` | preservar |
| restore de seed legado | `apps/web/src/components/learning/resolve-seed.ts` | helper do host ainda converte payloads existentes; nao pertence ao package novo | testes de coding activity | preservar |

## Configuracao compartilhada a preservar

| Item | Path | Regra |
| --- | --- | --- |
| dependencia workspace | `apps/web/package.json` | nao remover enquanto houver consumidor preservado |
| aliases e transpilation | `apps/web/next.config.ts` | nao remover ou remodelar durante a migracao sem prova de ownership exclusivo |
| package existente | `packages/infrastructure/ui-emception/**` | nao editar, mover, deprecar ou remover nesta parte |

## Checklist por consumidor

Antes de marcar uma linha como `migrado`:

- [x] ownership de grading confirmado;
- [x] props e callbacks atuais registrados;
- [x] dados publicos e privados classificados;
- [x] source of truth e reidratacao identificadas;
- [x] destino usa apenas API publica de `coding-surface`;
- [x] host continua possuindo auth, tenant, fetch e mutations;
- [x] teste unitario ou de componente atualizado;
- [x] teste browser ou integracao identificado;
- [x] imports antigos removidos somente deste consumidor;
- [x] consumidores preservados e aliases nao foram afetados.
