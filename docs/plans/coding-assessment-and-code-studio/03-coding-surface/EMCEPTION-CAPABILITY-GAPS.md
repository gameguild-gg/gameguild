# Emception capability gaps for coding surface

Status: baseline reaberta para 3.6; limite de valores compostos em investigacao.

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
| `EMC-GAP-001` | argumentos/retornos funcionais compostos | exercicios com arrays e matrizes | coding e coding-surface; executor oficial | testes do harness atual rejeitam Array; demais caminhos publicos precisam de verificacao | decisao de escopo explicita e proposta upstream, sem conversao opaca para string | investigando |

### `EMC-GAP-001`. Valores funcionais compostos

- Data: 2026-11-19.
- Status: investigando.
- Subplano: decisao em `CODE-UX-01`; caso `CS-REF-18`.
- Evidencia: leitura de
  `tools/emception/packages/core/src/testing/functional/harness.test.ts`
  confirma assertivas de rejeicao de Array em parametros e retorno. Nao foi
  executada nova prova de runtime durante a reescrita documental.
- Limite da evidencia: demonstra restricao nesse harness, nao ausencia em toda
  API publica. Verificar contratos, exports e caminho servidor antes de fechar
  a investigacao.
- Owner: coding/grading registra o requisito; o responsavel pelo Emception
  decide eventual evolucao upstream. O responsavel de produto aceita ou nao um
  corte inicial sem valores compostos.
- Nao autorizado: modificar Emception, criar harness concorrente ou esconder a
  limitacao com inputs tratados como strings sem contrato tipado.
- Proxima acao: preencher matriz familia/runtime/executor na 3.6.1 e registrar
  suporte provado ou bloqueio com proposta e disposicao explicita.

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
