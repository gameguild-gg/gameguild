# Emception capability gaps for coding surface

Status: baseline verificada; nenhum gap confirmado.

Este arquivo registra somente necessidades que nao podem ser atendidas pelos
entrypoints publicos do Emception instalado. Preferencia de API, conveniencia ou
codigo existente em um internal nao provam um gap.

Encontrar um gap nao autoriza editar Emception, `@game-guild/emception-ui`, usar
import `src/**` ou criar alias privado. O executor deve aplicar o
`EMCEPTION-GATE` do indice da parte 3.

## Registro

Nenhuma capability ausente foi confirmada durante o planejamento. O subplano
3.1 deve verificar esta afirmacao contra a revisao instalada.

| ID | Capability necessaria | Caso de uso | Consumidor | Evidencia nos exports publicos | Fallback | Estado |
| --- | --- | --- | --- | --- | --- | --- |
| N/A | nenhuma confirmada | editor, VFS, terminal e test runner | coding-surface | `@gameguild/emception-ide`, `emception` e `emception/testing` cobrem o corte | N/A | rejeitado |

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
