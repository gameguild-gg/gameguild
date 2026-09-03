# Parte 3. Expansão de reviews e operação

## Objetivo

Expandir o E2E principal com `SelfReview`, `PeerReview`, a porta durável de
`AIReview` e a operação acadêmica avançada. Esta parte contém `SEQ-12` a
`SEQ-16` e não redefine os contratos centrais aprovados nas partes anteriores.

Regras globais: [`08-implementation-sequence.md`](../08-implementation-sequence.md).

## Pré-requisitos

- Partes 1 e 2 concluídas, testadas e aprovadas;
- fluxo oficial individual e coletivo sem autoridade paralela;
- gradebook mínimo, release e auditoria básica funcionando de forma
  idempotente.

## Fora do escopo

- reabrir decisões centrais sem novo ADR e avaliação de impacto;
- substituir o runtime entregue na Parte 2 por implementações por método;
- disponibilizar `AIReview` em produção sem provider real aprovado.

## `SEQ-12`. Completar `SelfReview`

### Resultado

`SelfReview` é validado no test run e em submissions oficiais individuais e
coletivas. Somente ao final deste marco ele é considerado workflow completo.

### `SCHEMA-GATE` de SelfReview

Antes de alterar persistência, provar se o modelo genérico de evidências já
garante:

- uma evidência por execução, round e método;
- draft compartilhado com versão de concorrência para sujeito coletivo;
- um único submit final atômico;
- trilha append-only de cada mutação aceita do draft com ator, versões
  anterior e nova, request hash e instante;
- deduplicação durável de save e submit por escopo, idempotency key e request
  hash, com outcome persistido e sem evento duplicado em replay;
- ator, instante e versão registrados na finalização;
- imutabilidade da evidência finalizada.

Se o núcleo atender integralmente, registrar o gate com delta relacional zero.
Se não atender, apresentar tabelas, colunas, constraints e índices necessários
e obter aprovação antes de editar o baseline global e recriar os bancos
descartáveis. Não criar migration incremental.

### Implementação

- criar contrato próprio de autoavaliação por item;
- validar ator, sujeito, escala, limites e momento de envio;
- manter resposta acadêmica e evidência de self review separadas;
- produzir resultado direto ou encaminhar ao instrutor;
- representar a persona no test run sem confundi-la com o ator autenticado;
- registrar inicialmente somente a capability `AuthorTest` e concluir os E2Es
  de test run direto e combinado com instrutor;
- conectar o mesmo handler à tentativa oficial individual e coletiva; somente
  depois de autorização, efeitos acadêmicos e ambos os E2Es oficiais passarem,
  registrar `OfficialSubmission` na configuração de produção e permitir
  publish. Os E2Es usam registro oficial controlado no ambiente de teste, sem
  antecipar essa promoção;
- para grupo, manter uma única evidência compartilhada por round;
- permitir que qualquer participante do snapshot edite o draft coletivo com
  concorrência otimista;
- implementar `SaveCollectiveSelfReviewDraftV1(expectedVersion,
  idempotencyKey, requestHash)` sobre `IdempotentCommandEnvelopeV1`; cada save
  aceito grava na mesma transação um evento append-only e replay idêntico
  reutiliza o outcome sem duplicá-lo;
- aceitar um único submit final da evidência coletiva e registrar o ator real;
- estender a surface compartilhada de review para `SelfReview`, reutilizando o
  mesmo componente nos contextos `AuthorTest` e `OfficialSubmission`, sem criar
  um runtime ou formulário divergente por contexto;
- implementar na surface carregamento e resume da evidência individual ou
  coletiva, save versionado, conflito por `expectedVersion`, retry idempotente,
  submit final e estado somente leitura depois da finalização;
- no contexto coletivo, mostrar o estado compartilhado mais recente e atribuir
  cada mutação ao participante autenticado, sem apresentar uma evidência por
  integrante;
- aplicar release e gradebook já entregues aos dois tipos de sujeito.

### Gate

- somente o sujeito individual ou participante congelado do grupo pode realizar
  o self review;
- score não pode exceder limites nem entrar no answer payload;
- submissions coletivas preservam uma evidência e um resultado, não um por
  integrante;
- dois participantes não finalizam evidências coletivas concorrentes;
- saves concorrentes ou repetidos não perdem versões nem duplicam auditoria;
- capability `AuthorTest` não autoriza publish; `OfficialSubmission` só existe
  depois dos E2Es oficiais individual e coletivo;
- direto e combinado com instrutor passam no test run e nos E2Es individual e
  coletivo;
- testes de interface cobrem reload/resume, conflito de versão, retry, submit
  final e bloqueio de edição posterior nos contextos de teste e oficial;
- não existe implementação paralela específica da UI ou do runtime.

## `SEQ-13`. Completar `PeerReview`

### Resultado

As revisões entre alunos produzem um resultado agregado exatamente uma vez no
test run e na distribuição acadêmica real, inclusive quando o alvo é uma
submission coletiva.

### `SCHEMA-GATE` de peer

Antes de alterar a persistência, inventariar explicitamente
`PeerReviewsController`, `IPeerReviewAssignmentService`,
`PeerReviewAssignmentService`, `actions-peer-review.ts`, o workspace atual, os
clients gerados, `GradingQueueService`, `TasksService`, actions e painéis do
SpeedGrader, projeções de tarefas/fila e os produtores de notificação.
Confirmar que dependências de `CanonicalRow` e submissions irmãs já foram
eliminadas em `SEQ-11`. Apresentar apenas eventuais mudanças necessárias para:

- lease, expiração, reatribuição e idempotência de claims;
- cota do revisor separada do limiar recebido pela submission;
- evidências e agregação versionada;
- preservação de `AssessmentPeerReview.Score` como `ScoreValue` textual já
  normalizado em `SEQ-03`, caso a entidade continue armazenando a contribuição
  individual;
- anonimato na projeção e identidade preservada para auditoria;
- exclusão de todos os participantes quando o alvo for coletivo;
- transição durável `AwaitingInstructorResolution` quando o prazo encerrar sem o
  mínimo de evidências, além dos comandos idempotentes de extensão,
  reatribuição e resolução docente final. Se stages/evidências genéricos não
  representarem isso sem ambiguidade, apresentar o delta relacional neste gate.

Reutilizar tabelas atuais quando possuírem ownership e invariantes corretos.
`AssessmentPeerReview` pode permanecer como registro individual de claim e
evidência, mas não como segunda autoridade do resultado agregado.
Qualquer mudança aprovada edita o mesmo baseline global e recria os bancos
afetados.

### Implementação

- adaptar a atribuição, claim e workspace anônimos existentes ao stage/round
  canônico, preservando a UX útil sem preservar autoridade paralela;
- impedir que aluno revise a própria submission ou uma submission de grupo do
  qual participe;
- manter revisores individuais mesmo quando o alvo for coletivo;
- adicionar lease, expiração, reatribuição e idempotência;
- definir mínimo de evidência, prazo e comportamento terminal de insuficiência;
- quando o prazo encerrar sem o mínimo, mover o stage para
  `AwaitingInstructorResolution`, sem zero e sem marcar `PeerReview` como
  concluído;
- implementar comandos autorizados e idempotentes para o instrutor estender a
  janela, reatribuir claims ou finalizar por resolução docente. A finalização
  cria evidência docente e evento de auditoria próprios, registra que o limiar
  peer não foi alcançado e não altera retroativamente `ReviewMethods`;
- transformar cada submit autorizado do workspace em `ReviewEvidence` da
  `GradingExecution`; somente o handler de `PeerReview` aceita, agrega e
  conclui o stage;
- agregar resultados por submission conforme policy versionada;
- encaminhar opcionalmente o agregado para `InstructorReview`;
- exercitar múltiplas personas no test run;
- registrar inicialmente somente `AuthorTest` e concluir os fluxos direto e
  combinado no test run multipersona;
- conectar claims, distribuição e agregação às submissions oficiais individuais
  e coletivas; somente após esses E2Es, a autorização e os efeitos acadêmicos
  passarem, registrar `OfficialSubmission` na configuração de produção e
  permitir publish. Os E2Es usam registro oficial controlado no ambiente de
  teste, sem antecipar essa promoção;
- remover no mesmo corte qualquer atribuição direta de score final,
  agregação/fan-out por submissions irmãs e notificação emitida diretamente por
  `PeerReviewAssignmentService`, controller ou action web;
- persistir na outbox os eventos canônicos de stage, resultado e release
  necessários aos efeitos posteriores; notificações externas e passback
  permanecem desligados até os consumers de `SEQ-15`;
- remover ou adaptar, no mesmo E2E, rotas e métodos antigos que permitam
  concluir peer review fora da `GradingExecution`.

### Gate

- claim expirado não consome cota e pode ser reatribuído;
- concorrência não duplica review, agregação ou finalização;
- anonimato externo e identidade auditável coexistem;
- nenhum integrante do grupo-alvo recebe claim sobre a própria submission;
- direto e combinado com instrutor passam no test run e nos E2Es individual e
  coletivo;
- falta de evidência nunca gera zero nem espera indefinidamente depois do prazo:
  entra em `AwaitingInstructorResolution` e somente um dos comandos explícitos
  de extensão, reatribuição ou resolução docente altera esse estado;
- resolução docente de insuficiência preserva claims/evidências recebidos,
  motivo, ator e antes/depois, sem declarar falsamente conclusão peer;
- busca, testes de rota e testes de service comprovam que submit antigo não
  atribui score nem dispara notificação direta; existe uma única autoridade
  para evidência individual, agregação e resultado;
- filas, tarefas e painéis consomem projeções do stage/round canônico, sem
  restaurar `CanonicalRow`, submissions irmãs ou segunda autoridade;
- capability `AuthorTest` não autoriza publish; `OfficialSubmission` só existe
  em produção depois dos E2Es oficiais individual e coletivo.

## `SEQ-14`. Entregar a porta de `AIReview`

### Resultado

Um provider pode ser conectado sem alterar o orquestrador. Esta etapa entrega a
porta e sua durabilidade, não uma IA concreta de produção.

### `SCHEMA-GATE` condicional de AI

Primeiro provar se outbox, inbox e deduplicação do núcleo atendem ao contrato.
Somente se não atenderem, apresentar mudanças para:

- request estável e correlação com stage/round;
- resposta deduplicada e evidência do provider;
- timeout, retry e estado pendente;
- identidade e versão do modelo/provider.

Mudanças aprovadas editam o baseline global; não criam migration incremental.

### Implementação

- criar interface, registry específico, capability descriptor e configuração
  de provider sobre `IReviewCapabilityRegistry`;
- versionar request, response, evidência e identidade do modelo/provider;
- persistir stage e `AIReviewRequested` antes de qualquer chamada externa;
- despachar por outbox e receber por inbox deduplicada;
- aplicar timeout, retry e estado pendente sem score de fallback;
- bloquear publish sem provider compatível registrado;
- usar provider controlado somente nos contract tests e test runs;
- provar que o handler permanece indiferente ao sujeito individual ou coletivo;
- manter produção indisponível até existir provider real configurado.

### Gate

- nenhuma chamada externa ocorre dentro da transação de submit;
- resposta duplicada não duplica evidência nem resultado;
- indisponibilidade transitória mantém a execução pendente;
- ausência de provider bloqueia publish e nunca produz nota;
- provider controlado prova os fluxos direto e seguido por instrutor;
- a UI não afirma que `AIReview` está disponível em produção sem provider real.

## `SEQ-15`. Completar release, gradebook e operação

### Resultado

O E2E acadêmico já existe. Este marco acrescenta políticas e consumidores
operacionais avançados sem redefinir resultado, tentativa ou workflow.

### `SCHEMA-GATE` condicional de operação

`Program.PassingScore` e os demais campos acadêmicos existentes já foram
convertidos em `SEQ-03`. Usar outbox e projeções existentes primeiro e somente
propor persistência nova quando consulta, retenção ou idempotência operacional
não puderem ser atendidas corretamente. Toda proposta exige aprovação e edição
do mesmo baseline global; se não houver delta relacional, registrar o gate com
delta zero.

Para release agendado, o gate deve provar se `AssessmentResultRelease` já
suporta `ScheduledFor` em UTC, versão de concorrência e índice eficiente por
`(State, ScheduledFor)`. Qualquer delta permanece pertencente à rodada pelo
único `GradeRoundId`; não adicionar `AssessmentSubmissionId` redundante.

### Implementação

- habilitar o modo `scheduled` já reservado no contrato versionado de
  `AssessmentResultReleasePolicyV1`, sem alterar retrospectivamente o schema
  de policies de revisões publicadas;
- implementar `ScheduleGradeResultRelease` e
  `CancelScheduledGradeResultRelease` com ator, permissão, rodada esperada,
  versão de concorrência, idempotency key, motivo e auditoria. Reagendamento é
  uma nova execução idempotente de schedule sobre a versão esperada;
- persistir `ScheduledFor` em UTC e usar `TimeProvider` injetável. O worker busca
  releases vencidos por índice, mas executa o mesmo `ReleaseGradeResult` com
  identidade de serviço autorizada; nunca altera o estado diretamente;
- quando a rodada finalizar depois do horário configurado, a policy solicita
  release imediato pelo mesmo comando. Quando finalizar antes, cria o estado
  `Scheduled`. Cada nova rodada de regrade reaplica a policy sem alterar o
  agendamento ou a liberação das rodadas anteriores;
- cancelar um agendamento retorna a rodada a `Withheld`; não retira resultado já
  liberado. Retirada de release continua sendo outro caso de uso explicitamente
  fora deste corte;
- ampliar, se necessário, as policies além da seleção mínima entregue em
  `SEQ-10`; uma eventual média deve ser modelada como agregação explícita,
  mantendo uma única contribuição canônica;
- integrar `Program.PassingScore` textual e as projeções globais já
  normalizadas à consolidação do curso, sem conversão tardia ou cast numérico;
- manter projeções agregadas precomputadas sem aritmética decimal em SQL;
- construir filas docentes por estado de review;
- implementar os consumers de notificação e passback, que permaneceram
  deliberadamente desligados na Parte 2, consumindo somente os eventos
  canônicos adequados e nunca comandos ou services de grading diretamente;
- registrar cada consumer com `ConsumerKey` estável e deduplicar sua entrega por
  `(EventId, ConsumerKey)`. Falha de notificação não reabre gradebook, progresso
  ou auditoria já confirmados; a mensagem encerra somente depois de todas as
  entregas obrigatórias capturadas confirmarem;
- não reproduzir eventos anteriores apenas porque um consumer foi habilitado.
  Qualquer replay histórico é uma operação explícita, autorizada e auditada;
- adicionar métricas, alertas, retry operacional e inspeção de falhas;
- garantir que `GradeResultFinalized` alimente projeções acadêmicas e auditoria;
- garantir que `GradeResultReleased` alimente aluno e notificações;
- testar reprocessamento e reconstrução idempotente das projeções.

### Gate

- resultado pode contribuir no gradebook enquanto ainda está retido do aluno;
- atividade de peso zero pode liberar nota e feedback sem contribuir no total;
- schedule, cancelamento, reagendamento e worker são idempotentes, respeitam
  versão/rodada esperadas e geram exatamente um `GradeResultReleased`;
- relógio avançado em teste libera somente rodadas vencidas; resultado
  finalizado após o horário é liberado sem permanecer preso em `Scheduled`;
- regrade agenda e libera cada rodada independentemente, preservando a última
  rodada learner-visible até o release da substituta;
- retry não duplica projeção, notificação ou passback;
- falhas independentes preservam os receipts dos consumers já concluídos e
  deixam pendentes somente as entregas ainda não confirmadas;
- nenhuma notificação ou passback nasce de `GradeSubmissionAsync`,
  `ActivityGrade`, `ContentInteraction` ou outro produtor removido;
- auditoria reconstrói round, atores, overrides e release;
- nenhum consumidor executa review ou recalcula resultado oficial.

Referência: [`06`](../06-gradebook-audit-and-operations.md).

## `SEQ-16`. Auditar e fechar o E2E

### Resultado

O fluxo já foi substituído e limpo por fatia. O último marco confirma que não
restaram autoridades concorrentes, referências obsoletas ou lacunas na matriz.

### Implementação

- procurar contratos descartados, nomes `*Graded` e código inalcançável;
- confirmar que rotas de aluno não expõem JSON autoral;
- confirmar que packages e UI não produzem score oficial;
- confirmar ausência de fan-out de grading por integrante;
- atualizar mapas de serialização e documentação arquitetural;
- executar a matriz dos nove workflows no test run;
- executar todos os workflows oficialmente implementados na jornada de aluno;
- confirmar bloqueio de `AIReview` sem provider real;
- confirmar bloqueio de automated-only parcial enquanto a decisão de produto
  permanecer pendente;
- remover qualquer resíduo encontrado antes de concluir o marco.

### Gate final

- todos os itens da definição global de pronto do
  [`README`](../README.md#definição-global-de-pronto) estão satisfeitos;
- não existe caminho paralelo que gere score oficial;
- nenhum teste depende de banco histórico ou dado migrado;
- o baseline cria o schema final em banco vazio;
- API, web e packages passam em CI;
- observabilidade distingue falha técnica, espera legítima e revisão humana.

## Definição de pronto da Parte 3

- todos os gates de `SEQ-12` a `SEQ-16` estão satisfeitos;
- reviews adicionais reutilizam o mesmo orquestrador, autorização, rounds,
  evidências, resultado e release do E2E principal;
- `SelfReview` e `PeerReview` passam nos contextos de teste e oficial
  aplicáveis, inclusive para sujeito coletivo;
- insuficiência peer possui transição e resolução docente terminal, sem espera
  infinita, zero sintético ou falsa conclusão do método;
- `AIReview` possui porta durável e permanece indisponível em produção sem
  provider real;
- score global, release avançado, filas, notificações e passback consomem
  eventos canônicos sem recalcular grading;
- fan-out de eventos mantém confirmação durável e retry independente por
  consumer obrigatório;
- auditoria, observabilidade, mapas de serialização e matriz E2E estão
  completos;
- toda a suíte acumulada das Partes 1, 2 e 3 passa em CI com banco criado do
  zero e diff global sem drift depois de cada `SCHEMA-GATE`.

## Acompanhamento

| Marco | Status | Evidência |
| --- | --- | --- |
| `SEQ-12` | pendente | `SelfReview` individual e coletivo |
| `SEQ-13` | pendente | `PeerReview` individual e coletivo |
| `SEQ-14` | pendente | contract test de provider e gate condicional |
| `SEQ-15` | pendente | integração global, operação e projeções idempotentes |
| `SEQ-16` | pendente | CI, auditoria e checklist global |
