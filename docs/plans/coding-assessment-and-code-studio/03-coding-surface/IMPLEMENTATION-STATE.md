# Coding surface implementation state

Ultima atualizacao: 2026-11-18.

Status geral: implementacao funcional concluida; consolidacao canonica 3.5
planejada antes do gate browser final no CI.

Este e o handoff mutavel da parte 3. Atualize-o com fatos observados. Nao marque
um gate como concluido sem registrar a evidencia executada.

## Baseline de execucao

- Branch: `feature/coding-grading`.
- Commit-base: `41932100b2031c0ad78d7c02545a73addc6364b3`.
- Worktree preexistente: limpo; branch um commit a frente de origin.
- Worktree atual: contem somente a implementacao e a documentacao da parte 3
  sobre o commit-base registrado.
- Ultimo merge relevante: nenhum merge em andamento.
- Observacao: a criacao destes planos nao caracteriza a implementacao.

## Pre-requisitos

| Entrega | Estado | Evidencia |
| --- | --- | --- |
| Parte 01, dominio e adapter TypeScript | concluida | plano 01 e suites registradas nele |
| Parte 02, boundary backend de code | concluida | plano 02, build integral e suites registradas nele |
| Parte 3.1 | concluida | buscas de consumidores, contratos e exports publicos registradas em 2026-11-18 |
| Parte 3.2 | concluida | package isolado, 14 testes e typecheck verdes |
| Parte 3.3 | concluida | dois perfis, quatro modos e privacy cobertos; contrato sera consolidado em 3.5 |
| Parte 3.4 | em validacao final | consumidores e CI migrados; browser E2E depende do artifact schema 2 exato |
| Parte 3.5 | planejada | remover compatibilidade artificial de code antes do browser E2E final |

## Etapa ativa

- Subplano: 3.5, consolidacao do contrato canonico de coding.
- Gate atual: `CANON-01`, inventario de contratos e adapters versionados.
- Estado: planejado; implementacao ainda nao iniciada.
- Responsavel: Codex.

## Fatos confirmados

- `@game-guild/emception-ui` deve permanecer preservado.
- `coding-surface` sera um consumidor paralelo dos exports publicos do
  Emception.
- somente fluxos de grading explicitamente selecionados podem migrar.
- `function` e `standard-io` formam o corte inicial de tipos.
- nenhuma mudanca de tabela ou migration esta prevista na parte 3.
- `CodingAssignmentContent.ExerciseType` permanece no JSON ja persistido; nao
  cria coluna, tabela ou migration.
- `@game-guild/emception-ui` nao foi alterado; a unica mudanca sob
  `tools/emception` corrige um teste de binding para o path do adapter extraido
  na parte 2.

## Decisoes abertas

Nenhuma decisao aberta. A decisao V2 foi substituida pela consolidacao canonica
registrada em `DECISIONS.md`; bridge do host, artifact verificado e ajuste
test-only permanecem validos.

## Bloqueios

O CDN local ainda contem um manifest legado schema 1. O browser E2E V3 exige o
artifact schema 2 com identidade congelada correspondente ao commit e falha
fechado antes de iniciar API ou banco. Isso nao e gap de capability nem motivo
para relaxar a verificacao: o lane `coding` agora adquire e valida o artifact
exato antes do ciclo browser.

## Evidencias de validacao da etapa ativa

Validacoes locais executadas sobre a implementacao. O browser E2E integral
permanece reservado ao CI enquanto o artifact exato nao estiver no workspace.

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

## Arquivos alterados pela implementacao

- contratos de coding ainda duplicados em V1/V2, a consolidar na parte 3.5;
- compatibilidade de adapter backend e TypeScript a remover na parte 3.5;
- novo `packages/features/coding-surface`;
- campo JSON `ExerciseType` e validacao no modelo de Learning Courses;
- contrato OpenAPI e cliente TypeScript gerado para `ExerciseType`;
- bridge e cinco consumidores de grading em `apps/web`;
- lane `coding`, classificacao e required gate no CI;
- testes, fixtures e documentos de checkpoint associados.

## Proxima acao exata

Executar `CANON-01` da parte 3.5 e consolidar coding em um unico contrato e
adapter. Depois repetir as suites e deixar o lane `coding` adquirir o artifact
schema 2 exato. Com o browser E2E verde, registrar a evidencia, concluir
`CODE-UI-07` e iniciar a parte 4.

## Checklist de checkpoint

Antes de interromper ou trocar de executor:

- [x] baseline e worktree atualizados;
- [x] etapa e gate refletem o estado real;
- [x] arquivos alterados listados;
- [x] comandos e resultados registrados;
- [x] decisoes transversais registradas;
- [x] consumer map atualizado;
- [x] gaps e duplicacoes atualizados quando aplicavel;
- [x] bloqueios possuem evidencia;
- [x] proxima acao e executavel sem depender da conversa.
