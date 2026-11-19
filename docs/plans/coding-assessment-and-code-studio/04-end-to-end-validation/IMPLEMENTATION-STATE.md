# Estado de implementacao da parte 4

Atualizado: 2026-11-19.

## Baseline observada

- branch: `feature/coding-grading`;
- HEAD avaliado: `43144e23ec`;
- worktree: limpo antes desta atualizacao documental;
- tracking: alinhado com `origin/feature/coding-grading`;
- relacao com `origin/develop`: 188 commits atras e 7 a frente;
- parte 3: implementacao local consolidada, `CODE-UI-07` pendente;
- etapa ativa: `CODE-E2E-00`;
- estado: bloqueado ate reconciliar `develop`, corrigir a fixture canonica e
  obter a lane exata.

Essa baseline e evidencia historica, nao valor permanente. O executor deve
atualiza-la antes de iniciar implementacao.

## Gates

| Etapa | Estado | Evidencia ou bloqueio |
| --- | --- | --- |
| `CODE-E2E-00` | bloqueado | branch atras de `develop`; runner omite `ExerciseType`; lane exata pendente |
| `CODE-UI-07` | pendente | exige artifact Emception schema 2 e browser E2E oficial |
| `CODE-E2E-01` | nao iniciado | depende de `CODE-E2E-00` |
| `CODE-E2E-02` | nao iniciado | depende de `CODE-E2E-01` |
| `CODE-E2E-03` | nao iniciado | depende de `CODE-E2E-02` |
| `CODE-E2E-04` | nao iniciado | depende de `CODE-E2E-03` |
| `CODE-E2E-05` | nao iniciado | depende de `CODE-E2E-04` |
| `CODE-E2E-06` | nao iniciado | depende de `CODE-E2E-05` |
| `CODE-E2E-07` | nao iniciado | depende do aceite completo |

## Achados de entrada

1. `apps/web/scripts/coding-cycle-browser-e2e.mjs` possui
   `buildCodingContent()` sem o `ExerciseType` requerido pelo contrato atual.
2. O plano anterior dizia quatro subplanos da parte 3, mas existem cinco.
3. O cutover dos consumidores de grading ja ocorreu na parte 3; a parte 4 deve
   valida-lo, nao repeti-lo.
4. A matriz anterior nao separava obrigatoriamente `function` e `standard-io`.
5. A identidade canonica deste corte e `code-assessment-type@1`,
   `code-wasm-review@1` e payload `code-files`.
6. O artifact local schema 1 nao substitui o artifact oficial schema 2.

## Proxima acao exata

Atualizar esta baseline depois de reconciliar a branch com `develop`; em
seguida executar o subplano
[`00-entry-baseline-and-part-3-closure.md`](./00-entry-baseline-and-part-3-closure.md).

## Checklist de checkpoint

- [ ] baseline atualizada apos reconciliacao;
- [ ] subplano e gate ativos refletem o estado real;
- [ ] arquivos alterados listados;
- [ ] comandos e resultados registrados;
- [ ] decisoes transversais registradas;
- [ ] achados possuem owner e case ID quando aplicavel;
- [ ] bloqueios possuem evidencia;
- [ ] proxima acao e executavel sem depender da conversa.

