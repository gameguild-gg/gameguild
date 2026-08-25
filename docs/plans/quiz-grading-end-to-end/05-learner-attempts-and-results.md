# 05. Tentativas e resultados do aluno

## Objetivo

Unir o player real de quiz ao lifecycle oficial de `AssessmentSubmission`, sem
expor answer keys e cobrindo resposta, autoavaliação e visualização do resultado.

Esta fase começa depois que os workflows foram exercitados pelo professor no
`AssessmentTestRun`. Ela conecta enrollment, tentativa acadêmica e identidade
real do aluno ao pipeline já implementado; não recria grading ou revisão.

## Problemas atuais

- o viewer antigo renderiza `QuizPlayer`, mas usa o fluxo genérico de interação;
- a rota oficial de assessment cria submission, mas usa textarea genérica;
- o endpoint genérico pode expor `ProgramContent.JsonBody` autoral;
- a API valida apenas se `StructuredAnswerPayload` é JSON sintaticamente válido;
- a tentativa não está vinculada claramente a uma revisão imutável;
- o aluno não vê resultado por questão no lifecycle oficial.

## Bundle learner-safe

Ao iniciar ou reabrir uma tentativa, retornar:

```ts
interface QuizAttemptBundleV1 {
  schemaVersion: 1;
  assessment: LearnerAssessmentPolicy;
  submission: LearnerAttempt;
  definitionRevisionId: string;
  quiz: QuizLearnerContentDocument;
  workflow: LearnerWorkflowSummary;
}
```

Nunca retornar:

- answer keys;
- respostas corretas ocultas;
- rubrica privada do instrutor;
- prompts privados de IA;
- resultados de outros alunos ou pares;
- JSON autoral completo.

## Submissão canônica

O `QuizPlayer` envia somente:

```ts
interface QuizStructuredSubmissionV1 {
  schemaVersion: 1;
  answers: Record<string, StructuredAnswer>;
}
```

A API valida schema, tamanho, IDs, tipos, campos permitidos, revisão, janela,
tempo e idempotência. Score e feedback não pertencem a esse payload.

## Autoavaliação

Quando o método primário for `SelfGraded`, após a submissão o aluno recebe uma
etapa separada:

```ts
interface QuizSelfEvaluationV1 {
  schemaVersion: 1;
  submissionId: string;
  items: Array<{
    contentBlockId: string;
    score: number;
    feedback?: string;
  }>;
  generalFeedback?: string;
}
```

O servidor valida autoria, limites e completude. Sem revisão docente, essa
etapa finaliza. Com `InstructorGraded`, ela produz resultado primário e entra
na fila do professor.

## Estado mostrado ao aluno

Rótulos devem refletir a etapa real:

```text
Em andamento
Enviado
Aguardando avaliação por pares
Aguardando avaliação por IA
Aguardando correção automática
Aguardando sua autoavaliação
Aguardando avaliação do instrutor
Aguardando revisão do instrutor
Avaliado
Devolvido
```

Resultado provisório não deve parecer publicado. A política de feedback decide
quando resposta correta, feedback por item e justificativa ficam visíveis.

## Tarefas

- [ ] criar endpoint idempotente de start/reopen;
- [ ] implementar projeção learner-safe no limite da API;
- [ ] retirar JSON autoral dos DTOs acessíveis ao aluno;
- [ ] renderizar `QuizPlayer` na rota oficial de activities;
- [ ] enviar `QuizStructuredSubmissionV1` para `AssessmentSubmission`;
- [ ] validar o schema completo no servidor;
- [ ] impor tempo, tentativas e janela pelo relógio do servidor;
- [ ] criar a superfície e endpoint de `SelfGraded`;
- [ ] criar review read-only usando `quiz-surface`;
- [ ] exibir estágio, score e feedback conforme a política;
- [ ] tratar reload, retry, dupla submissão e tentativa expirada;
- [ ] retirar quiz avaliado do caminho genérico `submitActivity`.

## Arquivos principais

```text
apps/web/src/app/[locale]/learn/courses/[slug]/activities/[activityId]/page.tsx
apps/web/src/components/learning/learner-activity-form.tsx
apps/web/src/components/courses/learning/activity-component.tsx
apps/web/src/lib/learner/activity-actions.ts
apps/web/src/lib/learner/activity-contracts.ts
packages/features/quiz-surface
apps/api/Source/Modules/GameGuild.Learning.Assessments/Services/AssessmentService.cs
apps/api/Source/Modules/GameGuild.Learning.Courses/Extensions/ProgramContentMappingExtensions.cs
```

## Testes de segurança

- aluno não recebe answer key por endpoint específico ou genérico;
- aluno não acessa tentativa de outro usuário;
- payload não injeta score, feedback ou resultado;
- IDs inexistentes ou de outra revisão são rejeitados;
- dupla submissão não executa o pipeline duas vezes;
- `SelfGraded` só aceita o aluno dono da tentativa;
- resultado provisório não é retornado como final.

## Critério de saída

- o player real utiliza somente `AssessmentSubmission` para quizzes avaliados;
- cada tentativa usa a mesma revisão do início ao resultado;
- o aluno completa `SelfGraded` quando configurado;
- estado, nota e feedback são exibidos sem vazar informação privada.
