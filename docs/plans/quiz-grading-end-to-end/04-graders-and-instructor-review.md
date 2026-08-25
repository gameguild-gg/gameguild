# 04. Avaliadores e revisão do instrutor

## Objetivo

Implementar os cinco métodos como executores do mesmo contrato de estágio e
fazer `InstructorGraded` funcionar tanto como avaliador único quanto como
revisor final.

Cada executor deve ser integrado primeiro ao `AssessmentTestRun`. Depois, o
mesmo orquestrador será chamado por `AssessmentSubmission`, sem implementação
paralela para o aluno.

## Contrato comum

Cada executor recebe uma revisão imutável e produz `QuizGradeResultV1`:

```text
Grade(revision, structuredAnswers, policy, context)
  -> item results
  -> aggregate score
  -> evidence and feedback
  -> evaluator identity/version
```

O orquestrador, não o executor, decide se o resultado é final ou deve seguir
para o instrutor.

## `InstructorGraded`

Prioridade de entrega: primeira.

- sozinho: submission entra diretamente na fila docente;
- combinado: recebe resultado primário completo;
- professor aprova sem alteração ou altera por item;
- override exige motivo;
- total é consolidado no servidor;
- regrade posterior preserva histórico.

O SpeedGrader deve mostrar questão, resposta, revisão, resultado anterior,
rubrica e feedback, não IDs e JSON crus.

## `AutoGraded`

É correção determinística pelo sistema:

- usa answer key e regras puras;
- deve ser reproduzível e idempotente;
- roda exclusivamente no limite confiável do servidor;
- TypeScript e C# devem compartilhar fixtures de conformidade;
- sem instrutor, finaliza ao concluir;
- com instrutor, gera resultado primário para revisão.

O package `@game-guild/grading` é referência de contrato e algoritmo. O browser
pode executar preview não oficial, mas nunca publica a nota.

## `SelfGraded`

- o aluno envia score e feedback por contrato específico;
- servidor valida limites, autoria e itens;
- resultado identifica o aluno como avaliador;
- sem instrutor, finaliza;
- com instrutor, entra na fila de revisão.

Autoavaliação não deve reutilizar o payload de respostas nem o endpoint de
correção docente.

## `PeerReview`

Reaproveitar a infraestrutura existente e completar:

- critérios de elegibilidade;
- distribuição idempotente;
- anonimato;
- quantidade mínima;
- conflitos e recusas;
- consolidação de múltiplos scores;
- prazo e fallback;
- revisão final opcional do instrutor.

## `AIGraded`

- executor server-side isolado atrás de interface;
- modelo, prompt, rubric e versão registrados;
- resposta estruturada validada, sem aceitar texto livre como resultado final;
- timeout, retry e idempotência;
- falha não deve virar nota zero silenciosamente;
- custo e observabilidade por execução;
- sem instrutor, finaliza conforme a política escolhida;
- com instrutor, resultado permanece provisório.

O avaliador determinístico deve ser usado para itens que possuem regra exata;
IA não deve substituir uma correção mais reproduzível sem motivo.

## Orquestrador

Responsabilidades:

1. carregar snapshot e workflow da tentativa;
2. autorizar o ator ou serviço;
3. adquirir lock/idempotency key;
4. iniciar o estágio correto;
5. validar e persistir o resultado;
6. avançar para `InstructorGraded` ou finalizar;
7. projetar nota final;
8. emitir auditoria e notificações.

Não usar uma cadeia de `if` espalhada por controllers. Criar uma fronteira de
aplicação explícita para resolução e execução de estágio.

## Tarefas

- [ ] fechar interface e registry de executores;
- [ ] implementar orquestrador e transições idempotentes;
- [ ] concluir primeiro o fluxo `InstructorGraded` isolado;
- [ ] implementar `AutoGraded` com fixtures cross-language;
- [ ] implementar `SelfGraded` com autorização do aluno;
- [ ] completar `PeerReview` e sua consolidação;
- [ ] implementar `AIGraded` atrás de provider configurável;
- [ ] implementar as quatro combinações com revisão docente;
- [ ] permitir approve, override e regrade com motivo;
- [ ] impedir execução por ator ou método não configurado;
- [ ] criar filas e retries sem duplicar efeitos.

## Testes por executor

- resultado feliz com e sem instrutor;
- erro recuperável e erro definitivo;
- retry idempotente;
- edição posterior da definição não altera a tentativa;
- score agregado equivale aos itens;
- ator sem permissão é rejeitado;
- conclusão fora de ordem é rejeitada;
- override e regrade preservam o resultado anterior.

## Critério de saída

- os cinco métodos produzem o mesmo contrato de resultado;
- os nove workflows chegam a um único estado final coerente;
- `InstructorGraded` combinado sempre ocorre por último;
- nenhuma nota oficial depende de cálculo confiado ao browser.
