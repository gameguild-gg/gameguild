# Emception capability gaps for coding surface

Status: public compound-value limitation confirmed in `CODE-UX-01`; product
disposition remains pending. No upstream change has been made.

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
| `EMC-GAP-001` | argumentos/retornos funcionais compostos | exercicios com arrays e matrizes | coding e coding-surface; executor oficial | installed public harness rejects Array inputs/returns; public generic/custom cases do not supply a typed transport; official validator is scalar-only | explicit product disposition and upstream owner proposal, without string encoding | confirmado |

### `EMC-GAP-001`. Valores funcionais compostos

- Data: 2026-11-19.
- Status: confirmed public boundary; scope decision pending (`CS-ADR-015`).
- Subplano: decisao em `CODE-UX-01`; caso `CS-REF-18`.
- Evidence: on 2026-11-19, an actual import of installed `emception/testing`
  generated an integer harness and rejected Array inputs, Array returns and
  nested arrays with `Array/Dictionary parameter types not supported in v1`.
  `mapCppType` also rejects Array/Dictionary. This probe did not boot a compiler.
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
  owner, then TS/backend parity and browser/server conformance; or explicitly
  approve the limited initial algorithm scope. No external issue was published
  during this task. The capability matrix is in
  [`PRODUCT-MODEL.md`](./06-refoundation/PRODUCT-MODEL.md).

## Integracao ausente nao e capability upstream ausente

`tools/emception/packages/browser/src/canvas.ts` expoe build/start/stop por API
publica. O workbench atual nao a integra; isso pertence a coding-surface, nao
prova a necessidade de alterar Emception. A 3.6.3 deve provar renderizacao,
entrada, resize e cleanup com artifact real. Terminal interativo tambem exige
prova do caminho publico, nao apenas um painel de logs.

## Pre-condicao de artifact, nao capability gap

O browser cycle local encontrou um manifest CDN schema 1, enquanto o contrato
V3 exige o artifact schema 2 correspondente ao commit. Isso e uma pre-condicao
de supply chain e provenance, nao uma API ausente no Emception. O lane `coding`
adquire o artifact exato e valida sua identidade antes do E2E; a superficie nao
usa import privado nem pede mudanca upstream para contornar essa condicao.

Current artifact evidence: both local manifests remain `version: 1`; the
release has no provenance record. The exact HEAD/tree GitHub Actions query
returned zero artifacts. `CODE-UX-01` records this separately as infrastructure
unavailability; there is no acquisition or successful runtime preflight to claim.

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
