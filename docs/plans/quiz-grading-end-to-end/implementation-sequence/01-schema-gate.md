# SCHEMA-GATE da Parte 1: fundacao e autoria

- Status: aprovado e implementado
- Data do inventario: 2026-09-04
- Reconciliado com a politica de preservacao: 2026-09-28
- Plano executor: [`01-foundation-and-authoring.md`](./01-foundation-and-authoring.md)
- Escopo liberado apos aprovacao: `SEQ-03` a `SEQ-06`
- Migration incremental aplicada:
  `20260916194905_AddAssessmentGradingWorkflow`

Este documento descreve o delta estrutural completo proposto para a Parte 1.
Ele autoriza somente a migration incremental forward-only descrita aqui, com
conversoes e backfills explicitamente testados. A cadeia historica, os dados
existentes e estruturas cuja remocao nao foi comprovada sao preservados. O fato
de a plataforma ainda nao ter sido lancada nao torna esses artefatos
descartaveis. Esta decisao segue o
[`ADR-20260903-development-database-baseline`](../../../architecture/ADR-20260903-development-database-baseline.md).

A migration aplicada nao deve ser reescrita. A configuracao EF preserva
`DefinitionPayload`, `DefinitionSchemaVersion`, `GradingMethods`,
`PeerReviewsRequiredCount` e `StructuredAnswerPayload` como shadow properties;
os testes de migration devem continuar impedindo sua remocao acidental.

## Limites do corte

Entram neste gate:

- autoria atomica de content e assessment;
- revisoes imutaveis e snapshots executaveis;
- test runs e sujeitos sinteticos do instrutor;
- raiz generica de grading, rodadas, stages, resultados por item e evidencias;
- idempotencia e outbox academica com fan-out duravel;
- representacao inteira de ponto fixo para valores academicos, com escala `100`;
- evolucao incremental do schema global sem reescrever migrations anteriores.

Nao entram participantes de submission coletiva, claims e leases de
`PeerReview`, inbox ou payload de IA, release oficial, gradebook, passback,
notificacoes, assets ou tabelas criadas somente para uma interface futura.

## 1. Tabelas novas

Todas usam `uuid` como chave. Entidades mutaveis possuem `Version integer` como
token de concorrencia, `TenantId uuid`, `CreatedAt`, `UpdatedAt` e `DeletedAt`
quando soft delete fizer parte do lifecycle. Registros append-only nao possuem
`UpdatedAt` nem soft delete.

| Tabela | Responsabilidade | Colunas especificas principais |
| --- | --- | --- |
| `AssessmentDefinitionRevisions` | Revisao imutavel preparada no servidor | `AssessmentId`, `RevisionNumber`, `SchemaVersion`, `AuthoringSourceCanonicalJson`, `AuthoringSourceHash`, `AuthoringSourceHashVersion`, `ExecutionSnapshotCanonicalJson`, `ExecutionSnapshotHash`, `ExecutionSnapshotHashVersion`, `CreatedByUserId`, `CreatedAt` |
| `AssessmentTestRuns` | Sessao de teste exclusiva do instrutor | `AssessmentId`, `DefinitionRevisionId`, `CreatedByUserId`, `Status`, `CompletedAt`, `Version` |
| `AssessmentTestRunSubjects` | Persona sintetica pertencente a um test run | `TestRunId`, `PersonaKey`, `DisplayName`, `Version` |
| `GradingExecutions` | Raiz generica de execucao | `DefinitionRevisionId`, `ExecutionContext`, exatamente um entre `TestRunSubjectId` e `AssessmentSubmissionId`, entrega canonica, resposta canonica, `Status`, `ActiveGradeRoundId`, `SubmittedAt`, `FinalizedAt`, `Version` |
| `GradeRounds` | Rodada versionada, imutavel depois da finalizacao | `GradingExecutionId`, `RoundNumber`, `SupersedesGradeRoundId`, `Reason`, `Status`, `ResultSchemaVersion`, `ResultState`, `Score`, `MaxScore`, `Feedback`, `StartedAt`, `FinalizedAt`, `Version` |
| `ReviewStages` | Stage ordenado de uma rodada | `GradeRoundId`, `Sequence`, `ReviewMethod`, chave e versao do handler, provider opcional, `Status`, `StartedAt`, `CompletedAt`, `Version` |
| `GradeItemResults` | Snapshot do resultado de cada item ao fim de um stage | `ReviewStageId`, `ItemId`, `State`, `Score`, `MaxScore`, `Feedback` |
| `ReviewEvidence` | Evidencia versionada e auditavel produzida por um stage | `ReviewStageId`, `EvidenceKey`, `ItemId` opcional, `EvidenceType`, `SchemaVersion`, `CanonicalJson`, `PayloadHash`, `HashVersion`, ator ou servico produtor, `CreatedAt` |
| `GradingCommandReceipts` | Deduplicacao duravel de comandos | `TenantId`, `ResourceId`, `CommandType`, `ActorId`, `IdempotencyKey`, `RequestHash`, `OutcomeSchemaVersion`, `OutcomeCanonicalJson`, `CreatedAt`, `ExpiresAt` |
| `AcademicOutboxMessages` | Evento academico persistido na transacao de origem | `TenantId`, `EventType`, `EventSchemaVersion`, `PayloadCanonicalJson`, `PayloadHash`, `OccurredAt`, `Status`, `CompletedAt` |
| `AcademicOutboxDeliveries` | Rota congelada e receipt por consumer | `OutboxMessageId`, `ConsumerKey`, `Status`, `AttemptCount`, `NextAttemptAt`, `ClaimedAt`, `ClaimedBy`, `ConfirmedAt`, `LastError` |

Os JSONs cuja identidade participa de hash sao armazenados como `text`, com os
bytes UTF-8 canonicos validados antes da gravacao. Nao serao armazenados como
`jsonb` nem reconstruidos a partir de entidades EF.

### Campos canonicos de `GradingExecutions`

A entrega e a resposta possuem grupos all-or-none:

- entrega: `DeliverySchemaVersion`, `DeliveryCanonicalJson`, `DeliveryHash` e
  `DeliveryHashVersion`;
- resposta: `ResponseSchemaVersion`, `ResponseContentType`,
  `ResponsePayloadSchema`, `ResponseEnvelopeCanonicalJson`, `ResponseHash` e
  `ResponseHashVersion`.

A entrega e materializada uma unica vez. A resposta pode ser editada somente
enquanto a execucao aceitar draft e se torna imutavel no submit.

## 2. Tabelas removidas

Nenhuma tabela funcional sera removida neste corte.

`AssessmentPeerReviews`, rubricas, activity grades, content progress e LTI
permanecem existentes. A integracao desses modelos com o novo runtime sera
feita pelas fatias que os possuem, sem aliases de compatibilidade no core.

A cadeia historica e a tabela `__EFMigrationsHistory` sao preservadas. Um squash
futuro e uma operacao independente deste gate e exige inventario, backup
verificavel e confirmacao de que nenhum ambiente depende da cadeia anterior.

## 3. Colunas alteradas

### `Assessments`

Novas:

- `PublishedDefinitionRevisionId uuid null`;
- `ReviewConfigurationCanonicalJson text null`, limitado a 64 KiB e validado
  como `AssessmentReviewConfigurationV1 { schemaVersion, peer?, ai?, self?,
  instructor? }`; esse payload nao contem nem duplica `ReviewMethods`;
- `AttemptContributionMode varchar(32) null`;
- `ContentCompletionMode varchar(32) not null`, default
  `on-release-and-pass`;
- `ResultReleaseMode varchar(16) not null`, default `manual`;
- `ResultReleaseScheduledFor timestamptz null`.

Adicionada como nova autoridade:

- `ReviewMethods integer`, preenchida deterministicamente a partir de
  `GradingMethods` antes de se tornar obrigatoria. Nomes `*Graded` deixam o
  contrato e o runtime no mesmo corte, sem renomear nem apagar a coluna de
  origem.

Preservadas como compatibilidade de armazenamento, sem novas leituras ou
escritas pelo runtime:

- `DefinitionPayload`;
- `DefinitionSchemaVersion`;
- `GradingMethods`;
- `PeerReviewsRequiredCount`.

Essas colunas ficam mapeadas como shadow properties. A remocao fisica exige
outro `SCHEMA-GATE`, com inventario de consumidores, prova de que todos os dados
relevantes foram materializados nas revisoes imutaveis, teste de upgrade e
estrategia de rollback/restore.

Sem mudanca de tipo para scores: `MaxScore` e `PassingScore` permanecem
`integer`, agora definidos explicitamente como unidades `ScoreValue` escaladas
por `100`.
- `MaxAttempts`: passa a ser obrigatorio, default `1`, e a Parte 1 rejeita
  qualquer valor diferente de `1`.

O draft autoral continua dividido por ownership: o content tipado permanece em
`ProgramContent.JsonBody`; `Assessment` guarda apenas policy relacional e a
configuracao tipada que nao cabe em colunas sem antecipar tabelas de peer/AI.
Uma revisao preparada congela ambos em `AssessmentAuthoringSourceV1`.

### `AssessmentSubmissions`

- preserva `StructuredAnswerPayload` como shadow property inerte durante a
  materializacao no novo owner; novas respostas usam somente a
  `GradingExecution`;
- `Score` permanece `integer null` e passa a representar unidades
  `ScoreValue` escaladas por `100`;
- `StructuredAnswer` pode continuar como modalidade declarada, mas os bytes da
  resposta existem somente em `GradingExecutions`;
- a constraint de consistencia de payload deixa de exigir uma coluna JSON para
  essa modalidade.

### `content_interactions`

- remove a coluna `CompletionPercentage`, criada indevidamente a partir de um
  alias de `ProgressPercentage`;
- o alias C# passa a ser nao mapeado e usa somente `ProgressPercentage`;
- `ProgressPercentage` e `BestScore` passam a inteiros de escala `100`.

Esta remocao e uma excecao explicitamente inventariada: a migration deve copiar
qualquer valor ainda necessario para `ProgressPercentage` antes do `DROP`, provar
que nao existe consumidor SQL da coluna e testar o upgrade com ambas as colunas
populadas. Se qualquer uma dessas provas faltar, a coluna tambem deve ser
preservada como compatibilidade inerte.

As demais colunas listadas na secao 5 mudam somente de representacao e
contrato. Campos numericos nao academicos, como tempo, contadores, valores
financeiros, relevancia, gamification points e coordenadas de video, nao fazem
parte deste gate.

## 4. Constraints, FKs e indices

### Revisoes e publicacao

- unique `(AssessmentId, RevisionNumber)`;
- unique auxiliar `(Id, AssessmentId)`;
- FK composta de `(Assessments.PublishedDefinitionRevisionId, Assessments.Id)`
  para `(AssessmentDefinitionRevisions.Id, AssessmentId)`, impedindo publicar
  revisao de outro assessment;
- `RevisionNumber > 0` e versoes de schema/hash positivas ou conhecidas;
- hashes SHA-256 hexadecimais possuem exatamente 64 caracteres;
- JSON autoral limitado a 4 MiB e snapshot a 8 MiB.

### Test run e execucao

- unique `(TestRunId, PersonaKey)`;
- FK composta de test run para uma revisao do mesmo assessment;
- check de owner e contexto em `GradingExecutions`:
  `author-test` exige somente `TestRunSubjectId`; `official-submission` exige
  somente `AssessmentSubmissionId`;
- unique em cada FK nullable de owner, garantindo uma execucao por sujeito ou
  submission;
- grupos de entrega e resposta sao integralmente nulos ou integralmente
  preenchidos;
- entrega e resposta limitadas a 8 MiB cada;
- unique auxiliar `(GradeRounds.Id, GradingExecutionId)` e FK composta para
  `SupersedesGradeRoundId`, impedindo superseder rodada de outra execucao;
- FK composta de `(GradingExecutions.ActiveGradeRoundId,
  GradingExecutions.Id)` para `(GradeRounds.Id, GradingExecutionId)`, impedindo
  apontar para rodada de outra execucao;
- unique `(GradingExecutionId, RoundNumber)` e `RoundNumber > 0`;
- unique `(GradeRoundId, Sequence)` e `Sequence > 0`;
- unique `(ReviewStageId, ItemId)`;
- unique `(ReviewStageId, EvidenceKey)`;
- evidencias limitadas a 1 MiB.

### Workflows e valores

- `ReviewMethods IN (0, 1, 2, 4, 8, 9, 10, 12, 16, 24)` no draft;
- publicacao rejeita `0` no dominio;
- checks fechados para contextos, estados, review methods, completion, release
  e contribution mode;
- `scheduled` exige `ResultReleaseScheduledFor`; outros modos exigem `null`;
- `ScoreValue` e `integer` no intervalo `0..2147483647`, onde `100` unidades
  representam `1` ponto;
- `PercentValue` e `integer` no intervalo `0..10000`, onde `100` unidades
  representam `1%` e `10000` representa `100%`;
- scores nao negativos e `Score <= MaxScore` onde os dois campos coexistem;
- `PassingScore <= MaxScore`;
- `AssessmentGroup.WeightPercent <= 10000`;
- `ActivityGrade.Points <= MaxPoints` quando ambos existirem.

### Idempotencia e outbox

- unique `(TenantId, ResourceId, CommandType, ActorId, IdempotencyKey)`;
- mesma chave e request hash retorna o outcome; hash divergente gera conflito;
- unique `(OutboxMessageId, ConsumerKey)`;
- indice de dispatch `(Status, NextAttemptAt)`;
- indice de claim `(Status, ClaimedAt)`;
- mensagem so conclui quando todas as deliveries congeladas confirmarem.

Nao sera criada RLS nova neste corte. O escopo de tenant e a matriz de
autorizacao continuam obrigatorios no servidor.

## 5. Contrato inteiro final dos valores academicos

| Tabela | Coluna atual | Contrato inteiro final |
| --- | --- | --- |
| `Assessments` | `MaxScore`, `PassingScore` (`integer`) | `ScoreValue` (`integer`) |
| `AssessmentSubmissions` | `Score` (`integer`) | `ScoreValue` (`integer`) nullable |
| `AssessmentPeerReviews` | `Score` (`integer`) | `ScoreValue` (`integer`) nullable |
| `RubricCriteria` | `Points` (`integer`) | `ScoreValue` (`integer`) |
| `AssessmentGroups` | `WeightPercent` (`numeric(5,2)`) | `PercentValue` (`integer`) |
| `LtiLineItemMappings` | `MaxScore` (`integer`) | `ScoreValue` (`integer`) |
| `activity_grades` | `Points`, `MaxPoints` (`numeric(5,2)`) | `ScoreValue` (`integer`) nullable |
| `content_interaction_events` | `ProgressPercentage` (`numeric(5,2)`) | `PercentValue` (`integer`) nullable |
| `content_interactions` | `ProgressPercentage` (`numeric(5,2)`) | `PercentValue` (`integer`) nullable |
| `content_interactions` | `BestScore` (`numeric(5,2)`) | `ScoreValue` (`integer`) nullable |
| `content_interactions` | `CompletionPercentage` (`numeric`) | removida; era alias duplicado |
| `content_progress` | `ProgressPercentage` (`numeric(5,2)`) | `PercentValue` (`integer`) |
| `content_progress` | `Score`, `MaxScore` (`numeric(5,2)`) | `ScoreValue` (`integer`) nullable |
| `course_prerequisites` | `MinimumGrade` (`integer`) | `PercentValue` (`integer`) nullable |
| `program_enrollments` | `ProgressPercentage`, `FinalGrade` (`numeric(5,2)`) | `PercentValue` (`integer`) / nullable |
| `program_users` | `CompletionPercentage`, `FinalGrade` (`numeric(5,2)`) | `PercentValue` (`integer`) / nullable |
| `programs` | `PassingScore` (`numeric(5,2)`) | `PercentValue` (`integer`) |

Isso corresponde a 22 colunas atuais: 21 passam a armazenar unidades inteiras e
uma e removida. DTOs, commands, queries, servicos, LTI, clients gerados e web
usam `ScoreValue` ou `PercentValue`; JSON transporta seus inteiros de unidades,
sem strings, `decimal`, `double` ou `float`. TypeScript usa `number` branded e
valida `Number.isSafeInteger` mais os limites do contrato.

## 6. Transacoes e concorrencia

- `SaveAssessmentDraft` grava content e assessment em uma transacao e usa
  as versoes esperadas dos dois recursos;
- prepare grava revisao, manifest, snapshot e outbox na mesma transacao;
- publish valida revisao, hash do test run concluido e versao do assessment
  antes de trocar o ponteiro publicado;
- start/materializacao cria no maximo uma entrega por execucao;
- submit grava o envelope, fecha sua mutabilidade e cria a primeira rodada no
  mesmo commit;
- regrade cria nova rodada e nunca atualiza a anterior;
- idempotency receipt e outcome pertencem a mesma transacao do comando;
- evento e suas deliveries de rota congelada pertencem a mesma transacao da
  mudanca academica;
- todos os agregados mutaveis usam `Version` com concorrencia otimista;
- autorizacao ocorre antes de consultar ou devolver receipt de replay.

## 7. Evolucao incremental do schema EF global

O inventario atual possui:

- 130 IDs de migration;
- 76 arquivos companion `*.Designer.cs`;
- 47 partials `*.Security.cs`;
- um `ApplicationDbContextModelSnapshot` corrente;
- 83 arquivos de migration/seguranca com SQL manual;
- 391 tabelas no banco vazio reconstruido pela cadeia completa.

A operacao aprovada sera:

1. implementar o modelo final em codigo;
2. preservar as migrations anteriores e seus companions no Git;
3. gerar uma nova migration incremental e atualizar o snapshot corrente somente
   com o delta aprovado;
4. executar conversoes e backfills antes de `NOT NULL`, checks ou FKs que
   dependam dos novos valores;
5. preservar o comportamento dos 83 arquivos com SQL manual; qualquer alteracao
   deve estar destacada e testada, sem depender apenas do diff do `IModel`;
6. manter `Database.MigrateAsync()` e fazer o CI provar tanto criacao do zero
   pela cadeia completa quanto upgrade de banco populado pela migration
   anterior;
7. implementar `Down` somente quando a reversao for segura. Quando puder perder
   dados, falhar explicitamente e usar um plano operacional de backup/restore;
   Git nao e rollback de banco.

Bancos locais e compartilhados podem conter trabalho autoral, configuracoes e
submissoes representativas. Nenhuma remocao e autorizada somente por o produto
estar em pre-lancamento ou por a estrutura parecer antiga.

## 8. Diff global esperado

O diff funcional permitido e somente este:

- `+11` tabelas listadas na secao 1;
- `-0` tabelas funcionais;
- alteracoes de `Assessments`, `AssessmentSubmissions` e
  `content_interactions` listadas na secao 3;
- conversao das colunas da secao 5;
- preservacao das funcoes e triggers de score de assessment ate que um gate
  especifico prove o substituto funcional, inventarie consumidores e aprove a
  remocao por migration incremental;
- constraints, FKs e indices da secao 4;
- nenhuma mudanca funcional fora dos modulos Learning, Assessments, Grading e
  LTI.

O modelo corrente reconstruido possui 5.616 colunas de catalogo, com hash
SHA-256 `40d396ff8708f444e2f9814af230297dfa092402dcaf29e43f568ea6e21d075e`,
e 1.008 constraints, com hash
`31f97bd857754a7766c9e8925a761fcdcbe727232be1a9b331ab2acd4e9402b3`.
Depois da implementacao, um diff canonico deve explicar cada divergencia por
uma linha deste gate; qualquer outra diferenca bloqueia o corte.

## 9. Eventos academicos e outbox

A outbox de Economy nao sera reutilizada: ela e interna ao ledger, depende de
`PostingGroupId` e nao implementa fan-out com receipt por consumer.

`ApplicationDbContext.SaveChangesAsync` passara a separar eventos:

- eventos academicos duraveis sao mapeados para
  `AcademicOutboxMessages` e suas deliveries antes do commit;
- esses eventos sao removidos do lote entregue ao `IPublisher` em processo;
- eventos nao academicos continuam no mecanismo atual ate seus proprios
  modulos decidirem migrar;
- um worker processa somente depois do commit, com claim, retry e confirmacao
  idempotente por consumer;
- crash apos commit nao perde evento; falha de um consumer nao repete os ja
  confirmados.

## 10. SQL ativo fora do `IModel`

O catalogo detalhado e a estrategia de equivalencia estao em
[`01-schema-gate-sql-catalog.md`](./01-schema-gate-sql-catalog.md).

Decisao proposta:

- preservar sem alteracao funcional extensoes, schemas, roles, grants, todas as
  rotinas, triggers e os 41 indices especiais atuais;
- tratar `enforce_assessment_max_score`,
  `enforce_assessment_submission_score` e seus dois triggers como candidatos a
  remocao, nao como remocoes implicitas. Eles so podem sair em gate posterior
  depois de o novo caminho provar invariantes equivalentes, inexistencia de
  consumidores e upgrade seguro;
- nao criar nova funcao, trigger, view, policy ou role manual para grading na
  Parte 1;
- expressar o novo delta por EF, checks, FKs e indices declarativos;
- testar catalogo e comportamento em banco vazio e em upgrade populado.

## Decisao solicitada

A aprovacao deste gate autoriza exclusivamente o delta incremental acima.
Qualquer tabela, coluna, remocao ou artefato adicional exigira novo destaque e
aprovacao antes de ser criado, alterado ou apagado.

**Gate aprovado pelo responsavel do projeto para migration incremental
forward-only, preservando a cadeia historica, dados existentes e colunas de
compatibilidade inertes ate prova e aprovacao especificas de remocao.**
