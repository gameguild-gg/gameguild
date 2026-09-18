# ADR: Conclusão de content e liberação de resultado

- Status: Aceito
- Data: 2026-09-03
- Escopo: Assessments, Courses e Grading

## Contexto

Finalizar uma avaliação, liberar o resultado ao aluno, consolidar gradebook e
concluir content são transições diferentes. Inferir uma delas de peso ou de uma
rota genérica pode vazar aprovação retida e produzir progresso prematuro.

## Decisão

`AssessmentContentCompletionPolicyV1` possui os modos `on-submit`,
`on-finalize`, `on-release` e `on-release-and-pass`. O default inicial para
content avaliável é `on-release-and-pass`. A projeção learner-visible nunca
antecipa `Passed` de uma rodada retida.

`AssessmentResultReleasePolicyV1` possui `immediate`, `manual` e `scheduled`.
Somente `immediate` e `manual` são habilitados inicialmente; `scheduled` fica
reservado no contrato e requer capability operacional posterior.

Finalização e release são estados independentes. Release referencia uma rodada
única e o comando `ReleaseGradeResult` valida, pela cadeia relacional, que essa
rodada pertence à submission informada. A policy `immediate` persiste uma
solicitação durável na mesma transação da finalização; um worker usa o mesmo
comando idempotente disponível para liberação manual.

`ContentInteraction`, quando mantido, é somente read model para content
avaliável. Rotas genéricas de submit, conclusão, progresso ou grade não podem
decidir seu estado.

## Consequências

- peso zero não define política de release nem conclusão;
- conclusão, gradebook e notificação consomem eventos canônicos;
- falha entre finalização e dispatch não perde liberação imediata;
- `scheduled` não aparece como opção executável até sua fase própria.

## Alternativas rejeitadas

- marcar content concluído diretamente pelo browser;
- considerar finalização como liberação implícita;
- duplicar `AssessmentSubmissionId` na entidade de release;
- inferir policy pelo peso do grupo.
