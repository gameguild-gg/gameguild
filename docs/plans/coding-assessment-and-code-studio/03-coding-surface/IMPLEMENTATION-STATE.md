# Coding surface implementation state

Ultima atualizacao: 2026-11-21.

## Execution record: UX02-B persistence preparation - 2026-11-21

- Entry: committed UX02-A at `bb9cb883c7db6065a812449d5ed7bfd126245c38`,
  tree `01ab03dd3b781e8774da66a355e4c4ba2c0369df`, clean `feature/coding-grading`;
  user requested continuation. No merge or automatic commit.
- The broader UX02-A G-API failure/resource stop remains open. Governance
  permits unaffected checks within their own boundary; this continuation
  prepares and tests host persistence/recovery without closing UX02-A,
  entering UX02-C or cutting over the production editor.
- Specification: incomplete coding documents use Learning's existing
  revisioned authoring draft. Server Saved requires the exact write ack;
  edits made during that write remain unsaved. Failures/conflicts retain
  buffers and cannot silently overwrite another author. Reload/discard is
  explicit. Recovery checkpoints are cloned, author-only in-memory values,
  scoped to tenant/actor/course/content and base draft/publication revisions.
  No private solutions/cases are written to browser storage. A refresh or
  another device can recover acknowledged server data, not unsynced memory.
- Allowed edits: new persistence/session contracts and focused tests in
  `packages/features/coding-surface/src/`; its exports/architecture checks;
  a host bridge and focused tests in `apps/web/src/lib/coding-assignment/`;
  shared plan/state/ADRs. Existing Learning actions and authorization remain
  the server boundary; no second endpoint/store or active writer is added.
- Implemented: surface persistence/session ports and host bridge through the
  existing authorized Learning actions. A single in-flight save freezes its
  payload; only an exact body/revision acknowledgement advances Saved. A
  conflict stays locked even if reload fails. Scope changes/disposal prevent
  late responses from reviving a previous actor's state. Host metadata is read
  atomically with its revision; reload cannot enable a write using old metadata.
- Contract stage passed before behavior: strict surface types, two initial
  schema/port fixtures and lint; full web TypeScript validated the host port
  and fixtures. Missing public behavior then failed 22 session / 16 host
  oracles before implementation. Callback tests now import their pure module;
  public export/import boundaries have separate architecture/type assertions.
  Initial ES library/readonly fixture errors were corrected in tests without
  weakening compiler settings; they are not behavioral red evidence.
- Same-agent Auditor reproduced and fixed: lost conflict lock after failed
  reload; receiver-dependent recovery callback; changed body at the same
  revision; incomplete scope accepted by the host; cyclic metadata classified
  as a network failure; stale metadata overwrite after a newer coding reload.
  Malformed acknowledgements are invalid data, never Saved. Required oracles
  were retained. Both families use the same JSON fixtures as backend tests.
- Gate: G-CONTRACT/G-SURFACE; focused host bridge and existing Learning
  authoring/workspace tests, strict changed-file lint, G-WEB typecheck when
  safely budgeted. Existing UX02-A EF persistence/rejection evidence remains
  scoped as recorded below; no new backend change is planned. Full G-API and
  production browser/human acceptance remain open, not implicitly waived.
- Resources: entry MemAvailable 13,937 MiB; sequential checks, one JS worker,
  <=4096 MiB JS heap and CPU affinity 0-9 (two of twelve cores reserved).
  Do not repeat the twice-stopped API-wide compilation in this slice.
- No active editor/writer, generated client, API production source, table,
  migration, platform, Emception/UI, dev command or dependency was changed.
  This is tested integration preparation, not production UI cutover or human
  acceptance. The lesson coordinator must own the single save loop and adopt
  the whole acknowledged/reloaded lesson revision before issuing another write.
- Next: preserve the open broader gate; prepare the UX02-C published-data
  inventory and immutable publication/projection contract. Do not claim that
  an authoring draft is already publishable, or enter CODE-UX-03 runtime work.

### Builder / Auditor results for UX02-B preparation

Both roles were performed by the same agent. All selected final cases below
passed without skips; external/open gates were not rerun or relabeled green.

| Command / check | Result | Scope / limitation |
| --- | --- | --- |
| G-CONTRACT, coding and adapter typechecks/tests and strict package lint | exit 0; 49 + 15 tests | canonical executable contracts unchanged |
| G-SURFACE, typecheck, tests and strict lint | exit 0; 46 tests, 32 more than the committed baseline | memory-only lifecycle, ack/conflict/scope protections; no runtime/compiler |
| G-WEB focused selection: new `learning-draft-persistence.test.ts`, existing bridge, authoring actions and authoring workspace | exit 0; 106 tests, including 22 new host cases | real Server Actions with mocked HTTP; no API server or browser claim |
| Full web `tsc --noEmit --pretty false` and strict changed-file ESLint | exit 0 | 4096 MiB heap ceiling; no ignored type/lint diagnostics |
| Courses `--no-build --no-restore --filter FullyQualifiedName~CodingDraft` | exit 0; 4/4 | existing unchanged compiled service tests; EF InMemory roundtrip/conflict, not PostgreSQL HTTP |
| CodeAdapter `--no-build --no-restore --filter FullyQualifiedName~AuthorOnlyDraft` | exit 0; 2/2 | existing unchanged compiled draft-discriminator rejection cases |
| Preserved `@game-guild/emception-ui test`; release-classifier/required-gate Node suites | exit 0; 27 + 54 tests | no upstream/UI/CI source changes or publication |
| G-DOC, task-delta/negative-constraint review and official `needs_source_release()` on all 14 changed/new paths | 47 documents, 165 valid local links, 25 atomic rows; classifier false; whitespace clean | no runtime/IDE producer required; no production/human acceptance inferred |
| Broader G-API / full solution build | still open; not retried | previously recorded Authentication failure and twice-stopped API compilation remain; no new backend source change |

New implementation paths: surface `authoring-persistence.ts`,
`authoring-persistence-types.ts`, their contract/behavior tests and public
exports/architecture tests; host `learning-draft-persistence.ts` and its test.
CS-ADR-026 and the consumer map describe the required coordinator integration.

Commands use installed Node/pnpm, CPU affinity `0-9`, one Vitest worker and
sequential groups. The web lint runs from `apps/web` against exactly its two
new files with `--no-inline-config --max-warnings=0`; package lint covers all
three owned source directories. The G-WEB selection adds the new host test to
the three existing gate files. No dependency/tool was installed. An ephemeral
`/tmp/coding-ux02b-resource-guard.mjs` guards only owned processes, reserving
4 GiB system MemAvailable and stopping above 6 GiB owned RSS; this is not a
committed CLI or developer-command change. No API compile, Next build, runtime,
Docker, migration, data reset or background service was started.

Final resource observations: combined sampled owned-process RSS peaked at
826 MiB for G-WEB tests, 1404 MiB for full web types, 518 MiB for preserved
Emception UI tests and 326/311 MiB for the focused compiled backend cases.
The monitor includes descendants with separate process groups. Earlier
contract-stage full web types sampled 1551 MiB; all monitored commands stayed
well below the existing guard. RSS sums can double-count shared pages; these
are observations, not a physical-memory/runtime performance benchmark.
Two CPU cores remained reserved, and no resource stop was needed in this slice.

Bootstrap corrections are not product failures/passes: Jest rejected the old
G-REGRESSION invocation combining the package's existing `--runInBand` with
`--maxWorkers=1`. The plan command now uses the declared sequential test script;
all 27 cases ran. No protected package/config was edited. The Markdown audit
resolved unified/remark-parse from the declared react-markdown dependency
context after a direct web lookup failed; no library was installed.

## Execution record: UX02-A - 2026-11-21

- Entry: `f5436065e9f67e29967e77bbc22a8e9185b9e6fe`, tree
  `916eced726bb8da768be2229f80308df4ad6ca9f`, clean
  `feature/coding-grading`; no merge in progress.
- `CODE-UX-01` closure audit: strict prototype typecheck/lint passed;
  30 prototype and 26 preflight tests passed without skips; coding 15,
  adapter 15, bridge 5 and acquisition-policy 36 tests passed; package
  typechecks and strict contract lint passed. Documentation audit resolved
  168 local links in 47 files and all 25 atomic rows in seven tables.
  User design/scalar acceptance from 2026-11-20 remains authoritative.
  Published asset refresh and real execution remain a UX03 obligation.
- Active slice: `UX02-A`, implementation and in-scope audit complete;
  formal broader G-API gate remains open.
  Allowed edits: coding draft
  contracts/schemas, pure transformations, synthetic fixtures and tests;
  existing Courses authoring persistence tests and CodeAdapter rejection
  tests; shared plan/state/ADRs and the part 4 dependency-status mirror.
  No production host, canonical definition,
  generated client, table, migration, platform or Emception changes in this slice.
- Contract: a distinct `coding-authoring-draft` JSON document may contain
  incomplete problem, signature, scalar input and expectation fields. It is
  never asserted to be a publishable definition or delivered to a learner.
  One signature belongs to the function draft; stdio expectations distinguish
  absent (`null`) from intentionally empty (`""`). IDs survive edits and
  explicit duplication. Family/signature/environment/toolchain transformations carry an
  impact report; stale confirmation fails and cancellation preserves current data.
- Data preservation: existing canonical/wire interpretation and active writer
  remain unchanged. There is no automatic conversion or migration of persisted
  definitions. Import of an incompatible mixed/signature definition must fail
  explicitly rather than reinterpret it. Real persisted inventory is still
  unknown and remains required before a future breaking interpretation/cutover.
- Gate: strict coding/adapter types, focused draft schema/operation tests,
  G-CONTRACT, existing bridge regression, actual EF-backed shared-draft
  roundtrip/conflict tests and CodeAdapter draft-rejection tests. Any external
  backend failure remains visible; no relaxation of compiler, linter or security.
- Contract stage passed before transformation implementation: strict coding
  typecheck, 17 schema tests and lint. Initial operation-port test was red
  before implementation. Auditor then added two behavioral regressions that
  failed for the intended reasons: duplicate parameter names with different
  types incorrectly retained a value, and object property order produced a
  false change impact. Both causes were corrected without changing the oracles.
- In-scope backend proof: four shared-fixture save/reopen/conflict tests and
  the full Courses suite (902), CodeAdapter (74), Assessments (453) and
  Authorization (2815) suites passed without skips. Authentication reproduced
  its previously recorded email-resend throttle failure (4572/4573 passed).
  No Authentication source or test was modified. API.UnitTests compilation
  was stopped by the resource guard before a test result; the full solution
  build was not started. Details follow; neither is reported as passing.
- Next: resolve the broader gate failures with their owners/resource-budget
  disposition before declaring UX02-A green. UX02-B/C and all production,
  runtime and human acceptance remain open.

### Builder / Auditor results for UX02-A

Both passes were performed sequentially by the same agent, not an independent
reviewer. No human or production acceptance is inferred.

| Stage / command | Observed outcome | Scope / limitation |
| --- | --- | --- |
| Contract before behavior: `pnpm --filter @game-guild/coding typecheck`; focused `authoring-draft-contracts.test.ts`; strict ESLint | exit 0; 17 schema cases | incomplete shape, IDs/budgets, explicit missing/empty values and author-only discriminator |
| Transformation test-first and adversarial audit | absent operations failed first; two added behavioral regressions failed with concrete wrong values before their corrections | no assertion relaxation, suppressions or test skips |
| Final G-CONTRACT: coding/adapter typechecks and tests; strict ESLint for both packages | exits 0; coding 49, adapter 15 tests | 34 new draft contract/transformation cases; canonical definition and adapter behavior unchanged |
| Downstream regression: coding-surface typecheck/tests; web canonical bridge Vitest | exits 0; surface 14, bridge 5 tests | current consumers preserved; no production UI replacement or real runtime |
| Shared fixture through `ProgramContentAuthoringServiceTests.CodingDraft_*` | four focused cases passed, then full Courses 902/902 | actual service/EF InMemory write/read and expected-revision conflict; not PostgreSQL HTTP/browser proof |
| `GameGuild.Learning.Assessments.CodeAdapter.UnitTests` | 74/74 passed | includes both author-only-discriminator rejection cases with an otherwise valid definition |
| `GameGuild.Learning.Assessments.UnitTests` | 453/453 passed | existing academic contracts/regressions |
| `GameGuild.Identity.Authorization.UnitTests` | 2815/2815 passed | no platform changes |
| `GameGuild.Identity.Authentication.UnitTests` | exit 1; 4572 passed, one failed, zero skips | known `EmailCodeServiceTests.GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`, also recorded before this task |
| `GameGuild.API.UnitTests` compilation, first attempt and reduced-concurrency attempt | both resource-stopped; no test result and no pass | peak sampled owned RSS 6457 / 6230 MiB exceeded the 6144 MiB guard; the CLI exited 0 on SIGTERM but the supervisor reported failure |
| `dotnet build apps/api/GameGuild.sln` | not run | stopped after the API compilation exceeded the same budget twice; no analyzer/typing relaxation or larger heap |
| Documentation/import/path audit | 47 Markdown files, 165 valid local links, 25 atomic rows; whitespace clean; official classifier says `source_release_required: false` for all 17 changed/new paths | no Emception/IDE/CI, dependency manifest, API production source, host writer, database or migration change |

Executed backend commands used `--no-restore -m:1 -nr:false
-p:UseSharedCompilation=false --nologo --verbosity quiet` with explicit project
paths from G-API. The focused persistence run used `--filter
FullyQualifiedName~CodingDraft`; the full suites did not filter/skip the known
Authentication failure. VSTest needed loopback sockets outside the sandbox;
the initial sandbox denial is bootstrap evidence, not a failed product test.
The Node shell used installed Node 24.16.0. pnpm needed access to its existing
cache outside the sandbox; no tool/dependency/version was installed or changed.

Resources: initial MemAvailable 15,209 MiB on 31,806 MiB RAM, with 12 CPUs.
All checks were sequential. JS used one worker and a 4096 MiB heap ceiling.
Backend MSBuild used one worker and at most ten CPUs, reserving two; the
bounded API retry used only two CPUs. The non-API backend suites peaked at
1327 MiB sampled owned RSS. The two API compilation attempts exceeded the
6 GiB owned-process target, so the monitor stopped their process groups,
preserving at least 8274 / 8011 MiB sampled MemAvailable. No kernel OOM or
successful API test run is inferred from those stops. The full build was not
attempted after the second stop. A process check confirmed no dotnet,
VBCSCompiler or testhost was left running by these checks. Existing developer
services, Docker, data and ignored Emception assets were not reset/replaced.

Outstanding ownership: the Authentication maintainer owns the existing resend
test failure. API-wide compilation needs a separately justified resource-budget
disposition or a less memory-intensive build path, not a disguised success.
Neither failure was fixed by changing other developers' source or the gate.

Current status: functional baseline and canonical consolidation 3.5 completed
locally. `CODE-UX-01` closed on 2026-11-21 after its corrected audit.
`CODE-UX-02` is in progress: UX02-A draft contracts and pure transformations
are implemented, but its broader security gate has an external failure.
UX02-B persistence/session ports and host-action integration are implemented
and tested in scope; formal closure and the production cutover remain open.
UX02-C has not started. The production UI has not been cut over to the
refounded model. Compatible published assets still require local refresh and
real execution checks in CODE-UX-03, not an upstream IDE build/publication.

Este e o handoff mutavel da parte 3. Atualize-o com fatos observados. Nao marque
um gate como concluido sem registrar a evidencia executada.

## Baseline de execucao

- Branch: `feature/coding-grading`.
- Commit-base da consolidacao: `810c2a0764`.
- Baseline reavaliada em `43144e23ec`: branch alinhada com
  `origin/feature/coding-grading`, 188 commits atras e 7 a frente de
  `origin/develop` antes da atualizacao documental da parte 4.
- HEAD at refoundation entry: `723eeeb84589cfb23b0def630ee46ead02890acc`.
- Root tree: `c1236b3e35c0da3eed4e79135eb6b7090b91863f`.
- Entry worktree preserved: Learning workspace and content-tree source/tests,
  coding-surface source/tests/package, untracked `coding-workbench.tsx`, and
  `pnpm-lock.yaml`. No pre-existing changes were reverted. No merge/commit,
  fetch or database reset was performed. Historical develop counts above
  are not current remote-state evidence.
- Ultimo merge relevante: nenhum merge em andamento.
- Observacao: a criacao destes planos nao caracteriza a implementacao.

## Pre-requisitos

| Entrega | Estado | Evidencia |
| --- | --- | --- |
| Parte 01, dominio e adapter TypeScript | concluida | plano 01 e suites registradas nele |
| Parte 02, boundary backend de code | concluida | plano 02, build integral e suites registradas nele |
| Parte 3.1 | concluida | buscas de consumidores, contratos e exports publicos registradas em 2026-11-18 |
| Parte 3.2 | concluida | package isolado, 14 testes e typecheck verdes |
| Parte 3.3 | concluida | dois perfis, quatro modos, privacy e contrato canonico cobertos |
| Parte 3.4 | em validacao final | consumidores e CI migrados; runner fixture now emits canonical `ExerciseType`; local asset refresh and real browser E2E remain pending |
| Parte 3.5 | concluida localmente | `CANON-01` a `CANON-06` concluidos; validacao local verde, com fixture do runner e browser CI ainda pendentes no fechamento de `CANON-07` |
| Parte 3.6 | in progress | `CODE-UX-01` closed; UX02-A and UX02-B preparation implemented/tested in scope, formal G-API gate open; UX02-C and production cutover not complete |

## Etapa ativa

- Subplan: [3.6.2 domain and drafts](./06-refoundation/02-domain-drafts-and-data-boundaries.md).
- Current gate: `CODE-UX-02`, UX02-B in-scope audit recorded; formal broader
  UX02-A/B gate remains open. Authentication has its known throttle failure
  and API-wide compilation exceeded its resource guard. UX02-C/runtime/production
  acceptance is not marked complete.
- Design and initial scalar-only scope remain approved. ADRs 014-024 retain
  ownership/governance/artifact decisions; ADR025 records the new author-only
  draft; ADR026 records revision-bound host persistence and memory-only recovery.
  Actual local artifact preflight remains a CODE-UX-03 obligation.
- Responsavel: Codex.

### Current compact checkpoint

- Baseline: `bb9cb883c7db6065a812449d5ed7bfd126245c38`, tree
  `01ab03dd3b781e8774da66a355e4c4ba2c0369df`, clean entry on
  `feature/coding-grading`. No fetch, merge, commit or stage.
- Active row/stage: UX02-B preparation / in-scope audit recorded, broader
  G-API unresolved. New session/host ports are tested but no active host writer
  or UI was replaced. Allowed files/red-green evidence are recorded above.
  No schema/platform/Emception, canonical definition or generated-client edit.
- Contract: distinct author-only draft, stable identities, raw incomplete
  buffers, one signature, separate language/runtime/toolchain and protected
  validation solutions. It cannot enter definition/delivery/official grading
  paths. Existing drafts/revisions/data are not automatically rewritten.
- Acceptance: CS-REF-06/07/09/22 contract preparation only; their production
  persistence/recovery/projection acceptance stays open in UX02-B/C and later
  browser/human gates. CS-REF-18 retains its accepted initial-scope limitation.
- Terminal: current coding/adapter/surface typechecks/lint pass; package tests
  pass 49/15/46. G-WEB selected tests pass 106, full web TypeScript and lint pass.
  Unchanged compiled backend persistence/rejection cases pass 4+2, without
  rebuild. Full backend-suite results below are the prior UX02-A evidence;
  Authentication still has the recorded email-resend failure and API compilation
  was stopped twice. The full build was not run. No false green or required skip.
- Runtime: no asset download/refresh, compiler/IDE build, runtime boot or dev
  stack. Local schema-1 artifacts remain a UX03 preparation obligation.
- Human: prototype and initial scalar scope approved on 2026-11-20, not
  production usability or runtime approval. No new acceptance is inferred.
- Next exact action: UX02-C specification/read-only published-use inventory;
  identify the official statement/revision and wire/projection boundaries
  before changing them. Preserve the open broader G-API owner/resource
  disposition; do not close CODE-UX-02 or start CODE-UX-03 with missing evidence.
  UX02-B ports must be integrated by the sole lesson save coordinator at the
  planned UI cutover, not installed as a second autosave loop. No IDE release.

| Gate da refundacao | Estado | Evidencia necessaria |
| --- | --- | --- |
| `CODE-UX-01` | complete | corrected contract/document/design/preparation audit passed on 2026-11-21; no runtime/build claim |
| `CODE-UX-02` | in progress / gate open | UX02-A and UX02-B preparation implemented/tested in scope; broader Authentication/API resource gate remains open; UX02-C and production integration pending |
| `CODE-UX-03` | nao iniciado | refresh compatible published assets and pass local preflight before real runtime, canvas/terminal, cancellation and resource checks |
| `CODE-UX-04` | nao iniciado | autoria completa dos dois tipos e validacao pelo professor |
| `CODE-UX-05` | nao iniciado | resolucao, envio, recuperacao e revisao por persona |
| `CODE-UX-06` | nao iniciado | integracao, matriz de aceite e revisao humana |

## Fatos confirmados

- `@game-guild/emception-ui` deve permanecer preservado.
- `coding-surface` sera um consumidor paralelo dos exports publicos do
  Emception.
- a primeira composicao visual com o componente monolitico `Ide` foi corrigida:
  o package agora possui o workbench e consome `createEmception()` headless;
- somente fluxos de grading explicitamente selecionados podem migrar.
- `function` e `standard-io` formam o corte inicial de tipos.
- nenhuma mudanca de tabela foi decidida para a 3.6; necessidade demonstrada
  exige `SCHEMA-GATE`, migration e validacao, sem apagar historico.
- `CodingAssignmentContent.ExerciseType` permanece no JSON ja persistido; nao
  cria coluna, tabela ou migration.
- o diff atual de `@game-guild/emception-ui` e `tools/emception` permanece vazio;
- `@game-guild/coding` possui um unico contrato, parser por payload e fixture
  canonica, com `exerciseType` obrigatorio;
- backend e TypeScript registram apenas `code-assessment-type` e
  `code-wasm-review` na versao `1`, com payload schema `code-files`;
- qualquer adapter, handler, payload schema ou toolchain diferente da identidade
  corrente falha fechado;
- nomes `V1` importados de `@game-guild/grading` pertencem ao protocolo
  generico e permanecem fora da consolidacao de code.
- a superficie de autoria precisa ser distinta da superficie de execucao;
- configuracao passa a views por tarefa, com largura integral ou composicao
  lateral quando util; output e console nao recebem formularios;
- o contrato canonico completo nao deve ser usado como estado intermediario de
  formulario; recuperacao de draft e validacao para publicacao sao operacoes
  distintas no modelo alvo;
- canvas e terminal sao capabilities e nao exigem automaticamente outro tipo;
- contratos e componentes nossos podem evoluir para o modelo aceito, em vez de
  preservar a estrutura inicial apenas porque ja foi implementada.

## Decisoes abertas

`CS-ADR-017` adopts the Meta-Harness protocol for all of 3.6; `CS-ADR-018`
records the additional design iteration. The user subsequently explicitly
approved the prototype design and initial scalar-only function scope on
2026-11-20. This does not approve a production cutover or CI deployment.

`CS-ADR-014` records the actual field ownership/persistence trace and the
single Learning draft/publication model. `PRODUCT-MODEL.md` contains the
teacher/learner journeys, component dispositions, runtime matrix and budgets.
Implementation of these domain choices belongs to `CODE-UX-02`; no database
schema or platform module was changed.

`EMC-GAP-001` is confirmed through actual installed public API calls:
scalar harness generation succeeds; Array inputs, returns and nested values
are rejected. The official validator is also scalar-only. `CS-ADR-015`
now records the explicit user acceptance of initial scalar scope with an
upstream follow-up. Compound support remains unimplemented, not implicitly
supported or permanently excluded.
The installed ESM public export was re-probed at HEAD `40a8470b46` with the
same boundary result. CS-ADR-023 retains the discovered manual producer
deployment side effect; CS-ADR-024 withdraws the unnecessary build-only CI
proposal and records the compatible published consumer path.

## Bloqueios

Resolved fixture defect: the actual cycle imports `buildCodingContent()` from
`apps/web/scripts/coding-cycle-fixture.mjs`, which emits `ExerciseType:
"function"`. Its focused canonical bridge test passes, including rejection
of a missing type and private-test redaction. Historical mixed execution
coverage is retained; this does not claim the new family-specific browser
journeys are implemented.

A entrada da parte 4 continua bloqueada por `CODE-UX-01` a `CODE-UX-06`.
O browser E2E deve provar o modelo refundado, e nao congelar como aceita a
composicao antiga. Preflight verde nao substitui aceite de produto ou CI.

The real local preflight still rejects the schema-1 assets. Compatible 4.5.2
manifests are available from published packages; the earlier zero-result
current-HEAD GitHub queries do not establish a consumer artifact shortage.
Actual synchronization/local verification is pending before CODE-UX-03
execution, not a blocker requiring an Emception build before CODE-UX-02.
Preserve compiler identity, applicable CI provenance and old generated assets.
See `CS-ADR-024` and `QUALITY-GATES.md` for the backed-up refresh path.

Resource entry: 31,806 MiB RAM, 6,486 MiB available, 9,391 MiB swap used,
12 logical CPUs. No full stack/build was started. The historical cycle
requests a 12 GiB build heap; it is not suitable for an unmeasured local run
on that baseline. The recorded budget requires adequate memory first.

A suite `GameGuild.Identity.Authentication.UnitTests` possui uma falha externa
reproduzida tambem de forma isolada em
`EmailCodeServiceTests.GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`
(`4572/4573` testes verdes). Nao ha diff no modulo de Authentication e a falha
nao pertence ao contrato de coding; ela deve ser tratada pelo responsavel desse
modulo sem ampliar o escopo da parte 3.

## Evidencias de validacao da etapa ativa

Historical implementation checks are retained below. Current `CODE-UX-01`
evidence is listed separately; it does not certify runtime or product gates.

| Data | Comando ou inspecao | Resultado | Escopo |
| --- | --- | --- | --- |
| 2026-11-18 | `rg` de imports Emception e leitura dos cinco consumidores | cinco fluxos academicos confirmados; package e aliases preservados | 3.1 |
| 2026-11-18 | leitura dos exports publicos do Emception | IDE, controller, presets, testing e overlay publicos cobrem o corte | 3.1 |
| 2026-11-18 | `git status --short --branch` e `git rev-parse HEAD` | worktree limpo no commit-base registrado | 3.1 |
| 2026-11-18 | testes e typecheck de `coding`, `grading-adapter-code` e `coding-surface` | 15 + 15 + 14 testes; tres typechecks verdes | 3.2/3.3 |
| 2026-11-18 | Vitest direto nos sete consumidores web | 7 arquivos e 77 testes verdes com um worker | 3.4 |
| 2026-11-18 | `@game-guild/emception-ui test` | 7 suites e 27 testes verdes; package preservado | 3.4 |
| 2026-11-18 | TypeScript integral de `apps/web` | verde | 3.4 |
| 2026-11-18 | testes focados de `GameGuild.Learning.Courses` | 898 testes verdes | 3.4 |
| 2026-11-18 | `dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2` | sucesso, zero warnings e zero erros | 3.4 |
| 2026-11-18 | build de runtime Emception grading | 49 testes e bundle Vite verdes | 3.4 |
| 2026-11-18 | build de producao web | 337 paginas geradas; passou no retry previsto pelo guard de seguranca do Next | 3.4 |
| 2026-11-18 | testes de classificacao, artifacts, required gate e toolchain binding do CI | 96 testes verdes | 3.4 |
| 2026-11-18 | preflight browser coding cycle | 26 testes verdes | 3.4 |
| 2026-11-18 | browser coding cycle integral local | interrompido fail-closed: CDN local schema 1 nao satisfaz manifest V3 schema 2 | 3.4 |
| 2026-11-18 | publish da API com `-m:2`, captura OpenAPI e gerador oficial de `@game-guild/client` | `exerciseType` presente no contrato; diff gerado restrito a tipos/schema e metadata | 3.4 |
| 2026-11-18 | `@game-guild/client typecheck` e `@game-guild/client build` | verdes | 3.4 |
| 2026-11-19 | inventario `rg` de contratos, parsers, fixtures, adapters, toolchains e payload schemas | `CANON-01` concluido; escopo restrito a coding, adapter, surface, backend CodeAdapter, web e testes | 3.5 |
| 2026-11-19 | suites e typechecks de `coding`, `grading-adapter-code`, `coding-surface` e `grading` | 15 + 15 + 14 + 16 testes verdes; quatro typechecks verdes | 3.5 |
| 2026-11-19 | testes focados dos consumidores web | 11 arquivos e 195 testes verdes com um worker | 3.5 |
| 2026-11-19 | TypeScript integral de `apps/web` com heap de 8 GiB | verde apos regeneracao do client | 3.5 |
| 2026-11-19 | publish da API, captura OpenAPI e geracao oficial de `@game-guild/client` | `exerciseType` requerido; client typecheck e build verdes | 3.5 |
| 2026-11-19 | `CodeAdapter.UnitTests`, `Assessments.UnitTests` e `Courses.UnitTests` | 72 + 453 + 898 testes verdes | 3.5 |
| 2026-11-19 | `AssessmentGradingPostgreSqlHttpTests` com certificado local de Data Protection | 14 testes de integracao PostgreSQL verdes | 3.5 |
| 2026-11-19 | `@game-guild/emception-ui test` | 7 suites e 27 testes verdes; package preservado | 3.5 |
| 2026-11-19 | preflight do coding cycle fora do sandbox | 45 testes verdes, incluindo toolchain, browser observer e processos | 3.5 |
| 2026-11-19 | build de producao web | compilacao, TypeScript e 337 paginas verdes com `cpus: 1` | 3.5 |
| 2026-11-19 | `dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2 -p:TreatWarningsAsErrors=true` | sucesso em 5m40s, zero warnings e zero erros | 3.5 |
| 2026-11-19 | `CodingAssignmentJsonBodyMigrationTests` | 8 testes verdes; snapshot test-only preserva a saida historica sem enfraquecer `ExerciseType` no contrato atual | 3.5 |
| 2026-11-19 | `GameGuild.API.UnitTests` integral | 1882 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authorization.UnitTests` integral | 2815 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authentication.UnitTests` integral e caso isolado | 4572/4573 verdes; falha externa de throttle de email reproduzida isoladamente, sem diff no modulo | 3.5 |
| 2026-11-19 | `@game-guild/coding-surface typecheck` e `test` apos substituir o `Ide` monolitico | typecheck verde; 2 arquivos e 14 testes verdes | correcao visual 3.2/3.3 |
| 2026-11-19 | inspecao humana da autoria no Learning em viewport desktop | workbench amplo confirmado, mas configuracao, output e formularios de testes ainda misturados; parte 3.6 requerida | baseline `CODE-UX-01` |
| 2026-11-19 | revisao documental com Node em modo somente leitura e `git diff --check -- docs/plans/coding-assessment-and-code-studio` | 42 documentos e 109 links locais sem destino ausente; seis subplanos, 22 IDs de aceite e dependencias consistentes; sem erros de whitespace | planejamento 3.6, nao execucao dos gates |

The earlier planning rewrite did not run application builds. Its note follows:

A reescrita documental nao executou builds, testes de aplicacao ou novos
benchmarks de runtime. O comando `node` nao estava no PATH desta sessao; a
checagem usou o executavel instalado em `.nvm/versions/node/v24.16.0/bin/node`,
sem iniciar servicos nem alterar os arquivos de aplicacao ja presentes no worktree.

### `CODE-UX-01` execution evidence - 2026-11-19

| Check | Observed result | Limit |
| --- | --- | --- |
| installed public `emception/testing` calls | integer harness generated; arrays/matrices/aggregate returns rejected | feasibility only, no compiler boot |
| package manifests and installed public exports | core/browser/IDE 4.5.2, headless workspace/testing/canvas exports present | a public export does not establish a working family/environment combination |
| real cycle fixture through `coding-surface-bridge.test.ts` | 5 tests passed, including `CS-REF-21` canonical fixture/redaction/missing-type regression | no API/database writes |
| toolchain/editor/web-runtime preflight suites | 26 tests passed, sequentially | subprocess tests use dummy Node executables, not a Next build |
| preflight ordering regression after edit | affected 14-test toolchain suite passed again; invalid artifacts are rejected before grading-runtime build | no full runtime build |
| actual release manifest preflight | rejected: verified artifact matching frozen identity required | gate remains blocked |
| exact-name GitHub Actions artifact query | `total_count: 0` | read-only; no producer launched, no files downloaded |
| prototype Playwright inspection | 69 checks; six captures, three viewports; keyboard/task navigation and no horizontal overflow | layout only, no Emception or academic behavior |
| prototype browser resource observation | 2.27 seconds; maximum sampled known Chromium RSS 376 MiB; browser closed | not a WASM memory/startup benchmark |
| focused lint, syntax and document checks | fixture/bridge ESLint passed; script syntax passed; 122 local links across 43 docs resolve; diff whitespace clean | no application build |

The commands and capture paths are recorded in `PRODUCT-MODEL.md`. Node
24.16.0 was used explicitly because the tool shell lacks Node on PATH.
Sandbox restrictions required rerunning subprocess/Chromium checks outside
the sandbox; those bounded checks passed. No runtime was left running.

Current work files: `coding-cycle-fixture.mjs`, its importing cycle,
`coding-surface-bridge.test.ts`, the layout HTML specimen, `PRODUCT-MODEL.md`
and shared plan/state/decision/consumer/gap/duplication records. The original
application/source/lockfile changes are preserved and are not this execution's
production implementation. No backend, generated client, migration or Emception
source file was edited.

### Critical review and planning follow-up - 2026-11-19

The actual specimen was inspected in Chromium, alongside the first-party
references recorded in `COMPETITIVE-BASELINE.md`. UX-F01..06 identify missing
typed authoring, learner diagnosis, faithful reference/preview workflows,
host-constrained layouts, capability compatibility and draft recovery.
In the simulated 1280x720 host, the central region was 704 px wide and expected
output began at y=796; the 390x844 learner specimen placed result tabs at
y=1096. Changing the problem/starter to a maximum function did not change its
preview. These are prototype findings, not measurements of live Learning.
Review captures: `/tmp/coding-surface-review-20261119/` (ephemeral local evidence).

The follow-up changes documentation only: 01a, governance, terminal gates,
atomic rows in all six subplans, acceptance mapping and shared handoff.
No prototype, application, dependency, runtime, migration or platform code was
changed during this planning task. Prior application diffs remain untouched.
Passing layout checks have not been promoted to UX or runtime acceptance.

| Planning check | Result | Scope / limitation |
| --- | --- | --- |
| Node 24.16.0 read-only inline audit using installed remark parser/GFM | exit 0; 46 documents, 165 local links, 7 atomic subplans/24 rows; six findings mapped and all 22 CS-REF IDs preserved | local targets and required table structure, not runtime or UX acceptance |
| Node 24.16.0 read-only gate-target/manifest audit | exit 0; 14 existing command paths and six package manifests checked | planned prototype files are explicitly future deliverables, not executed tests |
| `git diff --check -- docs/plans/coding-assessment-and-code-studio` | exit 0 | documentation whitespace |
| Auditor self-review against Meta-Harness and entry scope | no new boundary violation identified; tiered context and next action reconciled; upstream/dev scripts have no diff | same-agent review, not independent human design acceptance |

### Authoring terminology correction - 2026-11-19

Baseline: clean worktree at `efcc1c69fe`. User explicitly requested the label
**Validate exercise** to distinguish authoring preparation from learner grading.
Only prototype copy and plan/state/ADR wording changed; internal IDs, behavior,
production code and grading contracts are unchanged.

Playwright/Node inline check: exact tab-name assertion failed before the edit
(exit 1, zero matches), then passed with keyboard navigation, panel/heading,
status and overflow checks at 1280x720 and 390x844 (exit 0). Mobile capture
was visually inspected. Captures: `/tmp/coding-surface-label-20261119/`.
The default Playwright browser was absent and sandbox launch was denied;
these bootstrap failures were not the behavioral red evidence. The checks
used the already-installed Chrome outside the sandbox, one browser, closed
afterward. No application stack, build, installation or real runtime was run.

### 3.6.1a interactive prototype execution - 2026-11-20

Baseline: `8c6c31b459a994e7f7f6fd7d0166d8446632a377`, clean worktree after
the user committed prior naming changes. This execution changed only the HTML,
its new `06-refoundation/prototype/` directory and shared documentation/ADRs.
Production packages, Emception/UI, dependency manifests, auth/platform, API,
migrations and developer commands have no task delta. No consumer cutover.

All rows followed specification/contracts, strict compilation, an independent
behavioral red, minimal implementation, then Builder/Auditor checks:

| Slice | Behavioral red / audited defect | Green design evidence |
| --- | --- | --- |
| UXP-01 | absent title/blank draft controls | shell, keyboard tasks, malformed states and bounded harness |
| UXP-02 | absent signature/family fields; duplicate environment field/panel ID | function and I/O from blank, type-specific values, stable case CRUD, explicit empty output and compatible environments |
| UXP-03 | absent reference/preview controls | correct/wrong records, stale candidate identity, edited/redacted preview, independent files and escaping |
| UXP-04 | absent learner entry | wrong/expected/actual, absent oracle, compile/error/cancel/stale states, frozen acknowledgement and preserved host feedback |
| UXP-05 | absent save/panel controls; clipped long names/lost case scroll/mobile label collision in capture review | retained failures/conflicts, scoped cloned recovery, lossless cancel, panels, focused views and keyboard reachability |
| UXP-06 | incomplete selection accepted; unknown fixture/unsafe IDs; no-op typed commands; unavailable capability; modal return focus; pending signature and scratch changes incorrectly affecting submission | all 13 frozen browser oracles and 11 Node tests pass; cases are required by exact ID, no skip/zero-case success; same-agent adversarial audit completed |

Terminal checks use installed Node 24.16.0, TypeScript 6.0.3, ESLint 10.13.0,
Playwright 1.64.1 and existing Chrome. The shell lacks Node on PATH, so commands
used `/home/miguel/.nvm/versions/node/v24.16.0/bin/node` directly with the root
`node_modules/typescript/bin/tsc` and `node_modules/eslint/bin/eslint.js`.
No dependency was installed. Node subprocess/Chrome checks ran outside the
restricted sandbox; its earlier file-only summary was not accepted as proof.

```bash
node node_modules/typescript/bin/tsc --project docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/tsconfig.json --noEmit
node node_modules/eslint/bin/eslint.js --no-inline-config --max-warnings=0 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype
taskset -c 0-9 node --test --test-concurrency=1 docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/contracts.test.mjs docs/plans/coding-assessment-and-code-studio/03-coding-surface/06-refoundation/prototype/workflows.test.mjs
```

Results: types/lint exit 0, final cumulative run 24/24 tests, zero skipped,
about 12 seconds including all refreshed captures. Source formatting uses the
installed Prettier. G-DOC exit 0: 47 documents, 168 local links, all 24 atomic
rows populated and all 22 CS-REF IDs preserved; new sources are ASCII and
`git diff --check` is clean. All task paths are confined to this docs subtree.
Final capture audit also reproduced a selected case leaving the visible list
after a mobile/desktop transition. An explicit failing viewport assertion was
added; selection scrolling was fixed and the full 24-test gate passed again.
The exact task is also reviewable through the prototype's frozen checklist.
Application/API/Emception build and security suites are inapplicable to this
documentation-only design fixture; they were not rerun or claimed passed.

Captures are ignored artifacts in
`apps/web/test-results/coding-surface-refoundation/`: six `uxp05-teacher-*` /
`uxp05-learner-*` images at 1600x1000, 1280x720 and 390x844, plus
`uxp06-teacher-io.png` and three `uxp06-canvas-*` images. Teacher/learner and
canvas captures were visually inspected. Canvas pixels, movement, stop/reopen
and distinct echo input are synthetic design checks, not Emception conformance.

Observed resources: one browser/page, seven reported Chromium processes with
summed RSS 858 MiB, Node RSS 132 MiB; the RSS sum double-counts shared memory
and is a single sample, not a peak/physical-memory benchmark. CPU affinity
`0-9` reserves two of 12 cores. Entry MemAvailable 15,718 MiB; no heavy stack
or compiler started. Browser/context disposal passed; no services left by the
prototype. Production runtime budgets remain unproven.

Actual frozen-toolchain preflight was rerun using the command in G-PREFLIGHT:
exit 1, `Code grading requires verified artifacts matching its frozen toolchain.`
No permissive manifest, upstream edit or producer launch was used. The earlier
exact-name GitHub query remains historical evidence, not a new remote query.

Human: pending. UXP-06 automation and same-agent audit do not approve usability.
No implicit acceptance of scalar-only scope or runtime support. Saved/recovery
records are in-memory for the open tab; actual refresh restarts the design
study. Independent runtime and durable persistence remain later requirements.

### UXP-07 editor-fidelity follow-up - 2026-11-20

Specification/contracts preceded behavioral changes. The strict contract
compiled before interaction implementation. An explicit new command-schema
test failed before validation was added; all three new browser oracles failed
on absent source tabs/explorer, not on harness/bootstrap. The earlier dirty
prototype work was preserved. Allowed files stayed within the prototype,
HTML entry and its current plan/model/gates/ADR/ledger.

Builder: a shared editor composition now covers author, learner and redacted
preview. Open tabs, explorer, colorized/numbered native text, per-file caret/
scroll, code modification markers and read-only support represent the intended
Monaco-backed experience without loading Monaco or a runtime. Author creation
and rename reject traversal/path collisions; deletion requires confirmation;
tab closure never deletes buffers. Signature configuration collapses outside
editing; learner study controls collapse on mobile entry to preserve code space.
Reference/incorrect preparation uses the same visual editor, separately from
the supplied learner files. No new learner file-management policy was assumed.

Auditor (same-agent): caught an explorer drawer intercepting tabs at constrained
width, missing keyboard focus after closing the last tab and support-file edits
reusing an old recorded verdict. Those causes were fixed; a Node test reproduced
the simulation defect before the fix. Current file sets reach the simulation
and unrecorded support/source combinations cannot fabricate results. Added
empty-tab, save-marker, read-only, per-file position, Tab/Escape and forced-color
checks. No types, lint rules or required assertions were suppressed.

Terminal: the same G-PROT commands above pass after the final changes, exit 0:
strict TypeScript, ESLint without warnings, and 30/30 tests (14 Node + 16 browser),
zero skips, 14.6 seconds. One Chrome, sequential pages, CPU affinity 0-9 leaves
two cores free. Network/compiler workers remain absent; context/browser cleanup
passes. No stack, dependency installation, heavy build or service was started.

Six additional captures `uxp07-author-editor-*` / `uxp07-learner-editor-*`
are in the same ignored output directory. Inspected author 1600/1280/390 and
learner 1280/390 captures; the browser also asserts mobile code height >=120 px,
command reachability and no page overflow. Native rendering is a design
specimen, not proof of editor language services or public runtime integration.
The earlier measured RSS sample is historical, not a new resource benchmark.

G-DOC passed: 47 Markdown documents, 168 valid relative links, all 25 atomic
rows populated, ASCII prototype sources and clean `git diff --check`. The
read-only AST check identifies specification tables by both Slice and
Specification headers; evidence/checklist tables are not misclassified as
six-column atomic specifications.
Current instructions and CS-ADR-022 record the updated seven-slice selector.
Human design acceptance, compound-value disposition and verified artifact
obligations remain pending; no production gate was entered or waived.

### UX01-C closure audit - 2026-11-20

Historical audit: its mandatory exact-source/build-only CI interpretation and
next-action proposal below were superseded by CS-ADR-024. Test observations
remain evidence of their stated scope, not runtime or production acceptance.

Baseline is the committed prototype `40a8470b46`, with clean entry worktree.
Scope was read-only technical validation plus the current documentation;
the artifact/compound decisions were not bypassed by starting production work.

| Check | Current result | Meaning |
| --- | --- | --- |
| strict prototype typecheck/lint and full sequential design suite | exit 0; 30/30 tests, zero skips, 14.3 seconds | committed design/contract regression only |
| coding and grading-adapter-code typechecks, focused ESLint | exit 0, no warnings | no schema/type/lint weakening |
| coding package suite | 15/15 | current owned contracts |
| grading-adapter-code package suite | 15/15 | pure adapter parity |
| real canonical bridge focused suite | 5/5 | current fixture, required family and private-data redaction |
| preflight/editor/runtime-launch unit suites | 26/26, zero skips | no Next, Docker, API or WASM boot; mocked child processes |
| official acquisition unit suite | 36/36 | mocked GitHub, digests/provenance/archive/producer-step checks, no download; duplicate-ZIP warnings are intentional negative fixtures |
| installed public ESM harness probe, cwd apps/web | 4 scalar harnesses pass; 6 compound probes reject | no compiler boot or private imports; not full execution conformance |
| current exact-source producer/artifact API queries | 0 runs, 0 artifacts | no available producer result for the current committed source |
| actual frozen manifest check | exit 1: verified artifact matching frozen toolchain required | G-PREFLIGHT not passed |

Commands used the installed Node 24.16.0 and package-local Vitest 5.0.5,
TypeScript 6.0.3, ESLint 10.13.0 and Chrome. Package tests ran with
`node node_modules/vitest/vitest.mjs run --maxWorkers=1` from their workspace;
the bridge selected `src/lib/coding-assignment/coding-surface-bridge.test.ts`
from `apps/web`. Package types used root TypeScript with their explicit
`--project .../tsconfig.json --noEmit --pretty false`; no tool was installed.
The public probe used ESM `import ... from 'emception/testing'`, not a CJS
require resolver or an upstream private file. Preflight and prototype commands
are in QUALITY-GATES; acquisition used the existing Python unittest suite.

Resource entry: MemAvailable 14,992 MiB, swap used 8,467 MiB. All checks stayed
lightweight, sequential and CPU-affined to 0-9 (two cores reserved), one browser
with disposed pages/contexts. No full application/API/compiler build, producer,
release write, database, dependency installation or background service.
Python bytecode created by its tests was removed; only docs remain changed.

Read-only cloud review: latest successful workflow run 44523205221 skipped
the producer and uploaded no artifacts; overall green is not release evidence.
Current manual `emception.yml` would also deploy the IDE to GitHub Pages even
with `publish_packages: false`. This contradicts the earlier safe-dispatch
assumption. CS-ADR-023 records that safety finding; its build-only proposal
was subsequently withdrawn by CS-ADR-024. No remote workflow/issue/publication
change was performed.

Human questions presented: accept the design after its walkthrough; approve
temporary scalar-only function scope or require compound support first; hand
build-only CI changes to the Emception owner or authorize that CI-only scope
here. These historical questions are not current instructions: design/scalar
scope was explicitly accepted and the unnecessary CI proposal withdrawn.
No automatic human acceptance or gate completion was inferred.

Final documentation audit: 47 documents, 168 valid local links and 25 complete
atomic rows; `git diff --check` passes. Explicit diff against runtime/IDE, CI,
apps/packages, scripts and root AGENTS is empty. Only the six current planning/
state documents changed; no unrelated source or generated bytecode remains.

## Arquivos alterados pela implementacao

- contrato canonico unico de coding, sem aliases de compatibilidade;
- identidade unica e concordante do adapter backend e TypeScript;
- novo `packages/features/coding-surface`;
- workbench visual proprio em `coding-workbench.tsx`, com runtime headless e
  sem renderizar a IDE generica;
- campo JSON `ExerciseType` e validacao no modelo de Learning Courses;
- contrato OpenAPI e cliente TypeScript gerado para `ExerciseType`;
- bridge e cinco consumidores de grading em `apps/web`;
- lane `coding`, classificacao e required gate no CI;
- testes, fixtures e documentos de checkpoint associados.
- ajuste exclusivamente test-only para ler a saida historica da migration; a
  migration, snapshots de banco e schema permanecem inalterados.

## Proxima acao exata

Continue [`3.6.2`](./06-refoundation/02-domain-drafts-and-data-boundaries.md):

1. Retain the explicit user design approval recorded for
   [3.6.1a](./06-refoundation/01a-prototype-workflows-and-critical-review.md).
   Do not repeat the prototype work or mislabel that approval as production
   runtime/usability acceptance. Initial scalar-only scope is also explicitly
   accepted in CS-ADR-015; compound support remains tracked in EMC-GAP-001.
2. Retain the implemented/tested UX02-A contracts and UX02-B persistence ports.
   Prepare UX02-C through read-only published-use inventory and its immutable
   publication/projection specification. The broader Authentication failure and
   API resource stop are still open, not resolved by the independent UX02-B
   checks. No production/runtime gate is closed or skipped. Follow the atomic
   cycle and resolve the required broader gate with its owner/resource
   disposition before dependent acceptance. Do not compile/publish Emception
   for this domain/draft work or install a second lesson autosave writer.
3. Before CODE-UX-03 real execution, preserve old generated CDN/release copies,
   explicitly refresh the compatible pinned published assets and pass both
   local manifest checks in G-PREFLIGHT. The current helper can retain schema 1;
   rerunning sync/dev without a backed-up refresh is not readiness evidence.
   Inspect actual changed-path classification for conditional CI provenance.
4. Recheck memory against the recorded budget before any full stack run.
   Preserve the existing developer environment; do not reset data or stop
   unrelated processes to make room.
   Use a local toolchain build only if compatible assets cannot be obtained
   or a separately approved runtime change genuinely requires one. No such
   need is established for the already published 4.5.2 assets.

Nao iniciar o programa abrangente `CODE-E2E-00` antes de `CODE-UX-06` nem
declarar qualidade de concorrencia apenas com a reescrita dos documentos.

### Subsequent product response - 2026-11-20

The user explicitly approved (1) the current prototype design and (2) the
initial simple-type function scope. These decisions are now recorded in
CS-ADR-015/022, 01a, the product model and CS-REF-18 disposition. No task-by-task
human test log, production acceptance or compound-runtime support was invented.
For (3), the user requested an explanation of the build; this is not approval
to modify CI, dispatch a workflow, deploy the IDE or publish packages. Only
documentation was updated; no build or new test run was necessary for these
recorded decisions. Its then-current artifact prerequisite was later corrected
by CS-ADR-024; it is not an instruction to wait for a compiler build.

### Consumer artifact planning correction - 2026-11-20

The user explicitly requested correction of the mandatory Emception build /
publication dependency. CS-ADR-024 records the replacement policy. Changes
are confined to the plans and shared handoffs; existing design/scalar-scope
approval, strict runtime checks, historical data and upstream ownership stay.

Read-only evidence gathered immediately before this update:

- npm metadata returned HTTP 200/version 4.5.2 for both public packages.
- Toolchain/Core CDN manifests returned HTTP 200/schema 2/version 4.5.2 and
  passed the current frozen version/ABI/lock/receipt/fingerprint check.
- Actual local canonical/web manifests are schema 1 and fail that check;
  other conventional local staged manifest locations were absent.
- Six classifier probes distinguish feature/docs consumers from runtime and
  lockfile changes; the 36 existing acquisition tests passed. No CI rule was
  changed, and required exact-source provenance is not bypassed.

This is manifest/policy evidence, not a full download, byte-level asset audit,
local refresh, browser execution, CODE-UX-01 closure or production acceptance.
The next executor follows the current action list above, not superseded
build-only instructions in historical records. No runtime/IDE, CI, app source,
database, dependency or developer-service change is part of this update.

Documentation Builder/Auditor pass (same agent, not independent product review):

| Check | Result | Scope |
| --- | --- | --- |
| installed Node read-only link/atomic-table/dependency audit | exit 0; 47 documents, 168 local links, 7 specification tables and 25 complete atomic rows | validates baseline/UX03 ownership and required safeguards, not runtime behavior |
| `python3 -B -m unittest discover -s scripts/ci/tests -p acquire_emception_release_test.py` | exit 0; 36 tests | existing conditional acquisition contract; duplicate-ZIP warnings are intentional negative fixtures |
| six direct `needs_source_release()` probes | exit 0; feature/domain/adapter/docs false, runtime and lockfile true | no classifier edit or new source-build requirement |
| `git diff --check -- docs/plans/coding-assessment-and-code-studio` and changed-path review | exit 0; only planning Markdown changed | preserves the pre-existing dirty plan changes; no app/runtime/IDE/CI edits |

The read-only audit uses filesystem APIs after sandbox child-process denial;
atomic-shape checks target specification tables, not historical result/checklist
tables. No production type/lint rule was relaxed and no unexecuted runtime,
full build or product gate was marked passed. No heavy test was needed for
this documentation-only correction.

## Checklist de checkpoint

Antes de interromper ou trocar de executor:

- [x] baseline de avaliacao registrada;
- [x] etapa e gate refletem o estado real;
- [x] arquivos alterados listados;
- [x] comandos e resultados registrados;
- [x] decisoes transversais registradas;
- [x] consumer map atualizado;
- [x] gaps e duplicacoes atualizados quando aplicavel;
- [x] bloqueios possuem evidencia;
- [x] proxima acao e executavel sem depender da conversa.
