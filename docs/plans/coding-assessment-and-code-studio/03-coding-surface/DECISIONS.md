# Coding surface decision log

Status: ativo durante a parte 3.

Este arquivo preserva decisoes que afetam mais de um subplano. Ele nao e um
diario de implementacao e nao substitui contratos ou testes. Uma decisao nova
deve usar o proximo ID `CS-ADR-NNN`, registrar evidencia e ser escrita antes da
implementacao que depende dela.

Status permitidos: `proposta`, `aceita`, `substituida` e `rejeitada`.

## `CS-ADR-001`. Superficie paralela para grading

- Data: 2026-11-18.
- Status: aceita.
- Contexto: `@game-guild/emception-ui` e um produto existente, com consumidores,
  aliases e testes que nao pertencem integralmente a grading.
- Decisao: criar `@game-guild/coding-surface` como superficie paralela.
  Consumidores de grading selecionados podem migrar; o package existente nao e
  editado, movido, deprecado ou removido.
- Alternativa rejeitada: decompor `emception-ui` para transforma-lo na base
  compartilhada da nova superficie.
- Consequencias: duplicacao de composicao GameGuild e aceita temporariamente e
  deve constar no `DUPLICATION-REGISTER.md`.
- Evidencia: indice da parte 3 e roadmap principal.
- Afeta: 3.1, 3.2, 3.4.

## `CS-ADR-002`. Efeitos academicos pertencem ao host

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a mesma superficie atende preview local e fluxo oficial, mas somente
  a aplicacao possui sessao, tenant, autorizacao e client da API.
- Decisao: `coding-surface` emite intents tipadas e nunca chama endpoints nem
  persiste submission, score, review ou release.
- Alternativa rejeitada: encapsular fetch e mutations academicas no package de
  UI.
- Consequencias: `apps/web` adapta dados e comandos; refresh usa a fonte do
  servidor.
- Evidencia: contratos das partes 01 e 02 e indice da parte 3.
- Afeta: 3.2, 3.3, 3.4.

## `CS-ADR-003`. Tipo, toolchain, persona e review sao eixos independentes

- Data: 2026-11-18.
- Status: aceita.
- Contexto: inferir experiencia pelo tipo de teste, linguagem ou review cria
  acoplamento e impede extensao segura.
- Decisao: o tipo do exercicio e discriminado no contrato canonico; toolchain,
  capabilities, modo e politica de review permanecem ortogonais.
- Alternativa rejeitada: deduzir tipo por extensao, linguagem ou
  `tests[].kind`.
- Consequencias: o registro exige renderers explicitos e falha para tipo nao
  registrado. O corte inicial implementa somente `function` e `standard-io`.
- Evidencia: dominio e adapter da parte 01.
- Afeta: 3.2, 3.3, 3.4.

## `CS-ADR-004`. Contexto de execucao persistido em arquivos

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a parte 3 atravessa varias sessoes, merges e contextos de IA.
- Decisao: estado, decisoes, consumidores, gaps e duplicacoes possuem arquivos
  canonicos atualizados em cada checkpoint.
- Alternativa rejeitada: depender da conversa ou de comentarios temporarios.
- Consequencias: um gate nao esta concluido enquanto as evidencias e a proxima
  acao nao estiverem registradas em `IMPLEMENTATION-STATE.md`.
- Evidencia: protocolo de contexto persistente do indice.
- Afeta: todos os subplanos.

## `CS-ADR-005`. Proposta inicial de formato paralelo para o tipo de exercicio

- Data: 2026-11-18.
- Status: substituida por `CS-ADR-008` antes de release.
- Contexto: o formato inicial nao possuia um discriminante de experiencia,
  enquanto o payload de Learning continha apenas testes e toolchain. Inferir a
  experiencia por linguagem ou por `tests[].kind` violaria `CS-ADR-003`.
- Decisao historica: preservar o formato incompleto e adicionar outro formato
  com `exerciseType: function | standard-io` e fallback na bridge do host.
- Motivo da substituicao: nao havia dado oficial que justificasse dois formatos;
  a proposta criava compatibilidade artificial dentro do mesmo desenvolvimento.
- Consequencias atuais: `CS-ADR-008` tornou `exerciseType` obrigatorio no unico
  contrato canonico. Nao ha fallback, tabela ou migration.
- Evidencia: inventario e consolidacao da parte 3.5.
- Afeta: 3.3 e 3.4.

## `CS-ADR-008`. Um unico contrato canonico para code antes do lancamento

- Data: 2026-11-18.
- Status: aceita.
- Contexto: contratos paralelos e adapters historicos foram criados durante o
  mesmo programa de implementacao. Nao ha dados, revisoes ou submissions
  publicados que exijam compatibilidade.
- Decisao: consolidar o formato completo atual como o primeiro contrato
  canonico de code. `exerciseType` e obrigatorio; tipos de propriedade de code
  nao usam sufixos numericos preventivos; existe somente um adapter e uma
  toolchain corrente.
  `schemaVersion: 1` e a identidade `adapterVersion: "1"` permanecem como
  discriminantes estritos, sem parser ou registro alternativo.
- Alternativa rejeitada: preservar caminhos criados apenas durante o
  desenvolvimento sob a justificativa de compatibilidade futura.
- Consequencias: fixtures locais podem ser descartadas, nao ha migration e
  payloads incompletos falham fechado. Contratos V1 importados do grading
  generico permanecem fora deste corte.
- Evidencia: ausencia de release e inventario inicial da parte 3.5.
- Afeta: partes 01, 02, 3.3, 3.4 e 3.5.

## `CS-ADR-006`. Bridge de wire pertence ao host

- Data: 2026-11-18.
- Status: aceita.
- Contexto: o DTO atual usa PascalCase e a estrutura historica de Learning,
  enquanto `coding-surface` deve consumir contratos canonicos e nao conhecer
  endpoints ou DTOs da aplicacao.
- Decisao: manter a conversao wire-canonico em `apps/web`; a superficie recebe
  somente contratos de `@game-guild/coding` e emite submissions canonicas.
- Alternativa rejeitada: fazer `coding-surface` depender dos tipos ou actions de
  `apps/web`.
- Consequencias: auth, tenant, mutations, retry e navegacao permanecem no host.
- Evidencia: consumidores mapeados em `CONSUMER-MAP.md`.
- Afeta: 3.4.

## `CS-ADR-007`. Browser cycle exige artifact exato e falha fechado

- Data: 2026-11-18.
- Status: aceita.
- Contexto: o CDN versionado presente no workspace ainda usa manifest schema 1,
  mas o coding cycle V3 exige schema 2, receipt, fingerprint e identidade
  congelada do adapter, nao um novo compilador para cada commit de UI.
- Decisao: validar a identidade Toolchain antes de montar o runtime e executar
  o browser E2E. O helper oficial exige artifact do source exato somente quando
  sua classificacao de mudancas o determina; caso contrario, o consumidor usa
  o pacote versionado existente. A verificacao local e do CI permanece
  fail-closed. A correcao desta distincao esta em `CS-ADR-024`.
- Alternativas rejeitadas: relaxar o verificador, aceitar o artifact legado ou
  fabricar manualmente manifest e provenance.
- Consequencias: os testes de package, componentes, API e runtime podem passar
  localmente; a conclusao do gate browser depende da evidencia do lane com o
  artifact correto.
- Evidencia: `apps/web/scripts/coding-cycle-toolchain-support.mjs`,
  `scripts/ci/acquire-emception-release.py` e lane `coding` em
  `.github/workflows/pr-verify.yml`.
- Afeta: 3.4.

## `CS-ADR-009`. Correcao test-only do binding do adapter extraido

- Data: 2026-11-18.
- Status: aceita.
- Contexto: a parte 2 moveu `CodeAssessmentTypeAdapter.cs` para o modulo
  `GameGuild.Learning.Assessments.CodeAdapter`, mas o teste de binding do
  Toolchain ainda lia o path removido dentro de Assessments.
- Decisao: atualizar somente a referencia de arquivo em
  `tools/emception/grading/toolchain-binding.test.mjs` para o path atual.
- Alternativas rejeitadas: recriar um arquivo legado no path antigo ou remover
  a verificacao de paridade.
- Consequencias: nenhuma fonte, runtime, default ou UI do Emception muda; o
  teste volta a verificar as mesmas constantes no owner atual do adapter.
- Evidencia: build do runtime grading com 49 testes verdes e bundle Vite verde.
- Afeta: 3.4.

## `CS-ADR-010`. Workbench proprio sobre o runtime headless

- Data: 2026-11-19.
- Status: aceita.
- Contexto: a primeira implementacao de `coding-surface` renderizava o
  componente monolitico `Ide` de `@gameguild/emception-ide`. Embora o package
  e os contratos fossem novos, Explorer, docking, canvas, terminal e hierarquia
  visual continuavam sendo a IDE generica. Isso contradizia o modelo de
  superficie orientada ao tipo de exercicio e a experiencia observada no
  Learning permanecia praticamente igual ao `Coding assignment` anterior.
- Decisao: `coding-surface` possui o workbench visual. Ele usa o entrypoint
  publico headless `createEmception()` para runtime, VFS e test runner, e usa
  Monaco para editar os arquivos autorizados. O package nao renderiza `Ide`,
  nao importa internals e nao modifica Emception ou `emception-ui`.
- Alternativas rejeitadas: renomear apenas a opcao no Learning; aplicar CSS
  sobre a IDE completa; ou alterar/decompor o package mantido pelo time do
  Emception.
- Consequencias vigentes: tabs, contexto, console e resultados pertencem ao
  novo package. A composicao headless permanece; controllers e intents nossos
  podem evoluir na refundacao. A restricao original de canvas/terminal a um
  perfil futuro foi substituida por `CS-ADR-012`: sao capabilities ortogonais,
  sujeitas a prova da combinacao e nao a criacao artificial de outro tipo.
- Evidencia: `packages/features/coding-surface/src/coding-workbench.tsx` e teste
  que prova a ausencia do componente monolitico.
- Afeta: 3.2, 3.3, 3.4 e validacao 4.0.

## `CS-ADR-011`. Autoria e execucao usam superficies distintas

- Data: 2026-11-19.
- Status: substituida por `CS-ADR-012`; mantida como historico do plano inicial.
- Contexto: a primeira autoria de coding reutilizou a superficie de execucao e
  injetou formularios de linguagem, arquivos e testes nos slots do workbench.
  Isso misturou configuracao do exercicio, codigo, terminal e edicao de testes
  numa coluna longa, mesmo quando o Learning oferecia largura suficiente. O
  seletor de tipo tambem nao selecionava um editor de configuracao proprio,
  como ocorre no registry de tipos do Quiz.
- Decisao: criar `CodingAuthoringSurface` como shell exclusivo do professor e
  manter `CodingSurface` orientada a execucao. A autoria tera workbench amplo e
  inspector lateral com Setup, Tests e Files. Um registry tipado selecionara o
  editor, validacao, defaults e capabilities de cada `exerciseType`. O estado
  de formulario sera um draft discriminado que pode estar incompleto; o
  contrato canonico `CodingAuthoringDefinition` so sera produzido depois da
  validacao. Formularios de autoria nao serao injetados em slots genericos do
  console ou do explorer.
- Alternativas rejeitadas: apenas ampliar o container atual; manter todos os
  formularios abaixo do terminal; ou adicionar condicionais de tipo no host sem
  um boundary de perfil.
- Consequencias: `standard-io` expoe somente testes stdio e `function` somente
  casos funcionais; troca de tipo trata dados incompativeis explicitamente. A
  composicao responsiva usa a largura real do container. O runtime headless e
  os exports publicos do Emception continuam preservados e nenhuma mudanca em
  `@game-guild/emception-ui` e necessaria.
- Evidencia: inspecao humana da autoria no Learning e plano
  `06-authoring-information-architecture-and-teacher-ux.md`.
- Afeta: 3.2, 3.3, 3.4, 3.6 e entrada da parte 4.

## `CS-ADR-012`. Refundacao orientada ao produto, nao ao wrapper atual

- Data: 2026-11-19.
- Status: aceita.
- Contexto: a revisao do fluxo e das referencias oficiais de LeetCode,
  HackerRank, Codio, CodeGrade e CodinGame mostrou que mover formularios para
  uma lateral nao resolve autoria, resolucao, recuperacao e validacao da
  atividade. O usuario autorizou reescrever o plano para refundar a superficie,
  sem preservar por preciosismo a implementacao inicial.
- Decisao: executar a 3.6 em seis etapas de produto, dados, runtime, autoria,
  learner/reviewer e integracao. Autoria e resolucao possuem composicoes
  proprias sobre uma fundacao compartilhada; paineis sao adaptados a tarefa,
  sem largura lateral obrigatoria. Componentes, slots e contratos nossos
  podem ser substituidos quando o modelo exigir, com consumidores e dados
  inventariados. Nao criar compatibilidade V2 especulativa.
- Decisao de modelo: familia, preparacao pedagogica, linguagem/ambiente,
  capabilities, apresentacao e politica academica sao eixos separados.
  Canvas e terminal podem atender familias existentes quando a combinacao
  estiver comprovada; nao sao reservados a um futuro tipo visual. Debugging
  normalmente e variante de starter code. Function e standard I/O sao o corte
  inicial; arrays/matrizes exigem decisao de viabilidade e escopo explicita.
- Fronteiras preservadas: grading possui score/review/release; apps/web e o
  dominio possuem persistencia/autorizacao. Emception e emception-ui nao sao
  editados. Mudanca necessaria de banco segue `SCHEMA-GATE` e migration real;
  nenhum dado existente e descartado apenas por ser de desenvolvimento.
- Alternativas rejeitadas: manter a arvore atual a qualquer custo; colocar toda
  a autoria num inspector estreito; tratar canvas como tipo; adicionar campos
  somente na UI sem persistencia; ou expandir para todas as features dos concorrentes.
- Consequencias: qualidade e medida pelos casos `CS-REF-*`, incluindo jornadas
  humanas e recursos. A matriz e requisito planejado, nao evidencia de sucesso.
  Limitacoes nao podem ser descartadas pelo executor para declarar conclusao.
- Evidencia: indice 3.6, `06-refoundation/COMPETITIVE-BASELINE.md` e
  `06-refoundation/ACCEPTANCE-MATRIX.md`.
- Afeta: 3.2 a 3.6, entrada da parte 4 e criterios de aceite do roadmap.
- Substitui: `CS-ADR-011` e a restricao de capabilities futuras de `CS-ADR-010`.

## `CS-ADR-013`. Preparacao do harness precede validacao da refundacao

- Data: 2026-11-19.
- Status: aceita.
- Contexto: 3.6 exigia runtime/browser reais, mas a fixture canonica e a
  aquisicao do artifact estavam somente em 4.0, bloqueado pela propria 3.6.
- Decisao atualizada em 2026-11-20 (`CS-ADR-024`): `CODE-UX-01` prepara fixture,
  contrato de artefatos e testes leves; `CODE-UX-03` sincroniza os binarios
  publicados compativeis e passa preflight local antes da execucao real.
  `CODE-UX-02` nao depende de build ou boot do compilador. 4.0 revalida na branch
  reconciliada e fecha a evidencia oficial de `CODE-UI-07`.
- Alternativas rejeitadas: esperar 4.0 para testar 3.6; substituir execucao real
  por mocks; relaxar provenance; ou declarar aceite E2E so com preflight verde.
- Consequencias: gates de produto e gate CI conservam evidencias distintas;
  indisponibilidade do artifact bloqueia o runtime afetado sem autorizar
  manifest fabricado ou alteracao no Emception.
- Evidencia: sequencia 3.6.1 -> 3.6.6 -> 4.0.
- Afeta: 3.4, 3.6 e 4.0.

## `CS-ADR-014`. One revision-bound problem and one authoring draft

- Date: 2026-11-19.
- Status: accepted as the implementation model; contract/persistence work is
  still pending `CODE-UX-02`.
- Evidence: `ProgramContent.JsonBody` already stores the coding definition;
  `ProgramContentAuthoringService` already owns JSON draft storage, expected
  revision/ETag and publication. The separate coding upsert currently writes
  `ProgramContent.JsonBody` directly. The current UI skips autosave while
  invalid, and a code edit is not itself a complete authoring draft.
- Decision: reuse Learning's shared draft and publication boundary. Add
  missing problem/reference concepts to the owned coding JSON contract and
  snapshot, with TS/backend/projection parity. Do not create a second mutable
  problem editor in lesson metadata or a second coding-draft table.
  The refounded host must converge its authoring entry points on that draft;
  the legacy direct upsert is not a parallel writer for the new editor.
- Draft: use a distinct incomplete discriminated authoring state, with stable
  file/case IDs. Server Saved means an acknowledged expected-revision write.
  Optional local recovery is explicitly local, scoped to actor/tenant/content/
  base revision and reconciled before restoring. The shared server draft is
  not per-author private storage. Teacher-only references use authorized
  authoring data; never silently put them in learner recovery or VFS.
- Data: preserve existing definitions and immutable submissions. Actual
  published data has not been counted in this session; inventory through
  authorized reads before changing its interpretation. No schema/migration
  change is authorized by this observation alone; existing JSON storage is
  the first implementation path.

| Field | Current authoritative location | Target owner / persistence | Revision and projection rule |
| --- | --- | --- | --- |
| Lesson title, slug, catalog description | Learning authoring payload / `ProgramContent` | host remains the single editable owner | capture any title needed for immutable assessment presentation; navigation metadata is not the problem answer context |
| Problem statement | absent as an explicit coding field; `JsonBody` is already occupied by coding | coding problem data within the existing authoring JSON; render with existing content primitives | freeze with the coding revision; learner/reviewer use that frozen problem, not mutable lesson chrome |
| Examples and constraints | absent as structured coding data | same problem object, never separate UI-only fields | learner-visible and snapshot-bound; example/reference are distinct concepts |
| Family and function signature | `ExerciseType`; signatures currently repeated in functional groups | coding draft/definition; one coherent function signature | normalize/reconcile cases explicitly; no inference from test kinds or preset |
| Language, toolchain, execution requirements | `Environment`, canonical toolchain and limits | coding execution contract + host binding to the exact runtime | freeze reproducibility requirements; resolve only supported combinations |
| Starter and support files | `Data.Files`, visibility/modifiable flags | coding definition, stable identities and protected-file rules | only public authorized files reach learner delivery/VFS |
| Reference/wrong solutions | no dedicated concept; private files currently mean compilation inputs | protected authoring validation data separate from starter/support files | a validation solution replaces the intended candidate files in an isolated validation operation; never compiles accidentally alongside starter or reaches learner data |
| Tests, visibility and expected values | `Tests.Public`/`Private`, canonical test contracts | coding owns technical cases; grading owns academic value semantics | freeze IDs/expected data; private cases excluded by server projection, not hidden controls |
| Runtime capability availability | partly preset features / profile flags | public API identity + environment + requirements + authorized mode resolver | derived availability; persist requirements, not a mutable assertion of support |
| Panel sizes, selected task/file and output tab | component state | surface local view preferences | no effect on snapshots, permissions or scoring |
| Incomplete teacher draft | React fields; valid-only direct upsert | existing Learning JSON draft + expected revision | recover through the host; cannot pretend to be a validated definition |
| Learner edits and scratch cases | namespace helper and browser storage | host-scoped local recovery; server submission stays authoritative | tenant/actor/revision/attempt isolation; restore mutable allowed text files only |
| Validation result | in-memory run result | local diagnostic evidence bound to candidate and runtime identity | invalidate after material edits; no academic status or fake persisted success |
| Published assessment revision | grading immutable manifest/snapshot | existing grading/domain publication flow | include the exact coding/problem candidate; subsequent edits cannot rewrite it |
| Official submission/review/release | grading runtime API and frozen response | grading and authorized host commands | immutable submission capture, retry key, released-result policy; no surface-owned score or release |

- Rejected: using a canonical definition type for invalid form state; storing
  statement only in a React prop; reusing private starter files as a reference
  solution; an autosave label that claims server persistence after a local edit.
- Implementation trace and layouts:
  [`PRODUCT-MODEL.md`](./06-refoundation/PRODUCT-MODEL.md).
- Affects: `CS-REF-06` through `09`, `15`, `16`, `19`, `22`; `CODE-UX-02` onward.

## `CS-ADR-015`. Compound values require an explicit upstream disposition

- Date: 2026-11-19.
- Status: initial scalar-only scope explicitly accepted by the user on
  2026-11-20; compound support remains deferred, not implemented.
- Evidence: the installed public `emception/testing` entry point successfully
  generates an integer function harness and rejects Array parameters, returns
  and nested array values. The source and official Courses validator retain a
  scalar-only contract. Generic custom test callbacks are not a typed compound
  function transport provided by the runtime.
- Decision: continue the initial function/I/O experience with the four
  currently supported scalar kinds, record `EMC-GAP-001` with the Emception
  owner, and add arrays/matrices only through a coherent public harness and
  official executor contract. The user explicitly answered yes to the initial
  simple-type scope. This resolves the CS-REF-18 product disposition, not
  array/matrix capability or browser/server execution conformance.
- No workaround: no private imports, upstream patch, parallel compiler/harness
  or unexplained conversion of arrays to strings.
- Affects: `CS-REF-18`, `CODE-UX-01` and subsequent supported-family options.

## `CS-ADR-016`. Verified artifacts and measured budgets are distinct from layout

- Date: 2026-11-19.
- Status: accepted technical execution rule; source-build assumption corrected
  by `CS-ADR-024`. Published compatible assets exist; local refresh is pending.
- Decision: keep `verifyCodingCycleToolchain()` and conditional exact-source
  CI acquisition intact. Consumers may reuse the matching published package;
  a schema-1 local manifest or successful prototype is not official execution
  evidence. Preserve old generated assets before an owned refresh; do not
  overwrite an unowned release or manufacture a schema-2 manifest.
- Evidence: the actual local manifest fails preflight. GitHub's read-only
  exact-name query for HEAD `723eeeb84589cfb23b0def630ee46ead02890acc`,
  tree `c1236b3e35c0da3eed4e79135eb6b7090b91863f`, returned zero artifacts.
  No artifact was downloaded. Fixture contract tests and all 26 lightweight
  preflight tests pass independently of that missing runtime evidence.
- Subsequent evidence, 2026-11-20: published 4.5.2 manifests pass the frozen
  binding check. The earlier current-HEAD artifact query did not test this
  consumer path and does not justify a mandatory source build.
- Resource rule: one browser worker, one execution, sequential builds and at
  least two logical CPUs left free. Use the measured host entry criteria and
  startup/output/cleanup thresholds in `PRODUCT-MODEL.md`. Never start the
  historical 12 GiB coding-cycle build on the currently pressured host merely
  to discover the same missing artifact later in boot.
- Affects: `CS-REF-10` through `12`, `21`, all real-runtime gates and part 4.

## `CS-ADR-017`. Meta-Harness governs all refoundation slices

- Date: 2026-11-19.
- Status: accepted by explicit product instruction.
- Decision: every atomic slice follows Specification -> Contract/schema ->
  Test -> Minimal implementation -> Quality gate. Adopt the invariant/import
  graph, negative constraints, tiered context, structural Stop & Ask and
  Builder/Auditor passes in
  [`GOVERNANCE.md`](./06-refoundation/GOVERNANCE.md).
- Terminal: use [`QUALITY-GATES.md`](./06-refoundation/QUALITY-GATES.md), with
  strict checks, actual selected cases and exit 0. Missing tests, required skips
  and relaxed assertions cannot pass. Runtime and human acceptance are separate.
- Memory: keep `IMPLEMENTATION-STATE.md` as the single required state ledger;
  retain numbered ADRs here. Update compact active instructions after each
  approved slice, without deleting historical evidence or unresolved failures.
- Consequence: plans 3.6.1 through 3.6.6 use explicit atomic sequences. New test
  infrastructure remains a planned deliverable until implemented and executed.
- Affects: all `CODE-UX-*` gates and UXP prototype slices; no production change.

## `CS-ADR-018`. Resolve prototype workflow gaps before design approval

- Date: 2026-11-19.
- Status: remediation implemented; design subsequently accepted on 2026-11-20
  (`CS-ADR-022`). Historical findings below explain the iteration.
- Evidence: critical review identified UX-F01..06. The prototype's preview
  ignores edited problem/starter; typed authoring and failure/validation/
  recovery states are missing; realistic host constraints require another
  layout iteration. Historical navigation/overflow checks remain limited.
- Decision: perform
  [`3.6.1a`](./06-refoundation/01a-prototype-workflows-and-critical-review.md)
  inside the current CODE-UX-01 gate, with typed synthetic states, independent
  behavioral oracles, adversarial audit and human design review. Complete
  teacher and learner journeys before approving the implementation blueprint.
- Boundary: no real runtime/API/database or official grading in this prototype.
  Simulated controls are allowed for design inspection and labeled as such.
  Product contract implementation remains in CODE-UX-02 onward.
- Dependency: design/contract work does not require runtime boot. The user
  separately accepted initial scalar scope in CS-ADR-015; actual local asset
  readiness remains required before CODE-UX-03 execution under CS-ADR-024.
- Affects: UX-F01..06, existing CS-REF acceptance mappings and current handoff.

## `CS-ADR-019`. Name authoring validation explicitly

- Date: 2026-11-19.
- Status: accepted by explicit product instruction.
- Decision: use **Validate exercise** for the authoring tab, heading and task
  references; localized labels must also name the exercise explicitly.
- Boundary: this checks exercise configuration and reference/incorrect
  solutions. Assessments/Grading separately evaluates learner submissions,
  assigns official scores and controls review/release. No academic effects
  belong to the authoring validation task.
- Affects: prototype copy and refoundation terminology only; existing internal
  IDs and contracts are unchanged. This does not complete any UXP slice.

## `CS-ADR-020`. Disposable, typed interactive design specimen

- Date: 2026-11-20.
- Status: accepted execution choice within UXP-01; not product acceptance.
- Decision: keep the HTML entry directly openable with classic browser scripts,
  checked using strict TypeScript/JSDoc and separate presentation types. Node
  tests inspect the same scripts; Playwright owns one browser, sequential pages
  and cleanup. No bundler, new dependency, application server or human CLI.
- Fixtures: incomplete drafts, stable identities and declared synthetic outcomes
  model interactions only. No source interpreter, compiler, academic scoring or
  actual persistence command. Unknown fixture inputs cannot invent a verdict.
- Isolation: preview receives an explicit redacted projection and independent
  scratch/files. Teacher data in this public design specimen is synthetic;
  this is not an authentication or production secrecy proof.
- Acceptance: UXP terminal evidence, human design acceptance, artifact provenance
  and compound-value scope remain separate obligations. Production never imports
  these disposable scripts. CS-ADR-019's Validate exercise naming is retained.
- Affects: UXP-01..06 inside CODE-UX-01 only.

## `CS-ADR-021`. Prototype audit completion is distinct from product acceptance

- Date: 2026-11-20.
- Status: accepted technical handoff; human design and scalar-scope acceptance
  subsequently recorded in `CS-ADR-015/022`.
- Decision: UXP-01..06 now have an implemented interactive specimen, a frozen
  oracle checklist and strict terminal evidence. Preserve the five author tasks,
  typed family-specific cases, independent preview and separate learner work.
  The production implementation still follows 3.6.2..3.6.6 after gate closure.
- Evidence: 11 contract tests and 13 browser journeys, no skips; inspected
  1600x1000, 1280x720 and 390x844 captures. Same-agent Auditor fixed typed-command,
  capability, focus, long-case layout, pending-signature and scratch/file-version
  defects without relaxing checks. This is not an independent human review.
- Simulation boundary: examples have declared recorded outcomes; arbitrary code
  is not evaluated. Save/recovery is scoped per-tab memory with a simulation
  label, never actual server durability. Preview submits no academic work.
  Canvas pixels and terminal echo are presentation specimens only.
- Current obligations: baseline documentation/contract closure and subsequent
  real runtime/production acceptance. `CS-ADR-024` corrects the former universal
  exact-source artifact requirement; local synchronized assets still need
  actual preflight in CODE-UX-03. No upstream/platform, dependency or database
  change is authorized here.

## `CS-ADR-022`. Represent the editor composition in the design specimen

- Date: 2026-11-20.
- Status: design iteration implemented; prototype design explicitly accepted
  by the user on 2026-11-20. Production usability/runtime acceptance remains open.
- Context: plain source forms did not resemble the intended Monaco-backed
  author/learner workflow, obscuring how file operations would actually fit.
- Decision: add UXP-07 within 3.6.1a. One disposable visual workbench composes
  numbered/colorized native editing, explorer, open tabs, per-file caret/scroll
  and modification indicators. Author file creation/rename checks relative
  paths and collisions; deletion is confirmed. Closing tabs never deletes data.
  Learner/preview retain supplied-file permissions; no new academic policy.
- Boundary: no actual Monaco/Emception initialization, compiler, alternate VFS,
  dependency or upstream/UI change. The same visual editor treats protected
  reference solutions separately, outside learner delivery. Unsupported file
  sets report unrecorded simulation outcomes instead of invented verdicts.
- Gate: strict contracts/lint, required behavioral oracles and captures at
  1600x1000, 1280x720 and 390x844; independent human/runtime gates remain open.
- Consequence: production may replace the native-text specimen with declared
  Monaco/public runtime integration while preserving the accepted interactions.
  Visual similarity is not API, language-service or runtime conformance.

## `CS-ADR-023`. Artifact production must not deploy the independent IDE

- Date: 2026-11-20.
- Status: safety finding retained; proposed build-only CI prerequisite
  withdrawn and superseded by `CS-ADR-024`. No workflow change or dispatch.
- Evidence: committed HEAD `40a8470b4694e4b092b270ec8160aa07f34d6739`, tree
  `5d59e6bbbbc88e092fdca19ffa43a30a23d0dbc9`, has no producer runs/artifacts.
  The real manifest check still fails. The inspected green workflow
  [44523205221](https://github.com/gameguild-gg/gameguild/actions/runs/44523205221)
  skipped the producer and uploaded no artifact.
- Correction to earlier handoff: `publish_packages: false` does not make the
  current manual workflow build-only. Its `deploy-pages` condition explicitly
  includes every `workflow_dispatch`. Using it as previously suggested could
  publish a feature-branch IDE to the shared GitHub Pages environment.
- Boundary: do not dispatch that path, replace an unowned local release,
  fabricate provenance, accept unrelated binaries or relax frozen identity.
- Planning correction: the absence of a producer result for a consumer commit
  was not evidence that a new toolchain or a CI change was required. Compatible
  4.5.2 manifests are published. The old proposal is not an open owner decision
  or a blocker for CODE-UX-01/02; use the consumer path in `CS-ADR-024`.
- Acceptance retained: verified local frozen identity before real execution,
  actual runtime conformance and no unintended deployment. A manifest check,
  workflow dispatch or overall green status alone is not runtime acceptance.
- Affects: artifact preparation before CODE-UX-03 and CS-REF-21; no platform,
  runtime/IDE, CI, schema or migration changes are required by this correction.

## `CS-ADR-024`. Consume published runtime assets without rebuilding the IDE

- Date: 2026-11-20.
- Status: accepted by explicit user instruction to correct the plan.
- Invariant: coding-surface owns its web UI and consumes public Emception APIs.
  It neither publishes the independent IDE nor changes its source, consumers
  or release workflow to validate our product.
- Evidence: npm metadata returns 4.5.2 for both `emception` and
  `@gameguild/emception-toolchain`. The
  [Toolchain manifest](https://cdn.jsdelivr.net/npm/@gameguild/emception-toolchain@4.5.2/cdn/manifest.json)
  and [Core CDN manifest](https://cdn.jsdelivr.net/npm/emception@4.5.2/cdn/manifest.json)
  both pass `requireFrozenCodeToolchain(CODE_TOOLCHAIN_V3, manifest)`.
  Local canonical/web manifests remain schema 1 and fail. No full artifact
  download, local synchronization, compiler build or runtime execution occurred.
- Decision: reuse the pinned published assets. Before real execution, preserve
  old generated copies, synchronize using the existing host path and verify
  actual local source/served manifests. Do not accept a stale legacy copy or
  the helper's 4.4.0 fallback for the current grading binding. `dev:fast` only
  syncs missing assets; the sync helper can retain schema 1. An explicit,
  backed-up refresh is needed, not just a dev restart.
- CI distinction: `needs_source_release()` applies exact HEAD/tree provenance
  only to its classified source changes. Preserve this rule and inspect all
  changed paths, including dependency manifests/lockfiles. Feature-package or
  documentation changes do not universally require a producer run. A local
  pinned consumer manifest check does not replace required CI provenance.
- Sequence: CODE-UX-01 owns product/design/contract preparation and lightweight
  tests; CODE-UX-02 owns domain/draft behavior without compiler execution.
  CODE-UX-03 owns actual asset readiness/G-PREFLIGHT before real runtime tests.
  CS-REF-21 remains required there; CODE-UI-07 still needs official browser CI.
  No unexecuted gate is marked complete by this documentation correction.
- Fallback only: a local source build may be used if compatible assets cannot
  be obtained or a separately approved runtime change requires it. Reuse the
  existing build/verification path under measured CPU/memory limits; it is not
  required for the published 4.5.2 assets and must not run on every dev startup.
- Negative constraints: no hand-written manifest/provenance, relaxed binding,
  silent version fallback, destructive replacement, mandatory IDE deployment,
  new release, CI rewrite or package publication. G-PREFLIGHT and real browser
  execution remain separate from the passing remote manifest observation.
- Supersedes: the mandatory build-only producer proposal in CS-ADR-023 and the
  unconditional artifact/preflight ordering in CS-ADR-007/013/016/021.
- Affects: UX01-C, UX03-A, CS-REF-21 and part 4 artifact terminology/handoff.

## `CS-ADR-025`. Incomplete authoring is a separate, non-executable document

- Date: 2026-11-21.
- Status: accepted implementation detail of CS-ADR-014; implemented and audited
  in scope. UX02-A's broader G-API gate remains open (external test/resource stop).
- Contract: `coding-authoring-draft`, not a falsely typed
  `CodingAuthoringDefinition`. Problem, one function signature, starter/support
  files, stable test/case IDs, explicit language/runtime selection, frozen
  toolchain identity and isolated reference/incorrect solutions belong to
  this author-only JSON. Numeric/boolean edits remain text buffers while
  incomplete. A missing I/O expectation is `null`; empty stdout is `""`.
- Boundary: parsers enforce bounded shape/IDs and reject injected academic or
  validation-result fields. Incomplete semantic values may be saved, but the
  document cannot pass existing definition/delivery/official adapter parsers.
  Selecting a runtime does not assert that the combination can execute.
- Changes: family/signature/environment/toolchain changes require an impact
  plan and explicit confirmation. Cancel returns a cloned current draft;
  confirmation of a stale baseline fails. Compatible parameter values match
  unique names/types, not guessed positions or ambiguous names. Duplication
  takes fresh IDs from the host, never random IDs or browser state in coding.
- Persistence: reuse the existing Learning JSON draft/revision boundary;
  tests use the same synthetic JSON fixtures in TS and the actual EF-backed
  authoring service. No new server store, API DTO, platform module, migration,
  generated client or new writer is introduced in this slice. Production
  persistence ports/recovery and immutable publication are UX02-B/C work.
- Preservation: canonical definitions, active host writers, retained snapshots
  and historical migrations are unchanged. No automatic data conversion is
  run. Explicit import rejects mixed families or conflicting signatures rather
  than discarding existing tests. Published-data counts remain unknown; an
  authorized inventory/disposition is still required before a breaking cutover.
- Rejected: making canonical parsers permissive, treating private starter files
  as reference solutions, embedding validation/grade state in the draft, inferring
  environment from a preset/file name, and claiming frontend-only persistence.
- Affects: UX02-A; CS-REF-06/07/09/22 contract preparation. It does not close
  their production acceptance or CODE-UX-02 by itself.

## `CS-ADR-026`. Revision-bound Learning persistence and author-only recovery

- Date: 2026-11-21.
- Status: implemented/tested UX02-B preparation inside CS-ADR-014/025 boundaries;
  broader G-API and production integration remain open.
- Persistence: coding-surface consumes a typed port. The web host uses existing
  authorized Learning get/save actions and JSON/revision storage; token/tenant
  come from the authenticated server context, not the local isolation scope.
  No endpoint, server store, platform rule, table or migration is added.
- Concurrency: one in-flight request freezes the draft being sent. Saved needs
  its exact next revision and body acknowledgement. Newer edits stay unsaved;
  failures retain them. A conflict cannot retry automatically even after a
  failed reload. Explicit discard/reload cannot erase edits made in flight.
- Metadata: the host supplies an atomic revision/payload pair. A newer coding
  reload cannot authorize a write with an older title, settings or metadata.
  The lesson coordinator must adopt the whole acknowledged/reloaded lesson
  and remain the single save loop; the new port is not a second autosave.
  This integration is required at the planned UI cutover, not claimed done now.
- Recovery: private reference/case buffers stay in the authorized author session.
  Checkpoints are cloned memory values, bound to tenant/actor/course/content
  and draft/publication revisions. Cross-scope/stale recovery is rejected.
  Dispose or changed scope prevents a late response from reviving old state.
  Acknowledged work recovers from the server on another device/refresh;
  unsynced memory does not survive page closure/refresh and is never called Saved.
  No private draft is put in browser/learner storage or error logs.
- Failures: unauthorized, conflict, invalid-data and unavailable remain explicit.
  Invalid snapshots, cyclic/non-serializable metadata or wrong acknowledgements
  cannot turn into saved state. Raw private exceptions are not exposed/logged.
- Evidence: strict contracts before behavior; initial missing implementations
  failed the planned oracles. Same-agent audit then reproduced six concrete
  lifecycle/identity/metadata defects before their fixes. Both families share
  the backend fixture. Terminal details are in the single implementation ledger.
- Preservation: active editor/writers, canonical definitions, official grading,
  published data, generated client, dependencies and Emception/UI stay unchanged.
  No compiler/build/publication prerequisite is introduced. Actual publication/
  projections are UX02-C; human/runtime acceptance remains separate.

## `CS-ADR-027`. Additive, revision-bound coding publication metadata

- Date: 2026-11-21.
- Status: metadata, explicit draft compiler and immutable-revision tests
  implemented/audited in scope. Full configuration/wire mapping, official
  client generation and broader gates remain open. No production UI cutover.
- Inventory: current local database has two coding contents without Problem,
  no coding drafts, revisions or submissions. Read-only counts and the entry
  commit are recorded in the single ledger; remote usage remains unknown.
- Decision: extend existing coding JSON with optional problem, private
  validation solutions and explicit file/test/case IDs. Absent fields stay
  absent on serialization. Do not rewrite historical data, invent its missing
  statement, renumber its tests or add another adapter/version/storage model.
  A complete new draft compiles explicitly; saving an incomplete draft is
  not publication. Existing revision infrastructure freezes the entire source.
- Privacy: learner projection is an explicit allowlist; reference/incorrect
  solutions are author-only replacements, not private starter/support files.
  They are excluded from learner delivery and official learner execution.
  No new browser storage, logs, asset URLs or academic policy.
- Proof: shared synthetic contract fixtures, historical serialization,
  incomplete-value rejection, stable identity, nested private canaries and
  old-revision delivery/submission tests before consumer cutover.
- Schema disposition: existing JSON columns and immutable revisions suffice;
  no relational change or migration is justified. Limits/runtime readiness
  and teaching UX are not established by these publication metadata tests.
- Affects: UX02-C, CS-REF-06/07/09/22; preserved Emception/UI and all platform
  modules remain read-only.

## `CS-ADR-028`. Scoped grading-worker budget enforcement

- Date: 2026-11-21.
- Status: targeted worker exception explicitly approved by the user. The
  separate memory-limit decision is now resolved by CS-ADR-029.
  Nine guard and four composition cases pass; the complete worker gate still
  has two pre-existing historical-identity assertions. No real runtime claim.
- Boundary: only `tools/emception/grading/browser.ts`, its execution-budget
  helper/tests and the worker build test selection may change. IDE,
  `@game-guild/emception-ui`, public Emception implementations and compiler
  harnesses remain preserved. Continue to consume their public APIs.
- Observation: browser `run()` does not forward the public `timeoutMs` and
  reports `timedOut: false`. There is no public per-program memory control.
  Setting `TestPlan.timeoutMsPerCase` is therefore not proof of cancellation.
- Decision: guard each grading compile-and-run with a deadline and combined
  UTF-8 stdout/stderr byte budget; dispose only that owned runtime on failure.
  Failures must settle the official request even if overlay cleanup cannot
  complete on the terminated runtime. Budget failures remain infrastructure
  failures, distinct from a student assertion failure or a zero grade.
- Sequence: prove the guard independently before published configuration
  wiring. Preserve existing defaults and data interpretation in this first
  slice. No claimed memory enforcement, new executor, adapter version,
  dependency or change to academic grading policy.
- Consequence: worker source may activate existing source-release provenance
  requirements. Check the official classifier; do not waive it or publish the
  standalone IDE as part of this domain phase. Real execution remains UX03.

## `CS-ADR-029`. Defer exercise memory control, independently budget validation

- Date: 2026-11-21.
- Status: explicitly accepted by the user. This is a scope/resource decision,
  not completed WB-B implementation or runtime proof.
- Product scope: configurable RAM limits for student programs are deferred in
  this delivery. The authoring capability must be explicitly unavailable;
  no field/control may claim enforcement through an unsupported runtime API.
  Time/output requirements may proceed through the supported grading boundary.
- Preservation: keep persisted drafts, immutable snapshots and historical
  values intact. WB-B must explicitly model an unavailable capability and
  handle any existing configured value without silently dropping/ignoring it.
  Do not fabricate a memory setting, academic penalty or successful validation.
- Boundary: no public Emception implementation, IDE, UI or compiler change.
  The previously authorized grading-worker exception remains narrowly scoped.
  Future per-program memory support needs separate planning with its owner.
- Independent validation budget: the user also permits up to 10 GB RAM for
  local builds/tests, not student execution. The ledger applies a conservative
  10,000,000,000-byte owned-RSS ceiling, preserves system headroom and reserves
  two CPUs. No global dev setting or upstream runtime limit changes.
- Consequence: the memory decision no longer blocks WB-B. Implement the
  contract/schema/test-first publication mapping and required gates next;
  no remaining acceptance or build result is inferred from this approval.

## `CS-ADR-030`. Explicit, frozen publication execution configuration

- Date: 2026-11-21.
- Status: WB-B contract/wire/backend implementation has passing focused and
  complete affected suites. Official execution fidelity is still blocked;
  this decision is not runtime acceptance or CODE-UX-02 closure.
- Decision: add optional `Execution` to the existing coding JSON with entry
  point, limits and exact toolchain. Add optional `Environment.Runtime`; new
  execution configuration requires it and coherent language/tools/library.
  Explicit configuration is validated, frozen and projected unchanged, never
  reconstructed from the current runtime or a guessed source file. Retained
  JSON without these fields keeps its existing behavior and is not rewritten.
- RAM remains unavailable under CS-ADR-029. Keep retained values recoverable;
  reject new publication with a configured RAM value. Empty draft RAM is
  absence, not zero or a fabricated 128 MB enforcement claim. Time/output
  configuration stays within the existing worker maxima of 30 s / 2 MB.
- Ownership: coding defines technical contracts; Courses persists existing JSON;
  CodeAdapter freezes and validates official execution; the web bridge maps
  DTOs; the authorized grading worker consumes the exact configuration through
  public Emception APIs. Academic scoring/release and the IDE remain unchanged.
- Schema disposition: additive JSON fields need no relational migration. Keep
  historical migrations, revisions, snapshots and files intact. Known legacy
  defaults are only for genuinely absent legacy configuration, never malformed
  or unsupported explicit data. No V2 protocol or second persistence system.
- Proof required: independent TS/backend and worker negatives, both-family
  roundtrip/revision/privacy tests, current official generated client and the
  applicable strict gates. CODE-UX-03 still owns actual compiler conformance.

## `CS-ADR-031`. Historical worker identity oracle is not the current adapter

- Date: 2026-11-22.
- Status: accepted maintenance of the documented historical-worker gate.
- Evidence: the complete worker selection passes 76/78; only the assertions
  demanding retired 4.4.0/4.5.1 literals in the current adapter fail. The
  retained lock hashes and all manifest/tamper/cross-version negatives pass.
- Decision: verify every historical identity against its retained lock and
  manifest binding, and verify only the current identity against the current
  adapter. Historical tests must explicitly reject rebinding to the current
  release. Preserve all fixtures, exact hashes and tamper negatives.
- No runtime, adapter registration, compiler identity or compatibility policy
  change is authorized by this oracle correction. An old executable remains
  unavailable unless its own verified artifacts exist; never substitute new
  compiler bytes or invent a historical adapter to satisfy a source-text test.

## `CS-ADR-032`. Keep feature implementations outside the worker artifact closure

- Date: 2026-11-22.
- Status: implemented and tested; no change to native execution scope.
- Finding: reusing coding's runtime parser in the trusted grading bundle would
  make feature-only source edits alter that bundle without activating the
  existing Emception exact-source classifier. Do not change CI to hide this
  introduced coupling or turn a feature package into an upstream dependency.
- Decision: retain a small typed, independently tested transport guard in the
  existing product-specific worker. Reuse its budget validation and exact
  frozen-binding constants. Mirror the supported wire rules/path normalization
  deliberately, record duplication and keep new unknown data fail-closed.
- Domain/web continue using their declared Zod parser. No new library,
  package/lockfile edit, speculative adapter version, public runtime/IDE edit
  or source-release policy relaxation. An import-boundary regression test
  prevents feature implementation imports from returning to the worker.
- Evidence: strict worker typecheck passed before behavior. The new AST import
  oracle failed against the feature import while 45 transport/composition cases
  passed. After removing the dependency, all 46 composition/import cases and
  the full 104-case worker selection pass, as does strict worker typecheck.
  This is mocked transport/composition evidence, not native conformance.

## `CS-ADR-033`. Lazy owned runtime acquisition, not a new executor

- Date: 2026-11-22. Boundary: independent UX03-A preparation after the develop
  merge; CODE-UX-02/03 remain open.
- Decision: expose a context-owned typed runtime session. Construction and
  inspection do no dynamic import, manifest fetch or worker boot. Explicit
  acquisition checks the current frozen identity through the public manifest
  parser and owned metadata comparison before calling public createEmception.
  Share pending acquisition only within this session; retry only on an explicit
  caller request. Disposal aborts pending metadata, rejects acquisition promptly
  and closes any late-created canvas/runtime, never a new context's instance.
- Boundaries: no app, academic command, compiler, harness, browser storage,
  protected Emception implementation or emception-ui dependency. The port is
  not wired into the old production workbench. The local release/served assets
  were refreshed using the existing sync helper after verified backups; archive
  SRI and all 29 bundle hashes pass. No local compiler build or IDE deployment.
- Proof limits: metadata preflight is not a cryptographic provenance claim for
  a later mutable deployment. The host/CI must serve the exact immutable release
  assets and retain normal source-producer checks. Isolated real-browser proof
  covers lazy boot, single acquisition, rejection and disposal only, not program
  compilation, functional cases, graphics, five-cycle resource plateau or human
  acceptance. The later CS-ADR-034 answers the two scope questions; remaining
  runtime findings and broader acceptance stay open.
- Evidence: strict contract before behavior; meaningful RED against the typed
  unimplemented acquisition, unit/architecture gates and a boot-only Chromium
  diagnostic with current public sources and actual published assets. See the
  dated ledger for exact counts, resource measurements and ignored reports.
- Duplication: the small current-identity metadata check intentionally mirrors
  the worker guard without importing that separately fingerprinted artifact;
  register it in DUPLICATION-REGISTER.md. Future convergence requires its owners.

## `CS-ADR-034`. Explicit execution phase and initial source file

- Date: 2026-11-22. Status: both choices explicitly authorized by the user;
  scoped implementation and real-worker proof recorded. Broader gates remain
  open; terminal evidence is not inferred human acceptance.
- Public exception: add optional compile/link/run phase metadata to ToolResult,
  populate it in the browser facade, validate it and let the existing public
  test engine distinguish build failure from program assertions. Guard phase
  presence at the official grading boundary. No IDE/UI, compiler/harness,
  library/version, new executor or academic-policy change is authorized.
- Compatibility: old raw-tool/other-provider results without phase remain
  assignable with their existing generic test semantics. Browser results now
  expose the actual terminal phase; official grading must not guess it from
  exit status or diagnostics. Existing fields, expected nonzero program exits,
  saved definitions and historical receipts are not rewritten.
- Entry decision: entryPoint.path identifies the initial editor source, not
  the translation unit required to contain main. Preserve serialized names,
  public text-source validation, all source units and current link ordering.
  Label it Initial file in the owned surface. A future teacher main-location
  constraint would need its own explicit pedagogical contract.
- Scope: current core types/result validator/testing engine and focused tests,
  browser facade/tests, grading entry guard/tests, owned labels and documents.
  CS-ADR-028/032 budget/import boundaries still apply. No unrestricted upstream
  modification or change to existing exact-source CI policy is implied.
- Required proof: contract compilation and old-shape assignability before
  behavior; independent RED; compiler/linker failure cannot pass empty-output
  or matching-exit assertions; completed nonzero runs retain semantics; missing
  phase is unavailable; real multi-file controls pass. Function ABI/graphics
  findings remain independent and cannot be promoted by this correction.
- Evidence: contract/types before independent behavioral RED; complete core,
  browser, worker and owned surface regressions; 12/12 current-source official
  worker controls with real pinned WASM assets, including main in another file
  and build failures matching otherwise successful assertions. The ledger
  retains exact hashes/resources, the first failed probes and proof limits.
  A permitted undefined-import fixture is not a linker failure and remains a
  separately recorded risk. Seven unchanged upstream lint warnings are not
  waived; no whole-phase, release, canonical or human acceptance is claimed.
- Follow-up validation: paired real-IDE controls preserve editing/tabs, shell,
  native stdin/error recovery and SDL3 pixels/focus/Stop. Public engine behavior
  changes only as intended in its invalid-build control. A separate shell-to-
  program input defect fails in both versions and remains explicit. Full
  rationale, compatibility qualifications and evidence are in
  [EMCEPTION-PHASE-EXTENSION-RATIONALE.md](./EMCEPTION-PHASE-EXTENSION-RATIONALE.md).

## `CS-ADR-035`. Local capability eligibility is not runtime or academic success

- Date: 2026-11-22. Status: accepted within the existing UX03-A ownership;
  independent preparation, not production UI integration or a new upstream grant.
- Decision: a pure coding-surface resolver consumes the explicit exercise
  family, environment/frozen identity, host-authorized mode and existing
  learner-file policy and host read-only restriction. It returns
  available/unavailable local operations and reasons, plus a graphics-target
  requirement independent of panel visibility.
  It receives no private cases, references, credentials or academic commands.
- Boundaries: authoring edits start no runtime. Program runs, technical tests
  and interactive input are separate. Only learner-public scope is offered
  in learner mode; reviewer execution remains read-only diagnostics. The host
  still owns authorization and execution must still preflight the artifact.
- Evidence scope: current pinned WASI C/C++ program/batch controls and SDL3
  canvas controls support their corresponding diagnostic paths. Function ABI,
  graphics case execution and unverified environment/input combinations stay
  unavailable with a concrete reason; no profile boolean or selected preset
  is treated as conformance. This is a temporary technical status, not silent
  removal of a promised exercise family or a change to grading policy.
- Consequences: minimizing a required canvas does not remove its execution
  target. Execution availability is distinct from worker readiness and a
  successful program/result. Missing upstream capability follows the existing
  EMCEPTION-GATE. The separate upstream-harness proposal was subsequently
  superseded by the approved owned composition in CS-ADR-036.

## `CS-ADR-036`. Owned native function support files, not an upstream harness patch

- Date: 2026-11-23. Status: approach explicitly approved by the user;
  feasibility and implementation gates remain separate.
- Context: a missing startup can be supplied by composition. C++ additionally
  needs a bridge between an ordinary function and the existing harness's C
  linkage. Native C cannot compile that C++ harness merely by changing its
  filename; it needs a C-compatible call/report template.
- Decision: first prove both paths with positive/negative real-artifact
  controls. Keep Emception's public C++ generator and generic test engine;
  supply temporary ABI/startup files. A native C call/report adapter may emit
  the report consumed by that same engine. No alternative compiler, judge,
  score/release policy, private upstream import or C-as-C++ substitution.
- Ownership: pure technical source composition belongs to coding; the surface
  orchestrates local public cases, the trusted worker composes official cases.
  Both must consume the same owned source generator. No React dependency or
  client-generated executable payload is trusted by official grading.
- Artifact boundary: only the exact pure source-composition module may cross
  the worker's prior feature-import restriction, with a negative dependency
  test. The existing repository-policy job already rebuilds the grading worker
  from the checkout; preserve that verification. SDK/toolchain acquisition is
  separate and its classifier/provenance rules remain unchanged. Do not require
  an upstream SDK publication merely because an owned template changes.
- Preservation: no public Emception core/browser/harness/IDE/UI, preset or
  compiler flag change. Supplied startup must be checked against retained
  main-containing solutions. Never rewrite published revisions or submissions.
  Technical files hidden from editor navigation are not secrets; private
  cases remain server-only. Register any necessary template duplication.
- Supersedes: the pending proposal to extend the upstream generator for this
  finding, and the blanket prohibition on owned function call/report support
  only within this narrow scope. Compound values, new languages, graphics and
  exercise memory remain outside this decision.
- Implementation contract: the pure composer validates scalar representation,
  generates support outside /home/user, and accepts a C++ public-harness factory
  instead of importing a runtime. C uses int/double/_Bool/const-char-pointer
  signatures; C++ retains int/double/bool/std::string. Weak target declarations
  reject absent functions even when zero is expected. Weak startup preserves
  a separately linked existing main. Inputs and submitted files are not rewritten.
- Preservation of history: the configured official path regenerates the
  support; definitions without the explicit execution configuration retain
  their original mapper. No snapshot is rewritten and no new contract version
  or alias is introduced. Result-group ordering/weights/boolean receipts and
  existing runtime/output budgets are retained.
- Evidence: initial twelve real WASM feasibility controls pass. Strict composer
  contracts precede 33/33 independent RED and 130 passing coding tests; the
  preview/worker have independent failure tests and current-source worker build.
  These do not replace the pending real production-mapper/scalar/receipt proof.

## `CS-ADR-037`. Student time and preparation time are separate budgets

- Date: 2026-11-23. Status: business rule and the narrow optional facade
  callback explicitly authorized by the user; scoped implementation, contracts
  and real scalar/budget proofs pass. Overall phase acceptance remains separate.
- Evidence: two C++ string groups exceed the existing combined 30-second
  guard during linking, after successful compilation and before checking the
  answer. Private-scratch/fixed-output controls reproduce the same boundary.
  Increasing that combined timeout would not implement the requested meaning.
- Decision: apply the published exercise time limit only while the student's
  program executes. Compilation/linking must have a separate finite
  infrastructure budget, with cancellation and no unlimited preparation.
  Do not add a persisted teacher field, migration or contract version merely
  to express an infrastructure budget. Preserve historical data/receipts and
  evaluate the affected frozen worker paths explicitly.
- Integration constraint: distinguish phases by an explicit supported callback,
  not output text, number of exit events or guessed elapsed-time subtraction.
  The existing public low-level helper exposes onPhase, but the native
  multi-source facade does not forward that lifecycle. Duplicating its native
  build pipeline would violate the single-compiler boundary. An optional
  facade callback is now authorized in CompileOptions, the browser facade and
  their focused contract/compatibility tests only. No IDE/preset/harness change.
- Bounded policy: 120000 ms total preparation per compile-and-run invocation,
  not per source or phase. This gives headroom above the measured 26-second
  first group and the second group's more-than-30-second preparation without
  charging that cost to the learner. The student timer starts only at run.
  Keep the existing aggregate output byte guard and five-minute whole-worker
  watchdog; neither is relaxed. Raw tool operations retain their existing
  budget. Reject missing/regressing phase signals, expired preparation and
  inconsistent terminal metadata. No silent old-SDK fallback in this worker.
- Atomic sequence: validate additive types first; record failing independent
  phase-order/cancellation and budget tests; minimal facade forwarding and
  worker integration; strict types/lint, core/browser/UI regressions and real
  scalar/public/private receipt proof. One heavy job at a time under the
  existing OS-available-memory guard. Source-byte and temporary cleanup
  checks remain mandatory.
- Proof: additive type/assignability tests, 28 independent lifecycle/budget/
  worker RED, a further cancellation-bypass RED and 795 passing regressions.
  All ten current-preview and ten rebuilt final-worker scalar controls pass.
  Three real final controls preserve the retained mapper and distinguish a
  normally completing from an infinite C program under the same 3000-ms limit,
  without producing a grade on cancellation. Presets/harness/IDE source stay
  unchanged. The rationale and ledger retain hashes/resources, the earlier
  failed producer evidence and seven pre-existing upstream lint warnings.

## Modelo para novas decisoes

```text
## `CS-ADR-NNN`. Titulo

- Data: YYYY-MM-DD.
- Status: proposta | aceita | substituida | rejeitada.
- Contexto:
- Decisao:
- Alternativas:
- Consequencias:
- Evidencia:
- Afeta:
- Substitui ou foi substituida por: N/A.
```
