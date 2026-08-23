# 01. Contratos e persistência

## Objetivo

Definir contratos versionados e a menor evolução de persistência capaz de
suportar revisão imutável, resultados por estágio, nota final e auditoria sem
criar tabelas por questão prematuramente.

## Persistência atual

`Assessment` já possui:

- `GradingMethods` como inteiro bitmask;
- grupo, score máximo, tentativas, tempo e apresentação;
- `DefinitionPayload` em `jsonb` e versão de schema.

`AssessmentSubmission` já possui:

- identificação da tentativa e aluno;
- payload estruturado em `jsonb`;
- `Score`, `Passed`, `Feedback`, `GradedBy` e `GradedAt`;
- apenas os estados `InProgress`, `Submitted`, `Graded`, `Returned` e `Late`.

Ela não possui resultado por item, estágios, versões de avaliador ou snapshot
imutável explicitamente vinculado à tentativa.

## Decisão de schema para `GradingMethods`

Adicionar `SelfGraded = 16` não exige coluna nem tabela. Exige alterar a
constraint existente.

Constraint-alvo:

```sql
"GradingMethods" IN (0, 1, 2, 4, 8, 9, 10, 12, 16, 24)
```

O domínio rejeita `0` ao publicar. O banco pode mantê-lo para drafts.

Política de migration:

- não renumerar valores existentes;
- não editar uma migration já aplicada em ambiente compartilhado;
- preferir migration forward-only que remova e recrie apenas a constraint;
- não adicionar tabela de métodos;
- não criar migração de dados legacy.

## Revisão imutável

Cada tentativa precisa apontar para a definição usada quando foi iniciada. A
revisão deve conter:

- documento completo autorizado para correção;
- projeção learner-safe;
- answer keys e rubricas;
- configuração de pontos;
- `GradingMethods` e políticas vigentes;
- hash e versão do schema.

Antes da implementação deve ser escrita uma decisão de persistência comparando:

1. revisão própria de content referenciada por ID;
2. snapshot versionado dentro de `Assessment.DefinitionPayload`;
3. entidade genérica de revisão já existente no módulo de Learning, se houver.

Não criar uma tabela nova sem provar que nenhuma revisão já existente atende a
identidade, retenção e consulta necessárias.

## Resultado operacional

Proposta mínima para estado corrente:

```text
AssessmentSubmissions.EvaluationPayload jsonb nullable
```

Contrato inicial:

```ts
interface AssessmentEvaluationV1 {
  schemaVersion: 1;
  definitionRevisionId: string;
  configuredMethods: GradingMethod[];
  currentStage: GradingMethod | null;
  stages: EvaluationStageV1[];
  finalResult: QuizGradeResultV1 | null;
  finalizedAt?: string;
}

interface EvaluationStageV1 {
  method: GradingMethod;
  status: "pending" | "running" | "completed" | "failed";
  evaluatorVersion?: string;
  actorIds?: string[];
  result?: QuizGradeResultV1;
  startedAt?: string;
  completedAt?: string;
}
```

`Score`, `Passed`, `Feedback`, `GradedBy` e `GradedAt` continuam como projeção
relacional do resultado final para consultas comuns. Eles não substituem o
detalhamento do pipeline.

## Execução oficial e execução de teste

O contrato de pipeline deve ser compartilhado, mas a persistência acadêmica
não. `AssessmentSubmission` continua exclusiva de aluno/enrollment. O test run
do professor usa um agregado `AssessmentTestRun` ou estrutura equivalente, com
retenção curta e sem participação em gradebook, progresso ou filas acadêmicas.

Não adicionar `IsTest` a `AssessmentSubmission`: essa alternativa exige filtros
em todas as consultas e permite que uma omissão contamine resultados oficiais.
Antes de criar `AssessmentTestRun`, registrar a justificativa de schema e
confirmar que não existe agregado genérico de execução que já atenda ao caso.

## Resultado de quiz

`QuizGradeResultV1` deve ser separado da resposta do aluno e incluir:

- ID estável de cada bloco;
- score e score máximo por item;
- estado do item;
- feedback;
- evidência ou justificativa permitida para o método;
- versão do avaliador;
- origem do resultado e overrides.

O payload de respostas nunca aceita score, answer key ou resultado de grading.
`SelfGraded` usa um endpoint e contrato próprios para a autoavaliação.

## Status operacional

Adicionar valores ao enum existente somente depois de decidir quais estados
precisam ser consultados e indexados. Candidatos:

```text
AwaitingPrimaryGrading
AwaitingInstructorReview
GradingFailed
```

O método primário específico pode ficar em `EvaluationPayload`; filas de alto
volume podem exigir projeção/indexação posterior. Não criar cinco colunas ou
cinco tabelas de fila antecipadamente.

## Auditoria

Usar `AuditLogs` para eventos imutáveis, sem tratá-lo como estado corrente:

```text
EvaluationStageStarted
EvaluationStageCompleted
EvaluationStageFailed
InstructorReviewApproved
InstructorReviewOverridden
SubmissionRegraded
ResultPublished
```

Cada evento deve carregar submission, revisão, método, ator, versão do
avaliador, resultado anterior, resultado novo e motivo quando aplicável.

## Tarefas

- [ ] adicionar `SelfGraded` aos contratos C# e TypeScript;
- [ ] normalizar serialização para nomes canônicos e ordem canônica;
- [ ] decidir e documentar a persistência da revisão imutável;
- [ ] fechar schemas JSON de tentativa, resposta, estágio e resultado;
- [ ] adicionar validação runtime equivalente em API e packages;
- [ ] decidir `EvaluationPayload` por ADR antes da migration;
- [ ] decidir a persistência isolada de `AssessmentTestRun` no mesmo ADR;
- [ ] projetar campos finais relacionais a partir do último estágio;
- [ ] anexar novos valores ao `SubmissionStatus` somente se necessários;
- [ ] alterar a constraint de `GradingMethods` sem nova coluna;
- [ ] adicionar testes EF, migration, round-trip JSON e concorrência.

## Critério de saída

- uma tentativa continua corrigível após edição do quiz;
- resposta, avaliação e resultado final são evidências separadas;
- os nove workflows fazem round-trip sem perder método ou ordem semântica;
- o estado corrente não depende de reconstruir `AuditLogs`;
- nenhuma alteração de schema existe sem ownership e consulta documentados.
