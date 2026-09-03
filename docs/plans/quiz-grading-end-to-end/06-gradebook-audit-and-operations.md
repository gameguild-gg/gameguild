# 06. Gradebook, auditoria e operação

## Objetivo

Garantir que o resultado final seja consumido de forma consistente pelo aluno,
professor, gradebook, analytics e integrações, com histórico e operação
observáveis.

## Regra do gradebook

O gradebook considera apenas submissões cujo pipeline terminou. Ele não exige
instrutor quando `InstructorReview` não foi configurado.

```text
workflow finalizado + grupo de peso positivo -> contribui para a nota
workflow finalizado + grupo de peso zero     -> mostra resultado, peso 0
workflow pendente                             -> não contribui ainda
sem grupo                                     -> resultado existe, sem colocação
```

`Finalized` é o gatilho do gradebook. `Released` é o gatilho de visibilidade e
notificação ao aluno. Um resultado finalizado e retido pela policy já pode
participar do gradebook interno. A projeção learner do gradebook usa somente
rodadas liberadas e não retorna total, percentual, denominador, breakdown ou
`FinalGrade` que permita inferir uma contribuição retida.

Quando há mais de uma tentativa, finalização sozinha não define a contribuição.
O consumer aplica uma única policy canônica do assessment. No corte inicial,
ela seleciona uma submission; trocar a seleção atualiza a projeção anterior
idempotentemente. Uma eventual média deve ser um modo explícito de agregação e
nunca a soma acidental de todas as finalizações.

Depois dessa seleção, a fórmula canônica inicial é única:

```text
groupRatio        = sum(effectiveScore) / sum(capturedMaxScore)
groupContribution = groupRatio * AssessmentGroup.WeightPercent
coursePercent     = sum(groupContribution)
```

Cada assessment fornece no máximo uma contribuição efetiva finalizada. Dentro
do grupo, a agregação é por pontos; não se calcula uma média simples dos
percentuais dos assessments. Grupo sem contribuição ainda produz estado parcial,
sem redistribuir seu peso para outros grupos. Grupo de peso zero e assessment sem
grupo não entram na soma. Grupo sem denominador positivo não produz contribuição
nem resultado global oficial. A configuração pode permanecer incompleta durante
a autoria, mas os grupos de peso positivo precisam totalizar exatamente `100%`
antes de `Program.PassingScore` produzir resultado global oficial do curso.
Nenhum consumer normaliza pesos silenciosamente.

Mudar grupo ou peso não altera `ReviewMethods`, não cria revisão docente e não
reabre submissão. Um caso de uso autorizado registra ator, antes/depois e um
evento canônico; o consumer recalcula apenas a projeção do gradebook de forma
idempotente. Remover o grupo retira a colocação, peso zero mantém a atividade
sem contribuição e peso positivo aplica a contribuição efetiva. Nenhuma dessas
transições cria round, regrade, release ou evidência de review.

Em `GroupAssignment`, existe um único resultado coletivo. O consumer de
`GradeResultFinalized` consulta o snapshot de participantes da submission e
cria ou atualiza uma projeção por enrollment. A chave
`(SubmissionId, GradeRoundId, EnrollmentId)` impede duplicação. Regrade atualiza
essas projeções a partir da nova rodada sem executar grading por integrante.

`AssessmentGroup.WeightPercent` e `Program.PassingScore` usam `PercentValue`
canônico em string. Scores e contribuições usam `ScoreValue`. Aritmética e
arredondamento ocorrem na API; projeções agregadas são precomputadas e também
persistidas como strings de largura fixa. Consultas podem ordenar essas strings,
mas não usam `SUM`, `AVG` nem cast para `numeric`. A API mantém aritmética exata
durante a agregação e quantiza uma única vez ao produzir cada projeção canônica;
assessment, grupo e curso compartilham os mesmos vetores de conformidade.

`Assessment.PassingScore` decide `Passed` para aquela submissão, em pontos
absolutos da revisão publicada. `Program.PassingScore` é percentual e decide o
resultado global do curso depois da soma ponderada. Nenhuma projeção pode usar
um campo no lugar do outro.

O baseline do núcleo em `SEQ-03` converte `Program.PassingScore`,
`AssessmentGroup.WeightPercent` e os demais campos acadêmicos existentes para
os value objects textuais antes de qualquer test run. O primeiro E2E usa
`Assessment.PassingScore` para a submission e `AssessmentGroup.WeightPercent`
para sua contribuição mínima; ele não consulta `Program.PassingScore`. Somente
`SEQ-15` aplica esse percentual à consolidação global do curso. Nenhuma fase
posterior pode reintroduzir conversão tardia, `decimal` persistido ou cast de
ponto flutuante.

## Tentativas

Definir uma política canônica de contribuição por assessment, com o primeiro
modo aprovado no ADR antes do primeiro E2E oficial:

- melhor tentativa;
- última tentativa;
- primeira tentativa;

Esses três modos selecionam uma submission. Média, se o produto realmente
precisar, é um modo avançado de agregação e precisa definir conjunto elegível,
arredondamento, regrade e tratamento de tentativas pendentes antes de entrar no
contrato.

A política deve ser aplicada no servidor e reutilizada por dashboard,
gradebook e integrações de score para escolher a mesma contribuição efetiva.
`AssessmentContentCompletionPolicyV1` decide separadamente em qual transição o
content progride e como múltiplas tentativas afetam esse estado. `maxAttempts >
1` só pode ser salvo, publicado e iniciado quando ao menos uma política de
contribuição estiver implementada de ponta a ponta. Caso contrário, o primeiro
corte limita `maxAttempts` a `1`. `SEQ-15` pode acrescentar políticas avançadas,
mas não inaugura a regra que o gradebook já precisava em `SEQ-10`.

## Resultado final

`GradingExecution` é a fonte de stages, rodadas, evidências e resultados. Os
campos relacionais de `AssessmentSubmission`, caso mantidos, são apenas a
projeção da rodada oficial ativa finalizada; eles não armazenam um segundo
histórico autoritativo.

Regras:

- resultado primário não aparece como final quando há revisão docente;
- aprovação sem mudança é evento auditável;
- override exige motivo e registra delta;
- regrade não apaga versões anteriores;
- regrade cria `activeRoundId` novo e aponta para a rodada substituída;
- regrade usa a mesma revisão, manifest, entrega e respostas da execução; avaliar
  definição diferente cria nova submission e nova execução;
- somente submission oficial aplica a policy de liberação ao aluno;
- LTI/passback e gradebook usam somente resultado finalizado;
- learner result e sua notificação usam somente resultado liberado.

`AssessmentTestRun` não possui policy, estado, linha ou evento de release
acadêmico.

## Liberação explícita

Liberação retida ou manual passa pelo comando `ReleaseGradeResult`. O comando
recebe submission, rodada esperada, versão de concorrência e idempotency key;
autoriza o instrutor no assessment e rejeita uma rodada já substituída por
regrade. Na liberação imediata, a transação de finalização persiste uma
solicitação durável e idempotente; um worker/dispatcher usa o mesmo caso de uso
com identidade de serviço da policy, em vez de alterar o estado diretamente no
consumer de finalização. Queda entre commit e dispatch conserva a solicitação e
o retry não duplica a transição.

Uma transição válida grava auditoria e um único `GradeResultReleased` na
outbox. Retry idêntico é inócuo; a mesma chave com payload divergente falha.
Nenhum endpoint de liberação pode fabricar ou recalcular `GradeResult`.

Release é persistido por rodada. Se uma rodada já liberada entrar em regrade,
ela continua sendo a última visão learner enquanto a substituta estiver
pendente, `Withheld` ou `Scheduled`. A contribuição interna troca para a rodada
nova quando ela finaliza; a visão learner troca somente quando ela é liberada.
Retirar uma nota publicada exige comando explícito e auditado, não é efeito
implícito de iniciar regrade.

Release agendado usa o modo `scheduled` reservado em
`AssessmentResultReleasePolicyV1`. `ScheduleGradeResultRelease` e
`CancelScheduledGradeResultRelease` possuem autorização, rodada/versão
esperadas, idempotência e auditoria. `ScheduledFor` é UTC. O worker usa
`TimeProvider`, seleciona apenas linhas vencidas e chama `ReleaseGradeResult`
com identidade de serviço; não grava `Released` diretamente. Rodada finalizada
depois do horário é liberada imediatamente pelo mesmo comando. Regrade aplica a
policy por rodada sem alterar agendamentos ou releases anteriores.

## Filas operacionais

Criar projeções por trabalho pendente:

- aguardando reviews de alunos;
- reviews de alunos insuficientes e aguardando intervenção;
- aguardando provider de IA ou retry;
- aguardando autoavaliação;
- aguardando correção docente;
- aguardando revisão docente;
- aguardando correção automática ou retry;
- falha operacional.

SpeedGrader deve distinguir `corrigir integralmente` de `revisar resultado`.
Contadores e filtros usam o estágio corrente, não apenas `Submitted`.
No primeiro E2E oficial, a fila mínima e o SpeedGrader já devem operar sobre o
stage/round canônico e produzir evidência, approve, override ou regrade pelos
comandos do runtime. O endpoint e service anteriores que atribuem score
diretamente são removidos nesse mesmo corte; esta seção amplia filtros e
operação, não adia a troca de autoridade.

## Auditoria

O histórico acadêmico transacional vive nas rodadas da `GradingExecution`
associada à submission oficial. A finalização e as mensagens de outbox são
gravadas na mesma transação. `Score` e `Passed` da submission, se preservados,
são projeções; `AuditLogs` é uma projeção para consulta de compliance, não a
fonte usada para reconstruir a nota.

Eventos mínimos:

```text
AttemptStarted
SubmissionReceived
ReviewStageStarted
ReviewStageCompleted
ReviewStageFailed
InstructorReviewApproved
InstructorReviewOverridden
SubmissionRegraded
GradeResultFinalized
GradeResultReleased
GradebookProjectionChanged
```

Para `AutomatedReview`, registrar versão do algoritmo e ID do snapshot da
definição.
Para `AIReview`, registrar provider, policy e versão, sem segredos. Para
`PeerReview`, preservar cada aluno revisor respeitando anonimato na projeção.
Para `SelfReview`, registrar o aluno como autor.

Cada mensagem de outbox possui ID estável, aggregate ID, round ID, sequence,
payload versionado, rota de consumers obrigatórios e timestamps. Cada consumer
possui `ConsumerKey` estável e confirmação durável/deduplicada por
`(EventId, ConsumerKey)`. A mensagem só encerra o dispatch quando todos os
consumers capturados confirmarem. Falha do `AuditService` deixa apenas sua
entrega pendente; não apaga o evento, não remove receipts dos demais e não
reverte silenciosamente a evidência acadêmica.

Adicionar um consumer depois não reproduz o histórico implicitamente. Replay é
uma operação explícita, autorizada e auditada. Consumers introduzidos em
`SEQ-15` entram na rota dos eventos novos a partir da habilitação.

Eventos acadêmicos duráveis são convertidos em registros de outbox antes do
commit e excluídos do dispatch direto feito pelo `IPublisher` em
`ApplicationDbContext.SaveChangesAsync`. Um mesmo evento nunca percorre os dois
caminhos.

## Notificações

- professor: trabalho docente novo, fila atrasada e falha de estágio/provider;
- aluno: submissão recebida, review próprio ou entre alunos pendente, resultado
  publicado e regrade;
- não notificar nota provisória como resultado final.

## Tarefas

- [ ] implementar a fórmula canônica por pontos dentro do assessment group e
  pelos pesos entre grupos, sem média de percentuais ou renormalização;
- [ ] implementar reprojeção idempotente e auditada quando assessment muda de
  grupo, fica sem grupo ou quando o peso do grupo muda;
- [ ] projetar resultado coletivo para os participantes congelados, sem criar
  submissions individuais;
- [ ] definir e aplicar política de contribuição de múltiplas tentativas;
- [ ] bloquear `maxAttempts > 1` até uma policy canônica estar disponível em
  todos os consumers;
- [ ] filtrar somente pipelines finalizados no gradebook;
- [ ] separar consumers de `GradeResultFinalized` e `GradeResultReleased`;
- [ ] separar projeção interna finalizada de toda projeção learner liberada e
  revisar dashboard, workspace, DTOs, mappers, clients e agregados do curso;
- [ ] implementar `ReleaseGradeResult` para liberação automática e manual com
  autorização, round esperado, concorrência e idempotência;
- [ ] persistir a solicitação de release imediato na transação de finalização e
  processá-la pelo mesmo comando idempotente;
- [ ] implementar schedule, cancelamento/reagendamento e worker de release sobre
  o mesmo comando de liberação;
- [ ] manter projeções agregadas de gradebook sem aritmética decimal no banco;
- [ ] expor ao aluno o breakdown da última rodada liberada; professor pode ver a
  rodada interna ativa e o histórico completo;
- [ ] atualizar SpeedGrader e filas por estágio;
- [ ] emitir eventos de auditoria transacionais;
- [ ] persistir rodadas de grading como fonte do histórico acadêmico;
- [ ] manter `AssessmentSubmission` apenas como projeção do resultado oficial
  ativo, sem duplicar o histórico de `GradingExecution`;
- [ ] implementar outbox durável e consumers idempotentes;
- [ ] persistir confirmação por `(EventId, ConsumerKey)` e concluir a mensagem
  somente depois de todas as entregas obrigatórias;
- [ ] tratar `AuditLogs` como projeção refeita a partir da outbox;
- [ ] implementar approve, override e regrade sem perda de histórico;
- [ ] em `SEQ-15`, integrar notificações e passback aos eventos canônicos; a
  Parte 2 apenas remove produtores diretos e persiste esses eventos na outbox;
- [ ] adicionar métricas de latência, falha e backlog por método;
- [ ] criar alertas para pipelines presos;
- [ ] remover no primeiro E2E os cálculos, submits e efeitos acadêmicos
  paralelos de quiz em `ContentInteraction` e `ActivityGrade`.
- [ ] bloquear complete/update progress e escrita direta de `ActivityGrade` para
  quiz avaliado, deixando somente a projeção canônica atualizar o read model de
  progresso conforme a policy de conclusão.

## Testes

- peso zero não altera resultado, apenas contribuição;
- assessments com escalas distintas no mesmo grupo usam soma de score sobre soma
  de `MaxScore`; consumers não calculam média de percentuais nem renormalizam
  pesos;
- `AssessmentSubmission.Passed` usa somente `Assessment.PassingScore`, enquanto
  `Program.PassingScore` só participa da consolidação global em `SEQ-15`;
- peso positivo aceita qualquer workflow finalizado;
- pipeline pendente nunca entra no total;
- resultado finalizado entra no gradebook mesmo quando a liberação ao aluno
  está retida;
- gradebook learner, dashboard, workspace e `FinalGrade` não incorporam nem
  permitem inferir resultado retido;
- resultado retido não dispara notificação nem endpoint de nota do aluno;
- troca de peso preserva workflow e histórico;
- assessment sem grupo mantém resultado sem colocação no gradebook;
- troca de grupo/peso reprojeta uma vez, registra antes/depois e não cria round,
  regrade, release ou evidência;
- resultado de grupo é calculado uma vez e projetado uma vez por participante;
- retry da projeção coletiva não duplica nota, progresso ou notificação;
- política de contribuição é idêntica em todas as consultas;
- múltiplas finalizações produzem exatamente uma contribuição efetiva no
  gradebook, ou continuam bloqueadas quando a policy não foi entregue;
- release manual sem permissão ou contra round obsoleto falha, e retry idêntico
  não duplica evento, projeção ou notificação;
- queda após finalizar e antes de despachar preserva a solicitação de release
  imediato e termina com exatamente um `GradeResultReleased`;
- schedule, cancelamento, reagendamento e execução vencida são idempotentes;
  worker nunca libera rodada futura nem duplica `GradeResultReleased`;
- approve, override e regrade geram eventos diferentes;
- rodada anterior permanece consultável depois de regrade;
- a última rodada liberada continua learner-visible durante regrade e só é
  substituída pelo release da nova rodada;
- falha temporária de audit, notificação ou passback mantém a entrega daquele
  `ConsumerKey` pendente para retry;
- crash depois do commit e antes do dispatch não perde evento, e retry não
  duplica o efeito;
- evento acadêmico durável não é publicado simultaneamente pela outbox e pelo
  dispatcher em processo;
- retries não duplicam notificação, passback ou nota;
- falha de um consumer não repete efeitos confirmados nem marca como concluídas
  as entregas ainda pendentes;
- aluno nunca vê resultado provisório como publicado.

## Critério de saída

- uma única nota oficial finalizada alimenta as projeções acadêmicas;
- uma transição explícita de liberação controla as projeções do aluno;
- release por rodada preserva a última visão learner durante regrade;
- uma única policy canônica determina a contribuição usada por todas as
  projeções;
- uma única fórmula canônica determina a agregação por grupo e curso;
- a nota oficial e seu histórico não dependem do `AuditService` best-effort;
- peso e workflow permanecem ortogonais;
- todo resultado é explicável por método de review, estágio, ator e evento;
- filas e métricas permitem operar os cinco reviews;
- indisponibilidade de provider é distinguida de resultado acadêmico.
