# Estado de implementacao da parte 4

Atualizado: 2026-11-20.

## Baseline observada

- branch: `feature/coding-grading`;
- HEAD avaliado: `43144e23ec`;
- worktree: limpo antes desta atualizacao documental;
- tracking: alinhado com `origin/feature/coding-grading`;
- relacao com `origin/develop`: 188 commits atras e 7 a frente;
- parte 3: implementacao local consolidada ate 3.5; refundacao 3.6 em seis
  etapas planejadas e `CODE-UI-07` pendente;
- etapa ativa: fora da parte 4; `CODE-UX-01` na parte 3.6;
- current dependency: part 4 stays blocked until `CODE-UX-06`. Fixture/binding
  preparation belongs to `CODE-UX-01`; actual asset synchronization and local
  preflight belongs to `CODE-UX-03`. Part 4 revalidates both with official CI.

Essa baseline e evidencia historica, nao valor permanente. O executor deve
atualiza-la antes de iniciar implementacao.

## Gates

| Etapa | Estado | Evidencia ou bloqueio |
| --- | --- | --- |
| `CODE-UX-01` | partial / open | design and initial scalar scope explicitly accepted; baseline closure audit/handoff pending; no compiler-build prerequisite |
| `CODE-UX-02` a `CODE-UX-06` | nao iniciado | depende do gate anterior; layout-only evidence does not certify runtime or product acceptance |
| `CODE-E2E-00` | bloqueado | depende de `CODE-UX-06`; revalida preparacao da 3.6.1/3.6.3 e fecha evidencia oficial |
| `CODE-UI-07` | pendente | exige artifact Emception schema 2 e browser E2E oficial |
| `CODE-E2E-01` | nao iniciado | depende de `CODE-E2E-00` |
| `CODE-E2E-02` | nao iniciado | depende de `CODE-E2E-01` |
| `CODE-E2E-03` | nao iniciado | depende de `CODE-E2E-02` |
| `CODE-E2E-04` | nao iniciado | depende de `CODE-E2E-03` |
| `CODE-E2E-05` | nao iniciado | depende de `CODE-E2E-04` |
| `CODE-E2E-06` | nao iniciado | depende de `CODE-E2E-05` |
| `CODE-E2E-07` | nao iniciado | depende do aceite completo |

## Achados de entrada

1. Historical finding resolved in `CODE-UX-01`: the browser cycle now imports
   `buildCodingContent()` with explicit `ExerciseType` from its testable fixture
   module. The canonical bridge regression passed; real runtime remains blocked.
2. A parte 3 possui seis subplanos; o sexto e um coordenador de seis etapas de
   refundacao, com requisitos de autoria, aluno e runtime baseados no benchmark.
3. O cutover dos consumidores de grading ja ocorreu na parte 3; a parte 4 deve
   valida-lo, nao repeti-lo.
4. A matriz anterior nao separava obrigatoriamente `function` e `standard-io`.
5. A identidade canonica deste corte e `code-assessment-type@1`,
   `code-wasm-review@1` e payload `code-files`.
6. O artifact local schema 1 nao substitui o artifact oficial schema 2.
7. Updated part 3 evidence: fixture repaired; 5 bridge tests, 26 lightweight
   preflight tests and 30 typed prototype checks recorded. Design and initial
   scalar scope were explicitly accepted. Published 4.5.2 manifests pass the
   frozen binding check; local schema-1 copies still need refresh. No full
   download/local synchronization or real-runtime acceptance was performed.
   The former compulsory source-build assumption is corrected by CS-ADR-024;
   conditional CI provenance remains intact. Canonical status stays in the
   shared [part 3 state](../03-coding-surface/IMPLEMENTATION-STATE.md).
8. A matriz `CS-REF-*` deve ser mapeada ao catalogo da parte 4, sem duplicar a
   fonte de status ou considerar o planejamento como aceite do produto.

## Proxima acao exata

Follow the shared part 3 ledger: audit baseline closure in `CODE-UX-01`, then
domain/draft work in `CODE-UX-02`. Do not repeat the approved prototype or wait
for an unnecessary Emception build/IDE release. Before real execution in
`CODE-UX-03`, preserve old generated assets, synchronize the compatible pinned
published package and pass actual local preflight. Then complete the rest of
[`3.6`](../03-coding-surface/06-authoring-information-architecture-and-teacher-ux.md).
Depois, atualizar esta baseline e executar
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
