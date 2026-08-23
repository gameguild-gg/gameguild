# 06. Gradebook, auditoria e operação

## Objetivo

Garantir que o resultado final seja consumido de forma consistente pelo aluno,
professor, gradebook, analytics e integrações, com histórico e operação
observáveis.

## Regra do gradebook

O gradebook considera apenas submissões cujo pipeline terminou. Ele não exige
instrutor quando `InstructorGraded` não foi configurado.

```text
workflow finalizado + grupo de peso positivo -> contribui para a nota
workflow finalizado + grupo de peso zero     -> mostra resultado, peso 0
workflow pendente                             -> não contribui ainda
sem grupo                                     -> resultado existe, sem colocação
```

Mudar grupo ou peso não altera `GradingMethods`, não cria revisão docente e não
reabre submissão. Recalcula apenas a projeção do gradebook.

## Tentativas

Definir uma política canônica por assessment:

- melhor tentativa;
- última tentativa;
- primeira tentativa;
- média, se o produto realmente precisar.

A política deve ser aplicada no servidor e reutilizada por dashboard,
gradebook, progresso e integrações.

## Resultado final

Os campos relacionais de `AssessmentSubmission` representam o último resultado
finalizado. `EvaluationPayload` preserva estágios e detalhe por item.

Regras:

- resultado primário não aparece como final quando há revisão docente;
- aprovação sem mudança é evento auditável;
- override exige motivo e registra delta;
- regrade não apaga versões anteriores;
- publicação ao aluno respeita feedback policy;
- LTI/passback usa somente resultado finalizado.

## Filas operacionais

Criar projeções por trabalho pendente:

- aguardando pares;
- aguardando IA ou retry;
- aguardando autoavaliação;
- aguardando correção docente;
- aguardando revisão docente;
- falha operacional.

SpeedGrader deve distinguir `corrigir integralmente` de `revisar resultado`.
Contadores e filtros usam o estágio corrente, não apenas `Submitted`.

## Auditoria

Eventos mínimos:

```text
AttemptStarted
SubmissionReceived
EvaluationStageStarted
EvaluationStageCompleted
EvaluationStageFailed
InstructorReviewApproved
InstructorReviewOverridden
SubmissionRegraded
ResultPublished
GradebookProjectionChanged
```

Para IA, registrar versão do modelo/prompt e evidência permitida. Para pares,
preservar atores com as regras de anonimato da visualização. Para
autoavaliação, registrar o aluno como autor. Para correção automática, registrar
versão do algoritmo e revisão da definição.

## Notificações

- professor: trabalho docente novo, fila atrasada e falha de executor;
- aluno: submissão recebida, autoavaliação pendente, resultado publicado e
  regrade;
- pares: convite, prazo e conclusão;
- não notificar nota provisória como resultado final.

## Tarefas

- [ ] implementar projeção ponderada por assessment group;
- [ ] definir e aplicar política de múltiplas tentativas;
- [ ] filtrar somente pipelines finalizados no gradebook;
- [ ] expor breakdown idêntico para professor e aluno;
- [ ] atualizar SpeedGrader e filas por estágio;
- [ ] emitir eventos de auditoria transacionais;
- [ ] implementar approve, override e regrade sem perda de histórico;
- [ ] integrar notificações e passback ao evento final;
- [ ] adicionar métricas de latência, falha e backlog por método;
- [ ] criar alertas para pipelines presos;
- [ ] remover cálculos paralelos de quiz em `ContentInteraction`.

## Testes

- peso zero não altera resultado, apenas contribuição;
- peso positivo aceita qualquer workflow finalizado;
- pipeline pendente nunca entra no total;
- troca de peso preserva workflow e histórico;
- política de tentativa é idêntica em todas as consultas;
- approve, override e regrade geram eventos diferentes;
- retries não duplicam notificação, passback ou nota;
- aluno nunca vê resultado provisório como publicado.

## Critério de saída

- uma única nota oficial alimenta todas as projeções;
- peso e workflow permanecem ortogonais;
- todo resultado é explicável por revisão, estágio, ator e evento;
- filas e métricas permitem operar os cinco métodos em produção.
