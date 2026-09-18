# Grading: matriz de autorizacao

## Escopo

Esta matriz fixa ator autenticado, sujeito representado, autorizacao, escopo e
auditoria dos comandos planejados. A persona de um `AuthorTest` e dado
simulado; ela nunca substitui o professor autenticado.

Permissoes de instrutor reutilizam o modelo existente:

- autoria, prepare, test e publish: `Program.{programId}.Edit`;
- review, override, regrade e release: `Program.{programId}.Review`;
- operacoes de aluno dependem de autenticacao, enrollment e vinculo com o
  subject congelado, nao de permissao administrativa generica;
- workers exigem service identity permitida para a capability exata do
  manifest e nao podem assumir identidade de professor ou aluno.

## Matriz

| Comando | Contexto | Ator autenticado | Sujeito | Autorizacao e escopo | Evento de auditoria |
| --- | --- | --- | --- | --- | --- |
| preparar revisao | autoria | professor | nenhum | `Program.{programId}.Edit`; assessment no mesmo tenant/program | `AssessmentRevisionPrepared` |
| publicar revisao | autoria | professor | nenhum | `Program.{programId}.Edit`; hash esperado e preflight aprovado | `AssessmentRevisionPublished` |
| iniciar test run | `AuthorTest` | professor | persona simulada | `Program.{programId}.Edit`; revisao do assessment | `AssessmentTestRunStarted` |
| salvar resposta de teste | `AuthorTest` | professor | subject sintetico | professor dono do test run e versao esperada | `AssessmentTestResponseSaved` |
| submeter resposta de teste | `AuthorTest` | professor | subject sintetico | professor dono do test run e versao esperada | `AssessmentTestResponseSubmitted` |
| executar review automatizado | ambos, se habilitado | worker deterministico | subject da execucao | service identity + handler/algoritmo/contexto exatos | `ReviewStageExecuted` |
| executar review por IA | ambos, se habilitado | worker externo | subject da execucao | service identity + provider/policy/contexto exatos | `ExternalReviewStageExecuted` |
| salvar draft individual | `OfficialSubmission` | aluno | proprio aluno | enrollment ativo + owner + versao esperada | `AssessmentAttemptDraftSaved` |
| salvar draft coletivo | `OfficialSubmission` | aluno | grupo congelado | ator pertence ao snapshot + versao esperada | `CollectiveAttemptDraftSaved` |
| submeter tentativa | `OfficialSubmission` | aluno | aluno ou grupo | regra de owner + idempotency key + versao esperada | `AssessmentAttemptSubmitted` |
| salvar self review | `OfficialSubmission` | aluno | aluno ou grupo | participante autorizado + evidencia em draft | `SelfReviewDraftSaved` |
| submeter self review | `OfficialSubmission` | aluno | aluno ou grupo | participante autorizado + finalizacao unica | `SelfReviewSubmitted` |
| reclamar peer review | `OfficialSubmission` | aluno revisor | submission alheia | assignment elegivel; exclui participantes do alvo | `PeerReviewClaimed` |
| submeter peer review | `OfficialSubmission` | aluno revisor | submission alheia | claim ativo do ator + versao esperada | `PeerReviewSubmitted` |
| salvar review do instrutor | ambos | professor | subject da execucao | `Program.{programId}.Review`; rodada ativa | `InstructorReviewSaved` |
| sobrescrever resultado | ambos | professor | subject da execucao | `Program.{programId}.Review`; motivo conforme policy | `GradeResultOverridden` |
| iniciar regrade | ambos | professor | subject original | `Program.{programId}.Review`; mesmos artefatos originais | `GradeRegradeStarted` |
| liberar resultado | `OfficialSubmission` | professor ou worker | subject oficial | `Program.{programId}.Review` ou service identity de release | `GradeResultReleased` |
| ler resultado retido | `OfficialSubmission` | professor | subject oficial | `Program.{programId}.Review` | `WithheldGradeResultRead` |
| ler resultado liberado | `OfficialSubmission` | aluno | proprio aluno ou grupo | enrollment + participante + release existente | acesso observavel conforme policy |

## Sujeito coletivo

- resolucao de grupo e snapshot de participantes acontecem antes do grading;
- `AutomatedReview` e `InstructorReview` recebem uma unica execucao coletiva e
  nao conhecem seus participantes;
- `SelfReview` coletivo possui uma evidencia compartilhada, editavel por um
  participante congelado ate o submit unico;
- `PeerReview` cria revisores individuais, exclui todos os membros do grupo
  alvo e agrega evidencias em um resultado da submission coletiva;
- a camada academica projeta o mesmo resultado para os participantes somente
  depois da finalizacao.

## Invariantes

1. Autorizacao ocorre antes de a idempotencia retornar um outcome.
2. Mesma chave e mesmo request hash retornam o outcome sem novo evento.
3. Mesma chave com hash diferente gera conflito.
4. Service identity nao chama comandos de autoria ou instrutor.
5. `AuthorTest` nunca cria enrollment, submission oficial, gradebook ou release.
6. Override registra ator real, valores anterior e novo e motivo exigido.
