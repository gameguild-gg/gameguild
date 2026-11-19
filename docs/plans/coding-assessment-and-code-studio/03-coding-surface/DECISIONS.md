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
  congelada correspondente ao commit testado.
- Decisao: o lane `coding` adquire o artifact Toolchain exato pelo helper
  oficial, valida sua identidade e somente depois monta o runtime e executa o
  browser E2E. A verificacao local e do CI permanece fail-closed.
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
- Decisao: `CODE-UX-01` prepara fixture, artifact exato e preflight com os
  mecanismos existentes. Os testes reais dos subplanos usam essa preparacao;
  4.0 revalida na branch reconciliada e fecha a evidencia oficial de `CODE-UI-07`.
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
- Status: proposal; no scope reduction has been approved.
- Evidence: the installed public `emception/testing` entry point successfully
  generates an integer function harness and rejects Array parameters, returns
  and nested array values. The source and official Courses validator retain a
  scalar-only contract. Generic custom test callbacks are not a typed compound
  function transport provided by the runtime.
- Proposal: continue the initial function/I/O experience with the four
  currently supported scalar kinds, record `EMC-GAP-001` with the Emception
  owner, and add arrays/matrices only through a coherent public harness and
  official executor contract. Product acceptance of that temporary limit is
  pending; otherwise the function scope remains blocked on upstream work.
- No workaround: no private imports, upstream patch, parallel compiler/harness
  or unexplained conversion of arrays to strings.
- Affects: `CS-REF-18`, `CODE-UX-01` and subsequent supported-family options.

## `CS-ADR-016`. Verified artifacts and measured budgets are distinct from layout

- Date: 2026-11-19.
- Status: accepted technical execution rule; artifact availability is blocked.
- Decision: keep `verifyCodingCycleToolchain()` and the exact-source producer/
  acquisition mechanism intact. A schema-1 local manifest or successful
  prototype is not official execution evidence. Preserve the existing release
  directory; do not overwrite it or manufacture a schema-2 manifest.
- Evidence: the actual local manifest fails preflight. GitHub's read-only
  exact-name query for HEAD `723eeeb84589cfb23b0def630ee46ead02890acc`,
  tree `c1236b3e35c0da3eed4e79135eb6b7090b91863f`, returned zero artifacts.
  No artifact was downloaded. Fixture contract tests and all 26 lightweight
  preflight tests pass independently of that missing runtime evidence.
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
- Status: remediation plan accepted; design and implementation still pending.
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
- Dependency: this iteration can proceed while artifacts/scope are pending;
  it cannot waive CS-REF-18/21 or imply approval of CS-ADR-015's scalar limit.
- Affects: UX-F01..06, existing CS-REF acceptance mappings and current handoff.

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
