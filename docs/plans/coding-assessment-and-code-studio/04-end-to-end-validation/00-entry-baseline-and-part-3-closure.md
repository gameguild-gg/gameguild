# 4.0. Entrada, baseline e fechamento da parte 3

Status: bloqueado ate a conclusao de `CODE-UX-06` na parte 3.6.

IDs: `CODE-E2E-00`, `CODE-UI-07`.

## Objetivo

Estabelecer uma baseline atual da `develop`, revalidar o harness preparado na
3.6.1/3.6.3 para o contrato entregue e obter a evidencia exata que ainda falta para
concluir a parte 3. Este subplano e pre-condicao dos demais; ele nao cria o novo
programa de validacao.

Pre-requisito adicional: concluir
[`3.6`](../03-coding-surface/06-authoring-information-architecture-and-teacher-ux.md)
com autoria, resolucao e recursos de execucao aceitos. Nao congelar o E2E sobre
a composicao antiga que injeta formularios no output do workbench.

Fixture/binding preparation belongs to `CODE-UX-01`; actual synchronization of
compatible published assets and local preflight belongs to `CODE-UX-03`, before
runtime execution. There is no mandatory upstream IDE build/publication.
Here those prerequisites are revalidated, not unlocked for the first time.

## Implementacao

1. Registrar branch, HEAD, relacao com `origin/develop`, worktree e versoes das
   toolchains em `IMPLEMENTATION-STATE.md`.
2. Reconciliar a branch com a `develop` mais recente e caracterizar mudancas em
   auth, coding, Emception, CI e no ciclo browser antes de editar o harness.
3. Revalidar `buildCodingContent()` em
   `apps/web/scripts/coding-cycle-browser-e2e.mjs` para emitir o
   `ExerciseType` e os demais campos exigidos pelo contrato entregue; corrigir
   somente divergencias novas, sem reabrir compatibilidade especulativa.
4. Cobrir explicitamente `function` e `standard-io`; nao usar ausencia do campo,
   alias ou fallback para preservar payload antigo.
5. Executar os testes unitarios do observer, transport e runtime web.
6. Executar o preflight do ciclo de coding.
7. Verify the schema-2 assets against the frozen executable identity and
   refresh them if it changed. Run official browser E2E through `PR Verify`
   using its existing supported entry. Preserve exact-source acquisition when
   the changed-path classifier requires it; otherwise consume the matching
   published package. Do not dispatch the independent Emception deployment
   merely to generate assets (`CS-ADR-024` in part 3).
8. Registrar a evidencia no estado da parte 3 e marcar `CODE-UI-07` concluido
   somente depois da lane verde.

## Gate

- branch reconciliada e baseline persistida;
- fixture do runner usa somente identidades canonicas;
- ambos os tipos de exercicio possuem cobertura identificavel;
- the official lane verifies the pinned schema-2 assets and any applicable
  exact-source producer provenance; no compulsory new release for UI changes;
- browser E2E oficial passou sem manifest fabricado ou verificacao relaxada;
- no unauthorized upstream/retained-UI change; preserve the explicit narrow
  runtime/worker exceptions and their tests recorded in part 3's governance
  and ADRs, rather than treating an approved exception as a new blanket veto;
- carry the accepted post-plan findings under CS-ADR-039 separately from
  passed core evidence; no hidden skip, false academic verdict or private leak;
- parte 3 registra `CODE-UI-07` como concluido com link ou run ID da evidencia.

## Bloqueios

- mudanca da `develop` que invalide o contrato atual deve ser caracterizada e
  decidida antes de adaptar o runner;
- incompatible assets or missing required source provenance keep `CODE-UI-07`
  open; a missing producer for an out-of-scope consumer commit is not a blocker;
- falha real do produto nao pode ser escondida como falha do harness.

## Fora do escopo

- criar catalogo novo;
- criar lifecycle de cenarios;
- mudar auth ou MFA para facilitar o teste;
- adicionar linguagem, schema, migration ou nova versao de adapter.
