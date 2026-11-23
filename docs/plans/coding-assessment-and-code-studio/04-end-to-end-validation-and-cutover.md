# Parte 4. Validacao end-to-end e aceite do coding grading

Status: bloqueado pela refundacao `CODE-UX-01` a `CODE-UX-06`; depois,
retorna para a entrada `CODE-E2E-00`.

## Objetivo

Provar o fluxo oficial de coding assessment por contratos, API, browser e
personas reais. A parte 3 ja realizou a integracao dos consumidores de grading
com `@game-guild/coding-surface`; esta parte valida e aceita esse cutover. Ela
nao executa uma segunda migracao, nao remove o coding assignment preservado e
nao altera o Emception para acomodar o harness.

O resultado deve dizer, com evidencia reproduzivel:

- quais capacidades funcionam;
- quais capacidades possuem bug, gap ou decisao pendente;
- qual modulo possui cada falha;
- se autoria, test run, tentativa, review e release concordam entre UI e API;
- se `function` e `standard-io` usam o mesmo pipeline oficial sem fallback.

## Required basic core and subsequent stabilization

CS-ADR-041 records the user's acceptance order: by the end of part 04, prove
simple code through official grading, not only local execution. Use a scalar
function such as integer addition and a standard-I/O sum with fixed public/
private cases, known correct/wrong submissions and technical-error controls.
Real instructor/learner sessions cover authoring, publication, frozen
submission, official review, release and learner results after refresh/new
session. Independent expected scores/feedback are mandatory.

No core case can be waived as an accepted external blocker merely to announce
that grading works. Private data, authorization, submission integrity, official
results and worker ownership remain required. Preserve other accepted UX
requirements; this baseline is not permission to delete them from the catalog.
The local runtime proofs and preserved IDE do not close this acceptance.

After the core is accepted, correct/polish recorded noncritical defects before
part 05 through the [post-04 stabilization checkpoint](./04-end-to-end-validation/06-acceptance-and-develop-integration.md#post-04-stabilization-before-part-05).
Failed resources remain visibly failed/unavailable and tracked until repaired;
there is no blanket permission to change Emception or relax a test.

## Estado de entrada

- partes [`01`](./01-domain-contracts-and-typescript-adapter.md) e
  [`02`](./02-backend-code-adapter-boundary.md) concluidas;
- a fundacao funcional e a consolidacao canonica da
  [`parte 03`](./03-coding-surface-and-web-integration.md) possuem evidencias
  locais; isso nao significa que a refundacao 3.6 esteja implementada;
- a parte 3.6 refunda autoria, resolucao e execucao em seis etapas e precisa de
  aceite antes do programa abrangente desta parte;
- `CODE-UI-07` ainda depende do lane `coding` com o artifact Emception schema
  2 exato;
- `CODE-UX-01` prepares the fixture/frozen-binding contract; `CODE-UX-03`
  synchronizes compatible published assets and passes local preflight before
  real execution. No mandatory Emception build or IDE publication is required;
- a branch deve ser reconciliada com a `develop` antes da evidencia final.

O subplano `CODE-E2E-00` revalida essa preparacao apos reconciliar a branch e
fecha a evidencia CI restante. Ele nao bloqueia a aquisicao de artifact ou a
correcao de fixture necessarias a 3.6. Nenhum outro subplano desta parte pode
ser declarado concluido enquanto essa entrada estiver aberta.

Here, an exact artifact means the frozen executable version/ABI/lock identity,
not a new runtime release for every coding UI commit. Existing CI requires
exact HEAD/tree producer provenance only for its classified source changes.
Preserve that rule; published consumer assets do not substitute for source
provenance when it is required. See part 3 `CS-ADR-024`.

## Identidades canonicas congeladas

| Contrato | Valor aceito neste corte |
| --- | --- |
| assessment type | `code-assessment-type` |
| assessment type version | `1` |
| review handler | `code-wasm-review` |
| review handler version | `1` |
| submission payload schema | `code-files` |
| exercise types | `function`, `standard-io` |

Os testes devem usar somente essas identidades. Versoes inventadas, aliases e
fallbacks silenciosos nao sao aceitaveis. Identidade desconhecida deve falhar
fechada e produzir diagnostico sem tentar outro adapter ou handler.

## Principios

- testes publicos ajudam o learner, mas o servidor continua autoritativo;
- testes privados nunca chegam ao browser learner, logs ou artifacts dele;
- `AuthorTest` e `OfficialSubmission` sao execucoes independentes;
- preview local nunca cria submission, score, gradebook ou release;
- `AutomatedReview` avalia somente o que o adapter suporta;
- `InstructorReview` pode revisar um resultado anterior com auditoria;
- score e `maxScore` usam inteiros conforme os contratos de grading;
- grade e feedback so ficam learner-visible depois da politica de release;
- uma falha de infraestrutura nunca e convertida em resposta errada;
- cada persona usa contexto e sessao independentes;
- setup e cleanup usam APIs oficiais e ownership manifest, nunca escrita direta
  no banco;
- nenhuma tabela, migration, regra de autorizacao de plataforma ou fonte do
  Emception e alterada para construir o harness.

## Baseline de regressao existente

Este plano evolui, e nao substitui antecipadamente, o ciclo presente na
`develop`:

- `apps/web/scripts/coding-cycle-browser-e2e.mjs` cobre o ciclo integrado;
- `apps/web/scripts/coding-cycle-web-runtime.mjs` prepara o runtime web isolado;
- `apps/web/scripts/coding-cycle-grading-observer.mjs` observa o acknowledgement
  causal sem depender do transporte RSC concreto;
- os testes de observer, transport e web runtime protegem o harness;
- a consulta direta de `RubricScoresPayload` e somente um diagnostico
  transitorio do runner anterior, nunca um oracle de aceite.

O runner anterior permanece como regressao ate que cada assertiva tenha um case
ID e um oracle autorizado equivalente. Corrigir sua fixture canonica nao
autoriza ampliar seu papel nem preservar payloads antigos por compatibilidade.

## Artefatos de validacao

```text
docs/testing/coding-grading/
  AGENT_PROTOCOL.md
  capability-catalog.*
  capability-status.md
  product-heuristics.md
  reports/

apps/web/test-results/coding-grading-validation/<run-id>/
```

O catalogo executavel possui IDs estaveis. Reports gerados sao candidatos e nao
alteram o status canonico sem revisao humana. Tokens, cookies, private tests,
answer keys e secrets nao entram em artifacts.

Mapear os requisitos `CS-REF-*` da
[matriz da refundacao](./03-coding-surface/06-refoundation/ACCEPTANCE-MATRIX.md)
aos casos deste catalogo. Reaproveitar evidencias quando o commit e o escopo
permitirem; nao criar uma segunda fonte de status nem considerar aceito um
requisito apenas por existir no plano.

## Matriz minima

Cada fluxo aplicavel deve possuir casos para `function` e `standard-io`. Um
tipo nao pode servir como proxy para o outro.

| Eixo | Cobertura obrigatoria |
| --- | --- |
| autoria | criar, validar, salvar e reabrir definicao |
| projecao | draft privado e delivery publica redigida |
| test run | executar revisao candidata sem efeito academico |
| learner | abrir, executar public tests e submeter oficialmente |
| review | automated, instructor e composicao configurada |
| release | retido, liberado e visivel em nova sessao |
| seguranca | outsider, learner sem grant, tenant e private tests |
| infraestrutura | timeout, cancelamento e worker indisponivel |
| congelamento | mesma revisao, handler, payload e submission no regrade |

A matriz inicial usa somente toolchains ja suportadas e estaveis. Expansoes de
linguagem da parte 5 nao bloqueiam este gate.

## Sequencia obrigatoria

| Ordem | Subplano | IDs | Resultado exigido |
| --- | --- | --- | --- |
| 4.0 | [`Entrada, baseline e fechamento da parte 3`](./04-end-to-end-validation/00-entry-baseline-and-part-3-closure.md) | `CODE-E2E-00`, `CODE-UI-07` | branch reconciliada, preparacao da 3.6 revalidada e lane exato verde |
| 4.1 | [`Catalogo de capabilities e oracles`](./04-end-to-end-validation/01-capability-catalog-and-oracles.md) | `CODE-E2E-01` | casos estaveis, ownership e oracles autorizados |
| 4.2 | [`Lifecycle de cenarios`](./04-end-to-end-validation/02-scenario-lifecycle.md) | `CODE-E2E-02` | personas, checkpoints e cleanup idempotentes |
| 4.3 | [`Contratos, API e seguranca`](./04-end-to-end-validation/03-contract-api-and-security.md) | `CODE-E2E-03` | pipeline servidor e negativos comprovados |
| 4.4 | [`Browser por persona`](./04-end-to-end-validation/04-browser-personas.md) | `CODE-E2E-04` | jornada oficial comprovada nos dois tipos |
| 4.5 | [`Laboratorio web`](./04-end-to-end-validation/05-web-validation-lab.md) | `CODE-E2E-05` | preparo e inspecao humana sem CLI de coding |
| 4.6 | [`Aceite e integracao em develop`](./04-end-to-end-validation/06-acceptance-and-develop-integration.md) | `CODE-E2E-06`, `CODE-E2E-07` | catalogo revisado e PR integrado preparado |

Cada subplano depende do gate anterior. O estado real fica em
[`IMPLEMENTATION-STATE.md`](./04-end-to-end-validation/IMPLEMENTATION-STATE.md),
nao apenas em relatos da conversa.

## Protocolo de contexto persistente

Antes de executar qualquer subplano, ler:

1. este indice e o subplano ativo;
2. [`IMPLEMENTATION-STATE.md`](./04-end-to-end-validation/IMPLEMENTATION-STATE.md);
3. [`DECISIONS.md`](./04-end-to-end-validation/DECISIONS.md);
4. o estado da
   [`parte 03`](./03-coding-surface/IMPLEMENTATION-STATE.md);
5. [`DUPLICATION-REGISTER.md`](./DUPLICATION-REGISTER.md);
6. o `AGENTS.md` aplicavel;
7. `docs/testing/coding-grading/AGENT_PROTOCOL.md`, depois que existir.

Ao iniciar, interromper ou concluir uma etapa, registrar baseline, arquivos
alterados, comandos realmente executados, resultados, bloqueios e proxima acao
exata. Decisoes que mudem fronteiras, oracles, privacidade, ownership ou
sequencia entram em `DECISIONS.md` antes da implementacao.

## Laboratorio web alvo

O laboratorio deve estender o contrato generico existente
`GradingScenarioAdapter`. O adapter de code fica em:

```text
apps/web/src/lib/grading-scenarios/adapters/code/
```

e e registrado por `apps/web/src/lib/grading-scenarios/registry.server.ts`.
A superficie humana permanece em
`/workspace/learning/grading-validation`. Nao criar rota paralela em
`/workspace/testing-lab` e nao criar CLI especifica de coding. Quiz e code
contribuem adapters independentes ao mesmo lifecycle generico.

## Artifact exato e CI

O fluxo browser oficial exige o manifest Emception schema 2 produzido pela lane
oficial. Manifest local schema 1, fixture fabricada ou relaxamento da
verificacao nao fecha `CODE-UI-07` nem esta parte.

O workflow `PR Verify` pode ser executado por `workflow_dispatch`; um draft PR
tambem pode produzir a evidencia antes do PR final. Isso nao transforma a
execucao intermediaria no PR de integracao das partes 01 a 04.

## Comandos alvo

O repositorio deve consolidar:

```bash
pnpm coding:validation:contracts
pnpm coding:validation:smoke
pnpm coding:validation:full
```

Esses comandos orquestram suites existentes; nao duplicam regras de produto.
O browser pesado roda com um worker. Builds backend usam concorrencia limitada,
por exemplo `-m:2`, para preservar recursos da maquina.

No minimo, o full deve cobrir:

- `@game-guild/coding`;
- `@game-guild/grading-adapter-code`;
- `@game-guild/coding-surface`;
- `GameGuild.Learning.Assessments.CodeAdapter`;
- `GameGuild.Learning.Assessments` e `GameGuild.Learning.Courses` afetados;
- integracao PostgreSQL aplicavel;
- observer, transport, runtime web e ciclo browser;
- suites de arquitetura/autorizacao exigidas pelo `AGENTS.md`;
- build backend warning-clean e build web.

## Separacao entre achado e correcao

O programa de validacao pode corrigir harness, fixtures, catalogo e
instrumentacao. Um achado que exija mudar regra de produto, schema, migration,
autorizacao de plataforma, Emception ou escopo de linguagem deve ser registrado
com owner e reproducer, e tratado em trabalho separado. Nao remodelar o produto
silenciosamente para fazer o teste passar.

## Criterios de conclusao

- `CODE-UI-07` possui evidencia verde com artifact schema 2 exato;
- `function` e `standard-io` passam pela identidade canonica congelada;
- author -> publish -> learner -> review -> release -> learner esta comprovado;
- author test nao gera efeitos academicos;
- automated e instructor review possuem evidencia e auditoria;
- privacidade, tenant e autorizacao possuem casos negativos;
- refresh e nova sessao learner observam somente resultado liberado;
- erro tecnico permanece erro tecnico;
- versoes congeladas continuam executaveis e identidades desconhecidas falham
  fechadas;
- nenhum aceite depende de leitura direta do banco;
- `@game-guild/emception-ui`, `tools/emception/**` e consumidores fora do novo
  grading permanecem preservados;
- nenhum schema, migration ou modulo de plataforma e alterado pelo harness;
- validacao humana e automatica exercitam o mesmo lifecycle;
- o diff final nao inclui expansao de linguagem nem refundacao do Code Studio;
- a branch final reconciliada com `develop` passa pelos gates aplicaveis antes
  do PR integrado das partes 01 a 04.
- mandatory basic-core cases have actual official score/release/learner
  evidence, not only local or mocked results; carry genuine noncritical debt
  to the owner-reviewed checkpoint before part 05, without hiding failures.
