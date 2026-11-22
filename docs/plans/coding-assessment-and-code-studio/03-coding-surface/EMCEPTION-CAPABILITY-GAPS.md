# Emception capability gaps for coding surface

Status: public compound-value limitation confirmed in `CODE-UX-01`; initial
scalar-only scope explicitly accepted by the user on 2026-11-20. Compound
support remains deferred. The upstream runtime and UI remain unchanged;
CS-ADR-028 records the separately approved grading-worker safety exception.
Per-exercise memory control remains unsupported; the user explicitly approved
deferring that capability in this delivery (CS-ADR-029).

Este arquivo registra somente necessidades que nao podem ser atendidas pelos
entrypoints publicos do Emception instalado. Preferencia de API, conveniencia ou
codigo existente em um internal nao provam um gap.

Encontrar um gap nao autoriza editar Emception, `@game-guild/emception-ui`, usar
import `src/**` ou criar alias privado. O executor deve aplicar o
`EMCEPTION-GATE` do indice da parte 3.

## Registro

O baseline anterior cobria editor, VFS e testes escalares. Ele nao comprova
todos os recursos da refundacao. `CODE-UX-01` verifica novamente a revisao e os
exports instalados antes de afirmar suporte a uma combinacao.

| ID | Capability necessaria | Caso de uso | Consumidor | Evidencia nos exports publicos | Fallback | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| N/A | nenhuma confirmada | editor, VFS e test runner | coding-surface | `@gameguild/emception-browser` fornece `createEmception()` headless; `emception/testing` fornece planos e resultados; Monaco e a camada de edicao do host | N/A | rejeitado |
| `EMC-GAP-001` | argumentos/retornos funcionais compostos | exercicios com arrays e matrizes | coding e coding-surface; executor oficial | installed public harness rejects compound values; official validator is scalar-only | initial simple-type scope explicitly accepted; typed compound support remains a follow-up, without string encoding | fallback aceito |
| `EMC-GAP-002` | configurable per-exercise memory limit | publish execution requirements faithfully without claiming unenforced restrictions | coding draft and official worker | public CreateEmceptionOptions, RunOptions and NativeBuildConfig expose no per-program memory cap | explicit capability deferral approved; preserve data, no silent ignore or runtime change | escopo reduzido |

### `EMC-GAP-002`. Per-exercise memory control

- Date: 2026-11-21. Active boundary: UX02-C/WB-B.
- The draft/canonical model contains `memoryLimitBytes`. Inspection of public
  browser creation, run and native-build contracts found no per-program memory
  limit. The worker's existing Chromium JS heap cap is an infrastructure limit
  for a different resource/scope, not enforcement of this authoring field.
- No memory stress test or real compiler/browser execution was performed.
  This is a public configuration gap, not a claim that the runtime has no
  internal WebAssembly maximum or that a process-wide limit would suffice.
- Deadline/output bounds can be composed with the public output sinks and
  `dispose()`; CS-ADR-028 authorizes that targeted grading guard only. The
  browser facade currently ignores `timeoutMs`, so setting TestPlan alone
  was not cancellation proof. Guard/fake-API tests now pass; actual runtime
  evidence remains a separate CODE-UX-03 gate.
- Decision accepted on 2026-11-21: explicitly defer configurable exercise RAM
  while delivering real time/output limits; keep the upstream runtime intact.
  CS-ADR-029 separates that scope choice from the independently authorized
  10 GB build/test budget. Do not erase historical values, silently ignore a
  configured unsupported memory setting or claim it is enforced.
- Follow-up: record the disposition, then TS/backend wire and revision parity
  before consumer integration. No external issue or remote workflow was created.

### `EMC-GAP-001`. Valores funcionais compostos

- Data: 2026-11-19.
- Status: confirmed public boundary; initial scalar-only scope explicitly
  accepted on 2026-11-20 (`CS-ADR-015`), compound support not implemented.
- Subplano: decisao em `CODE-UX-01`; caso `CS-REF-18`.
- Evidence: on 2026-11-19, an actual import of installed `emception/testing`
  generated an integer harness and rejected Array inputs, Array returns and
  nested arrays with `Array/Dictionary parameter types not supported in v1`.
  `mapCppType` also rejects Array/Dictionary. This probe did not boot a compiler.
- Rechecked on 2026-11-20 at HEAD `40a8470b46`, using the installed public
  ESM import `emception/testing` from `apps/web`: four scalar harnesses succeed;
  six compound probes reject Array/Dictionary inputs/types/returns, including
  nested arrays. No private import, new dependency or compiler boot. Product
  acceptance of the initial scalar-only scope was subsequently given explicitly
  by the user; no support was inferred from the rejection probes.
- Other public paths inspected: `TestPlan` exposes generic doctest/custom
  callbacks, not an alternative typed compound function contract. The official
  worker uses the retained assessment mapper/public harness; Courses validation
  and `@game-guild/coding` schemas allow string/boolean/integer/float only.
  A new private/custom host harness would be a competing implementation, not
  evidence that compound cases already work through the supported boundary.
- Limit of evidence: no real browser/server compound compilation was run;
  the unsupported type fails before compilation. This is not a general claim
  that an arbitrary C++ program cannot use arrays internally.
- Owner: coding/grading registra o requisito; o responsavel pelo Emception
  decide eventual evolucao upstream. O responsavel de produto aceita ou nao um
  corte inicial sem valores compostos.
- Nao autorizado: modificar Emception, criar harness concorrente ou esconder a
  limitacao com inputs tratados como strings sem contrato tipado.
- Follow-up: implement the public typed harness/transport with the Emception
  owner, then TS/backend parity and browser/server conformance. The limited
  initial algorithm scope is now explicitly approved. No external issue was published
  during this task. The capability matrix is in
  [`PRODUCT-MODEL.md`](./06-refoundation/PRODUCT-MODEL.md).

## Integracao ausente nao e capability upstream ausente

`tools/emception/packages/browser/src/canvas.ts` expoe build/start/stop por API
publica. O workbench atual nao a integra; isso pertence a coding-surface, nao
prova a necessidade de alterar Emception. A 3.6.3 deve provar renderizacao,
entrada, resize e cleanup com artifact real. Terminal interativo tambem exige
prova do caminho publico, nao apenas um painel de logs.

## Pre-condicao de artifact, nao capability gap

The local browser cycle found a schema-1 CDN manifest, while the grading
binding requires schema 2, the frozen version/ABI/lock hash, receipt and
fingerprint. This is an artifact precondition, not a missing public API.

On 2026-11-20, published 4.5.2 Toolchain/Core CDN manifests passed that binding
check. The local copies still fail and need an explicit backed-up refresh
before CODE-UX-03 execution; no full artifact download or local runtime pass
has been recorded. `CS-ADR-024` withdraws the mandatory source-build/build-only
CI proposal. The existing source classifier still requires exact-source
provenance when applicable; unchanged-runtime consumers can reuse published
assets. Refreshing those assets alone does not require an upstream IDE
deployment, runtime source change or new release. The separately approved
grading-worker edits now match the official source classifier and therefore
require its existing exact-source CI provenance; that gate is not waived.

Status permitidos: `investigando`, `confirmado`, `fallback aceito`,
`proposta upstream`, `resolvido upstream`, `escopo reduzido` e `rejeitado`.

## Modelo para novo gap

```text
### `EMC-GAP-NNN`. Titulo

- Data:
- Status:
- Subplano bloqueado:
- Capability necessaria:
- Caso de uso e persona:
- Consumidor:
- Entry points publicos verificados:
- Evidencia de que composicao nao resolve:
- Risco de dados ou compatibilidade:
- Fallback ou reducao de escopo:
- Proposta ao responsavel do Emception:
- Decisao e evidencia:
```

## Criterio para remover um gap

Entradas nao sao apagadas. Quando resolvidas, recebem status final, referencia
da decisao e teste que prova a capability. Isso preserva o historico sem tornar
uma limitacao antiga em requisito permanente.
