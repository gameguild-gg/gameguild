# Coding surface implementation state

Ultima atualizacao: 2026-11-19.

Status geral: implementacao funcional e consolidacao canonica 3.5 concluidas
localmente; fixture canonica do runner e gate browser final ainda pendentes.

Este e o handoff mutavel da parte 3. Atualize-o com fatos observados. Nao marque
um gate como concluido sem registrar a evidencia executada.

## Baseline de execucao

- Branch: `feature/coding-grading`.
- Commit-base da consolidacao: `810c2a0764`.
- Baseline reavaliada em `43144e23ec`: branch alinhada com
  `origin/feature/coding-grading`, 188 commits atras e 7 a frente de
  `origin/develop` antes da atualizacao documental da parte 4.
- Worktree atual: consolidacao 3.5 implementada e validada localmente sobre a
  implementacao funcional da parte 3; documentos da parte 4 em atualizacao.
- Ultimo merge relevante: nenhum merge em andamento.
- Observacao: a criacao destes planos nao caracteriza a implementacao.

## Pre-requisitos

| Entrega | Estado | Evidencia |
| --- | --- | --- |
| Parte 01, dominio e adapter TypeScript | concluida | plano 01 e suites registradas nele |
| Parte 02, boundary backend de code | concluida | plano 02, build integral e suites registradas nele |
| Parte 3.1 | concluida | buscas de consumidores, contratos e exports publicos registradas em 2026-11-18 |
| Parte 3.2 | concluida | package isolado, 14 testes e typecheck verdes |
| Parte 3.3 | concluida | dois perfis, quatro modos, privacy e contrato canonico cobertos |
| Parte 3.4 | em validacao final | consumidores e CI migrados; runner deve emitir `ExerciseType` canonico e browser E2E depende do artifact schema 2 exato |
| Parte 3.5 | concluida localmente | `CANON-01` a `CANON-06` concluidos; validacao local verde, com fixture do runner e browser CI ainda pendentes no fechamento de `CANON-07` |

## Etapa ativa

- Subplano: 4.0, fechamento da parte 3 apos a consolidacao 3.5.
- Gate atual: `CODE-UI-07`, fixture canonica e browser coding cycle no lane com
  artifact exato.
- Estado: implementacao do produto concluida localmente; harness e evidencia
  externa pendentes.
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
- o diff atual de `@game-guild/emception-ui` e `tools/emception` permanece vazio;
- `@game-guild/coding` possui um unico contrato, parser por payload e fixture
  canonica, com `exerciseType` obrigatorio;
- backend e TypeScript registram apenas `code-assessment-type` e
  `code-wasm-review` na versao `1`, com payload schema `code-files`;
- qualquer adapter, handler, payload schema ou toolchain diferente da identidade
  corrente falha fechado;
- nomes `V1` importados de `@game-guild/grading` pertencem ao protocolo
  generico e permanecem fora da consolidacao de code.

## Decisoes abertas

Nenhuma decisao aberta. A proposta de formatos paralelos foi substituida pela
consolidacao canonica registrada em `DECISIONS.md`; bridge do host, artifact
verificado e ajuste test-only permanecem validos.

## Bloqueios

O helper `buildCodingContent()` de
`apps/web/scripts/coding-cycle-browser-e2e.mjs` ainda omite o
`ExerciseType` obrigatorio. O write path atual valida esse campo, portanto a
fixture deve ser atualizada para os tipos canonicos `function` e `standard-io`.
Nao se deve restaurar ausencia de tipo, alias ou fallback para compatibilidade.

O CDN local contem o manifest operacional `version: 1`. O browser E2E oficial
exige o artifact de grading `schemaVersion: 2` com identidade congelada
correspondente ao commit e falha
fechado antes de iniciar API ou banco. Isso nao e gap de capability nem motivo
para relaxar a verificacao: o lane `coding` agora adquire e valida o artifact
exato antes do ciclo browser.

A suite `GameGuild.Identity.Authentication.UnitTests` possui uma falha externa
reproduzida tambem de forma isolada em
`EmailCodeServiceTests.GenerateEmailCodeAsync_ThrottlesResendsWithinSixtySeconds`
(`4572/4573` testes verdes). Nao ha diff no modulo de Authentication e a falha
nao pertence ao contrato de coding; ela deve ser tratada pelo responsavel desse
modulo sem ampliar o escopo da parte 3.

## Evidencias de validacao da etapa ativa

Validacoes locais executadas sobre a implementacao. O browser E2E integral
permanece reservado ao CI enquanto a fixture nao estiver canonica e o artifact
exato nao estiver no workspace.

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
| 2026-11-19 | inventario `rg` de contratos, parsers, fixtures, adapters, toolchains e payload schemas | `CANON-01` concluido; escopo restrito a coding, adapter, surface, backend CodeAdapter, web e testes | 3.5 |
| 2026-11-19 | suites e typechecks de `coding`, `grading-adapter-code`, `coding-surface` e `grading` | 15 + 15 + 14 + 16 testes verdes; quatro typechecks verdes | 3.5 |
| 2026-11-19 | testes focados dos consumidores web | 11 arquivos e 195 testes verdes com um worker | 3.5 |
| 2026-11-19 | TypeScript integral de `apps/web` com heap de 8 GiB | verde apos regeneracao do client | 3.5 |
| 2026-11-19 | publish da API, captura OpenAPI e geracao oficial de `@game-guild/client` | `exerciseType` requerido; client typecheck e build verdes | 3.5 |
| 2026-11-19 | `CodeAdapter.UnitTests`, `Assessments.UnitTests` e `Courses.UnitTests` | 72 + 453 + 898 testes verdes | 3.5 |
| 2026-11-19 | `AssessmentGradingPostgreSqlHttpTests` com certificado local de Data Protection | 14 testes de integracao PostgreSQL verdes | 3.5 |
| 2026-11-19 | `@game-guild/emception-ui test` | 7 suites e 27 testes verdes; package preservado | 3.5 |
| 2026-11-19 | preflight do coding cycle fora do sandbox | 45 testes verdes, incluindo toolchain, browser observer e processos | 3.5 |
| 2026-11-19 | build de producao web | compilacao, TypeScript e 337 paginas verdes com `cpus: 1` | 3.5 |
| 2026-11-19 | `dotnet build apps/api/GameGuild.sln --no-restore --nologo -m:2 -p:TreatWarningsAsErrors=true` | sucesso em 5m40s, zero warnings e zero erros | 3.5 |
| 2026-11-19 | `CodingAssignmentJsonBodyMigrationTests` | 8 testes verdes; snapshot test-only preserva a saida historica sem enfraquecer `ExerciseType` no contrato atual | 3.5 |
| 2026-11-19 | `GameGuild.API.UnitTests` integral | 1882 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authorization.UnitTests` integral | 2815 testes verdes | 3.5 |
| 2026-11-19 | `GameGuild.Identity.Authentication.UnitTests` integral e caso isolado | 4572/4573 verdes; falha externa de throttle de email reproduzida isoladamente, sem diff no modulo | 3.5 |

## Arquivos alterados pela implementacao

- contrato canonico unico de coding, sem aliases de compatibilidade;
- identidade unica e concordante do adapter backend e TypeScript;
- novo `packages/features/coding-surface`;
- campo JSON `ExerciseType` e validacao no modelo de Learning Courses;
- contrato OpenAPI e cliente TypeScript gerado para `ExerciseType`;
- bridge e cinco consumidores de grading em `apps/web`;
- lane `coding`, classificacao e required gate no CI;
- testes, fixtures e documentos de checkpoint associados.
- ajuste exclusivamente test-only para ler a saida historica da migration; a
  migration, snapshots de banco e schema permanecem inalterados.

## Proxima acao exata

Reconciliar a branch com `develop` e executar o subplano
[`4.0`](../04-end-to-end-validation/00-entry-baseline-and-part-3-closure.md):
corrigir a fixture para `ExerciseType`, deixar o lane `coding` adquirir o
artifact schema 2 exato e executar o browser E2E oficial. Com a evidencia verde,
registrar o resultado e concluir `CODE-UI-07`. Nao iniciar `CODE-E2E-01` antes
disso nem substituir o artifact por um manifest local fabricado.

## Checklist de checkpoint

Antes de interromper ou trocar de executor:

- [x] baseline de avaliacao registrada;
- [x] etapa e gate refletem o estado real;
- [x] arquivos alterados listados;
- [x] comandos e resultados registrados;
- [x] decisoes transversais registradas;
- [x] consumer map atualizado;
- [x] gaps e duplicacoes atualizados quando aplicavel;
- [x] bloqueios possuem evidencia;
- [x] proxima acao e executavel sem depender da conversa.
