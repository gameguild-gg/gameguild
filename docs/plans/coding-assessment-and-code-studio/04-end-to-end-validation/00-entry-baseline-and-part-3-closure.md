# 4.0. Entrada, baseline e fechamento da parte 3

Status: planejado.

IDs: `CODE-E2E-00`, `CODE-UI-07`.

## Objetivo

Estabelecer uma baseline atual da `develop`, corrigir o harness anterior para o
contrato canonico e obter a evidencia exata que ainda falta para concluir a
parte 3. Este subplano e pre-condicao dos demais; ele nao cria o novo programa
de validacao.

## Implementacao

1. Registrar branch, HEAD, relacao com `origin/develop`, worktree e versoes das
   toolchains em `IMPLEMENTATION-STATE.md`.
2. Reconciliar a branch com a `develop` mais recente e caracterizar mudancas em
   auth, coding, Emception, CI e no ciclo browser antes de editar o harness.
3. Atualizar `buildCodingContent()` em
   `apps/web/scripts/coding-cycle-browser-e2e.mjs` para emitir o
   `ExerciseType` canonico exigido pelo contrato atual.
4. Cobrir explicitamente `function` e `standard-io`; nao usar ausencia do campo,
   alias ou fallback para preservar payload antigo.
5. Executar os testes unitarios do observer, transport e runtime web.
6. Executar o preflight do ciclo de coding.
7. Adquirir o artifact Emception schema 2 pela lane oficial e executar o browser
   E2E pelo `PR Verify` via `workflow_dispatch` ou draft PR.
8. Registrar a evidencia no estado da parte 3 e marcar `CODE-UI-07` concluido
   somente depois da lane verde.

## Gate

- branch reconciliada e baseline persistida;
- fixture do runner usa somente identidades canonicas;
- ambos os tipos de exercicio possuem cobertura identificavel;
- artifact schema 2 exato foi produzido e verificado pela lane oficial;
- browser E2E oficial passou sem manifest fabricado ou verificacao relaxada;
- nenhuma fonte de `tools/emception/**` ou `@game-guild/emception-ui` foi
  alterada;
- parte 3 registra `CODE-UI-07` como concluido com link ou run ID da evidencia.

## Bloqueios

- mudanca da `develop` que invalide o contrato atual deve ser caracterizada e
  decidida antes de adaptar o runner;
- indisponibilidade do produtor exato mantem `CODE-UI-07` aberto;
- falha real do produto nao pode ser escondida como falha do harness.

## Fora do escopo

- criar catalogo novo;
- criar lifecycle de cenarios;
- mudar auth ou MFA para facilitar o teste;
- adicionar linguagem, schema, migration ou nova versao de adapter.

