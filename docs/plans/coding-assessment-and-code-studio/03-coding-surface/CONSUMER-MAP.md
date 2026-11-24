# Coding surface consumer map

Status: consumidores de grading migrados e validados sobre o commit-base
`41932100b2031c0ad78d7c02545a73addc6364b3`.

Este mapa separa consumidores pertencentes ao fluxo de grading daqueles que
devem continuar em `@game-guild/emception-ui`. Um path encontrado por busca nao
e automaticamente autorizado a migrar. Cada linha precisa de ownership,
destino e teste de regressao confirmados.

Status permitidos: `investigar`, `preservar`, `pronto para migrar`, `migrando`,
`migrado` e `validado`.

## Consumidores de grading candidatos

| Fluxo | Path atual | Modo | Dependencia atual | Destino esperado | Estado | Regressao obrigatoria |
| --- | --- | --- | --- | --- | --- | --- |
| definicao de coding assessment | `apps/web/src/components/learning/console/courses/[course]/assessments/[assessmentId]/coding-definition/coding-definition-editor.tsx` | `author` | `@game-guild/coding-surface` e bridge do host | `coding-surface` | validado | roundtrip de draft, tipo explicito e expectativas privadas |
| preview de coding no authoring | `apps/web/src/components/learning/authoring/coding-assignment-preview.tsx` | `author-test` | `@game-guild/coding-surface` | `coding-surface` | validado | preview sem attempt, gradebook ou release |
| atividade learner | `apps/web/src/components/learning/coding-activity-client.tsx` | `learner` | `@game-guild/coding-surface` e delivery redigida | `coding-surface` | validado | public data, storage namespaced, submit oficial e refresh |
| assessment grader de code | `apps/web/src/components/learning/assessment-grading/assessment-grader.tsx` | `grader` | `@game-guild/coding-surface` e snapshot do host | `coding-surface` | validado | snapshot, evidencias e review autorizada |
| painel code do SpeedGrader | `apps/web/src/app/[locale]/(speedgrader)/speedgrader/assessments/[assessmentId]/code-grader-panel.tsx` | `grader` | reusa o assessment grader migrado | `coding-surface` por composicao migrada | validado | code e itens nao-code permanecem funcionais |

Os sete arquivos de teste focados desses fluxos e da bridge somam 77 testes
verdes. O browser cycle integral permanece no lane `coding`: sua fixture deve
emitir `ExerciseType` canonico e sua execucao exige o artifact Toolchain schema
2 exato.

## Refoundation inventory - 2026-11-19

Baseline: `723eeeb84589cfb23b0def630ee46ead02890acc`. The validation states
above are historical implementation evidence, not acceptance of the new UX.
None of these consumers was cut over to the refounded product in `CODE-UX-01`.

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
| `authoring-surface.tsx`, typed draft views/helpers and offline Monaco in coding-surface | UX04-A controlled Problem/Environment/Starter/Tests over the existing incomplete draft; no runtime or host import | new task views have local contract/unit/browser proof; reference/wrong validation is UX04-B and host persistence/form migration is UX04-C; do not claim the console consumer has switched yet |
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
