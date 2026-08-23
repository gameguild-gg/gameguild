# 03. Test run de assessment pelo professor

## Objetivo

Entregar o primeiro fluxo vertical completo usando o professor como autor,
respondente de teste e, quando configurado, revisor final. Isso permite validar
content, assessment e grading antes de construir toda a jornada acadêmica do
aluno.

O test run deve reutilizar os mesmos contratos, projeção learner-safe,
orquestrador e avaliadores do fluxo oficial. A diferença está nos efeitos
externos: teste não cria progresso, nota acadêmica ou contribuição no
gradebook.

## Três experiências distintas

| Experiência | Ownership | O que valida | Persistência acadêmica |
| --- | --- | --- | --- |
| Preview de content | content | renderização e interação autoral | nenhuma |
| Test run de assessment | assessment | resposta, workflow, grading, revisão e resultado | isolada |
| Tentativa oficial | assessment submission | jornada real do aluno e efeitos acadêmicos | oficial |

O preview de content pode usar o estado autoral para conferir apresentação. O
test run exige uma definição salva e usa a projeção que um aluno receberia. Ele
não deve receber o JSON autoral diretamente.

## Relação com o dry-run anterior

O plano anterior
[`learning-grading-part-4-authoring-preview-and-dry-run.md`](../learning-grading-part-4-authoring-preview-and-dry-run.md)
propõe um endpoint stateless para testar correção determinística. Esse endpoint
continua útil como primitiva de diagnóstico, mas não fecha:

- workflows assíncronos;
- `SelfGraded`;
- múltiplas avaliações de pares;
- revisão final `InstructorGraded`;
- reload e retomada;
- transições e resultado de cada estágio.

O `Assessment Test Run` amplia esse conceito para uma execução isolada e
persistida do pipeline.

## Por que não usar `AssessmentSubmission`

`AssessmentSubmission` representa tentativa acadêmica e exige conceitos como
aluno, enrollment, tentativa, progresso e gradebook. Usá-la para o professor
exigiria matrícula falsa ou um `IsTest` que todas as consultas precisariam
lembrar de filtrar.

Direção recomendada: um agregado próprio `AssessmentTestRun`, sem
`EnrollmentId`, `CourseGroupId` ou efeitos acadêmicos. O orquestrador trabalha
contra um contexto comum de execução, não diretamente contra uma tabela:

```text
IGradingExecutionContext
  OfficialAssessmentSubmissionContext
  AuthorAssessmentTestRunContext
```

Essa separação evita duplicar regras e evita contaminar o modelo oficial.

## Persistência mínima proposta

Uma única estrutura operacional, sujeita à decisão de schema da Fase 1:

```text
AssessmentTestRun
  Id
  AssessmentId
  DefinitionRevisionId
  AuthorId
  Status
  WorkflowSnapshot
  StructuredAnswerPayload
  EvaluationPayload
  CreatedAt
  UpdatedAt
  ExpiresAt
```

Payloads podem permanecer em `jsonb` versionado. Não criar tabelas por item,
peer sintético ou estágio nesta primeira implementação.

Uma execução pode expirar e ser removida por retenção. Ela não entra no audit
log acadêmico; eventos de diagnóstico podem ser mantidos no próprio payload ou
em logs operacionais correlacionados pelo `testRunId`.

## Garantias de isolamento

Um contexto `AuthorTest` deve bloquear por construção:

- criação de `AssessmentSubmission`;
- consumo de tentativas do aluno;
- progresso e conclusão de conteúdo;
- gradebook e cálculo de nota do curso;
- notificações acadêmicas;
- LTI/passback;
- analytics de aprendizagem;
- distribuição real para pares.

O resultado é confiável como teste do avaliador, mas não é uma nota acadêmica.

## Fluxo da tela

Adicionar `Testar assessment` como ação principal no editor de assessment. A
execução deve abrir uma rota ou superfície full-screen focada na tarefa:

```text
1. Criar ou reiniciar test run
2. Responder como aluno
3. Enviar respostas
4. Executar o avaliador primário configurado
5. Revisar como instrutor, quando configurado
6. Inspecionar resultado, estágios e diagnósticos
```

A tela deve mostrar permanentemente:

- badge `Modo de teste`;
- revisão e workflow usados;
- estágio corrente;
- ação para reiniciar;
- indicação de que não haverá gradebook ou progresso.

Não exibir textos instrucionais genéricos dentro do produto. O estado de teste,
os nomes das etapas e os comandos devem tornar o fluxo autoexplicativo.

## Comportamento por método

### `InstructorGraded`

O professor responde em persona de aluno e depois alterna para a etapa de
correção, usando a mesma superfície prevista para o SpeedGrader.

### `AutoGraded`

Ao enviar, o servidor executa o avaliador determinístico e mostra o resultado.
Com revisão docente, a tela avança para aprovação ou override.

### `SelfGraded`

Depois de responder, o professor permanece na persona de aluno e preenche a
autoavaliação. Com revisão docente, alterna depois para revisor.

### `PeerReview`

O test run cria personas sintéticas locais ao teste, como `Peer 1` e `Peer 2`,
para exercitar formulário e consolidação sem usar contas reais. Isso valida o
algoritmo de combinação, mas não substitui os testes posteriores de elegibilidade
e distribuição entre alunos reais.

### `AIGraded`

O teste usa o mesmo provider configurado. A tela deve mostrar custo/execução e
falhas operacionais sem transformar erro em nota. Um provider fake pode ser
usado apenas em testes automatizados e ambientes sem credencial.

## API-alvo

Casos de uso equivalentes a:

```text
POST   /assessments/{assessmentId}/test-runs
GET    /assessment-test-runs/{testRunId}
POST   /assessment-test-runs/{testRunId}/submit
POST   /assessment-test-runs/{testRunId}/self-grade
POST   /assessment-test-runs/{testRunId}/peer-grade
POST   /assessment-test-runs/{testRunId}/instructor-review
POST   /assessment-test-runs/{testRunId}/restart
DELETE /assessment-test-runs/{testRunId}
```

Todos exigem permissão de gestão no assessment. O cliente nunca escolhe outro
workflow ou revisão depois que o test run foi criado.

## Tarefas

- [ ] fechar o contrato `IGradingExecutionContext`;
- [ ] decidir e revisar a persistência de `AssessmentTestRun` antes da migration;
- [ ] criar test run vinculado a uma revisão salva;
- [ ] reutilizar a projeção learner-safe;
- [ ] renderizar `QuizPlayer` em persona de aluno;
- [ ] submeter respostas estruturadas pelo orquestrador comum;
- [ ] implementar alternância de persona por estágio;
- [ ] reutilizar a superfície de revisão docente;
- [ ] criar peers sintéticos isolados;
- [ ] bloquear todos os efeitos acadêmicos no contexto de teste;
- [ ] permitir restart e retenção/limpeza;
- [ ] exibir resultado por item, transições e diagnósticos.

## Testes

- test run não cria enrollment nem submission oficial;
- test run não altera gradebook, progresso, notificação ou passback;
- answer key não aparece no payload da persona de aluno;
- resultado determinístico é idêntico ao executor oficial;
- `SelfGraded` registra a persona respondente;
- peers sintéticos não aparecem em filas reais;
- revisão docente preserva resultado anterior e override;
- reload retoma o estágio correto;
- restart cria uma execução limpa;
- autor sem permissão não acessa test run.

## Critério de saída

- professor percorre content -> assessment -> resposta -> grading -> revisão ->
  resultado sem depender de aluno matriculado;
- o pipeline utilizado é o mesmo que será chamado pela submissão oficial;
- nenhum efeito acadêmico é produzido;
- os nove workflows podem ser exercitados nessa superfície antes da jornada do
  aluno ser implementada.
