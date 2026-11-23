# 4.6. Aceite e integracao em develop

Status: planejado; depende de `CODE-E2E-05`.

IDs: `CODE-E2E-06`, `CODE-E2E-07`.

## Objetivo

Executar a validacao consolidada, revisar humanamente os reports e preparar o
PR integrado das partes 01 a 04 sem incluir expansao de linguagem ou Code
Studio.

## Implementacao

1. Executar `coding:validation:contracts`, `coding:validation:smoke` e
   `coding:validation:full` sobre a branch reconciliada com `develop`.
2. Executar builds warning-clean, suites backend afetadas e verificacoes de
   arquitetura/autorizacao exigidas pelo `AGENTS.md`.
3. Revisar cada report e promover o status canonico somente por decisao humana.
4. Validar o `DUPLICATION-REGISTER.md` contra o diff real.
5. Confirmar que os consumidores de grading migrados na parte 3 usam
   `coding-surface` e que os consumidores preservados continuam funcionando.
6. Mapear toda assertiva restante do runner anterior para case ID ou divida de
   harness explicita.
7. Registrar blockers de linguagem na parte 5 e questoes de Code Studio no
   plano correspondente, sem inclui-los neste aceite.
8. Revisar o diff para excluir trabalho alheio ao programa.
9. Abrir um unico PR para `develop` cobrindo as partes 01 a 04, com comandos,
   resultados, case IDs, riscos e blockers aceitos.

`workflow_dispatch` e draft PR podem ser usados antes para obter artifact e
evidencia de CI. Eles nao substituem o PR final integrado.

## Preservacao obrigatoria

- nao remover ou remodelar `@game-guild/emception-ui`;
- nao remover capabilities, toolchains ou consumidores de
  `tools/emception/**`;
- nao remover o coding assignment preservado por este programa;
- nao alterar schema, migration ou modulo de plataforma para fechar o harness;
- nao criar compatibilidade artificial com contratos intermediarios deste
  roadmap;
- nao incluir linguagens da parte 5 nem refundacao do Code Studio.

## Mandatory official basic-core proof

Apply CS-ADR-041 before claiming that basic coding grading works. This is an
acceptance requirement, not a passing report or a replacement for existing
part 3/4 requirements. Use simple synthetic exercises and known expectations.

| Core control | Required observable oracle |
| --- | --- |
| scalar `function` | instructor authors/publishes integer addition; learner submits visible code without transport boilerplate; known correct/wrong and zero/negative cases yield expected official outcomes |
| `standard-io` | instructor authors/publishes an I/O sum; public/private cases remain distinct; correct/wrong submissions produce independently specified official results |
| review and release | automated and instructor review use real authorized personas; frozen submission/revision and integer score/feedback agree; results stay hidden before release and appear after learner refresh/new session |
| failure and access negatives | compiler/runtime/infrastructure failures remain distinguishable under the official contract; invalid/missing code cannot pass falsely; private data and other actors/tenants' work remain inaccessible |

Run the existing contracts/smoke/full commands with real pinned runtime assets,
authorized API execution and isolated persona browsers. Record case IDs, exact
commands/exits, source/artifact identity and sanitized evidence. Mocks, an IDE
demo, local exit 0 or the six text-port controls cannot satisfy this table. A
core failure blocks the affected flow instead of becoming an accepted skip,
arbitrary zero score or fabricated success.

## Gate de aceite

- todos os casos obrigatorios estao `pass` ou possuem blocker externo aceito e
  documentado; the basic-core table above admits no blocker waiver;
- `function` e `standard-io` passam pelo fluxo completo;
- validacao humana e automatica usam o mesmo lifecycle;
- nenhum aceite final depende de leitura direta do banco;
- toda duplicacao conhecida esta registrada;
- nao existem condicionais de code no core generico;
- frozen artifact identity, applicable source provenance and official browser
  E2E checks pass; no new upstream release is forced merely for UI changes;
- builds e suites obrigatorias passam na branch reconciliada;
- o diff do PR corresponde somente ao dominio, adapter, backend, surface,
  integracao e validacao aprovados nas partes 01 a 04.

## Correcao posterior

Implementacoes transitorias so podem ser removidas em trabalho separado com
evidencia de cobertura equivalente. Um bug encontrado pelo programa deve ser
corrigido no modulo owner e revalidado; nao reduzir o catalogo para obter
verde.

## Post-04 stabilization before part 05

Order explicitly accepted by the user: complete required basic grading proof
in part 04, then correct and polish recorded noncritical defects before part
05. Keep the [single findings register](../03-coding-surface/IMPLEMENTATION-STATE.md#current-priority-and-post-plan-findings---2026-11-23)
as the status source, not a second competing backlog.

For each confirmed defect, preserve the original failure and user-facing
limitation, select its owner and repair scope, validate the contract, reproduce
an independent RED, implement the minimum correction and run applicable strict
gates plus neighboring/preserved-consumer regressions. Obtain the Emception
owner's scoped review before an upstream change. This checkpoint authorizes
no runtime/IDE refactoring, artifact rewriting or platform/schema change.
Optional future enhancements are not covert repairs.

Checkpoint exit requires:

- the part 04 basic-core E2E proof still passes after corrections;
- confirmed defects in retained/offered functionality are fixed with their
  original oracles and regressions, not removed assertions;
- unsupported future capabilities remain explicitly unavailable with an
  owner-reviewed disposition, never mislabeled as implemented support;
- applicable G-SURFACE/G-WEB/G-API/G-RUNTIME and preserved Emception regressions
  return 0 for each repair; a required failure keeps that repair open;
- desktop/mobile and instructor/learner task review verifies affected UX;
  pending human feedback is not automatically accepted;
- the ledger records exact commands/results and final owner/human review.

Do not infer completion from this document or start part 05 to bypass known
core debt. This checkpoint does not change the existing integrated-PR strategy
or promote canonical capability status without human review.
