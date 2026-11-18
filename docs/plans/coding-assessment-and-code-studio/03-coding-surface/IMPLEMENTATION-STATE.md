# Coding surface implementation state

Ultima atualizacao: 2026-11-18.

Status geral: planejado.

Este e o handoff mutavel da parte 3. Atualize-o com fatos observados. Nao marque
um gate como concluido sem registrar a evidencia executada.

## Baseline de execucao

- Branch: a capturar no inicio do subplano 3.1.
- Commit-base: a capturar no inicio do subplano 3.1.
- Worktree preexistente: a capturar no inicio do subplano 3.1.
- Ultimo merge relevante: a confirmar no inicio do subplano 3.1.
- Observacao: a criacao destes planos nao caracteriza a implementacao.

## Pre-requisitos

| Entrega | Estado | Evidencia |
| --- | --- | --- |
| Parte 01, dominio e adapter TypeScript | concluida | plano 01 e suites registradas nele |
| Parte 02, boundary backend de code | concluida | plano 02, build integral e suites registradas nele |
| Parte 3.1 | nao iniciada | N/A |
| Parte 3.2 | aguardando 3.1 | N/A |
| Parte 3.3 | aguardando 3.2 | N/A |
| Parte 3.4 | aguardando 3.3 | N/A |

## Etapa ativa

- Subplano: 3.1, caracterizacao e baseline de decisoes.
- Gate atual: `CODE-UI-01`.
- Estado: nao iniciado.
- Responsavel: nao atribuido.

## Fatos confirmados

- `@game-guild/emception-ui` deve permanecer preservado.
- `coding-surface` sera um consumidor paralelo dos exports publicos do
  Emception.
- somente fluxos de grading explicitamente selecionados podem migrar.
- `function` e `standard-io` formam o corte inicial de tipos.
- nenhuma mudanca de tabela ou migration esta prevista na parte 3.

## Decisoes abertas

Nenhuma decisao adicional confirmada. O subplano 3.1 deve registrar somente as
que surgirem de evidencia do repositorio.

## Bloqueios

Nenhum bloqueio confirmado. Capability ausente deve ser registrada no arquivo
de gaps antes de ser tratada como bloqueio.

## Evidencias de validacao da etapa ativa

Nenhuma. Planejamento documental nao conta como execucao de `CODE-UI-01`.

| Data | Comando ou inspecao | Resultado | Escopo |
| --- | --- | --- | --- |
| N/A | N/A | nao executado | N/A |

## Arquivos alterados pela implementacao

Nenhum. Preencher durante a execucao, distinguindo codigo, testes e documentos.

## Proxima acao exata

Iniciar a parte 3.1: capturar branch, `HEAD` e worktree; buscar consumidores
atuais; preencher `CONSUMER-MAP.md`; atualizar duplicacoes e gaps com evidencia.

## Checklist de checkpoint

Antes de interromper ou trocar de executor:

- [ ] baseline e worktree atualizados;
- [ ] etapa e gate refletem o estado real;
- [ ] arquivos alterados listados;
- [ ] comandos e resultados registrados;
- [ ] decisoes transversais registradas;
- [ ] consumer map atualizado;
- [ ] gaps e duplicacoes atualizados quando aplicavel;
- [ ] bloqueios possuem evidencia;
- [ ] proxima acao e executavel sem depender da conversa.
