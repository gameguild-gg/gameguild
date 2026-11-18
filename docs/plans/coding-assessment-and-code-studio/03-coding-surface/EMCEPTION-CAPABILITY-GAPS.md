# Emception capability gaps for coding surface

Status: nenhum gap confirmado.

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
| N/A | nenhuma confirmada | N/A | N/A | ainda nao caracterizado | N/A | aberto para verificacao |

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
