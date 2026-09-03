# Grading: mapa de tipos, serializacao e fronteiras

## Objetivo

Este documento mapeia os contratos de `@game-guild/grading`, sua integracao atual
com `@game-guild/quiz` e `@game-guild/quiz-content`, e a projecao operacional em
Assessment na API. Ele deve ser usado para:

- localizar a fonte de verdade de cada tipo e campo de grading;
- distinguir configuracao autoral, answer key, resposta do aluno e resultado;
- impedir que detalhes de quiz contaminem o nucleo generico de grading;
- impedir que regras de gradebook e Assessment sejam gravadas no documento de quiz;
- auditar serializacao, desserializacao, redaction e persistencia;
- orientar a entrada futura de outros tipos de conteudo no grading.

## Modelo em uma pagina

Existem tres familias de dados diferentes. Elas nao devem ser tratadas como um
unico objeto:

```text
ProgramContent.JsonBody
  QuizContentDocumentV1
    blocks: Record<blockId, QuizEntry>       <- conteudo autoral do quiz
    grading: ContentGradingDefinition       <- como o conteudo deve ser avaliado

Assessment + AssessmentGroup               <- entrega, tentativas e gradebook

AssessmentSubmission
  StructuredAnswerPayload                  <- o que o aluno respondeu
  Score / Passed / Feedback / GradedAt      <- resultado confiavel
```

No codigo atual, `ContentGradingDefinition` pertence ao conteudo e mistura itens
autorais com policies operacionais. O alvo aprovado separa
`ContentGradingDefinitionV2`, limitado aos items, de
`AssessmentExecutionPolicyV1`, pertencente ao assessment.
`AssessmentGroup.WeightPercent` decide participacao no gradebook.
`AssessmentSubmission` guarda tentativas e resultados confiaveis.

No alvo, `AssessmentExecutionPolicyV1` e materializado pelas fontes unicas do
agregado Assessment e congelado na revisao. Ele nao e um segundo JSON mutavel
que replique as colunas relacionais.

## Limites entre packages e camadas

| Camada | Responsabilidade | Nao deve possuir |
| --- | --- | --- |
| `@game-guild/quiz` | perguntas, respostas tipadas, learner projection e avaliacao local de pratica | Assessment, tentativas, gradebook, `ContentGradingDefinition` |
| `@game-guild/grading` core | politicas genericas, item de grading, resposta estruturada e resultado | UI, React, banco, navegacao, forma autoral de um conteudo especifico |
| adapter quiz em `@game-guild/grading` | traducao `QuizEntry <-> grading`: items, answer key, redaction, wire answer e avaliacao | edicao de quiz, storage do documento, Assessment |
| `@game-guild/quiz-content` | documento versionado, campo `grading`, lifecycle e sincronizacao entre blocos e items | algoritmo de grading, UI, persistencia relacional |
| `@game-guild/quiz-surface` | editor e player | answer key de runtime oficial, Assessment, configuracao de grading generica |
| integracao em `apps/web` | salvar conteudo, projetar/desprojetar Assessment e conectar submission | redefinir contratos de quiz ou grading localmente |
| API Learning Courses | persistir `ProgramContent.JsonBody` | interpretar perguntas na infraestrutura generica |
| API Learning Assessments | entrega, tentativas, grupos, pesos, submissions e resultados confiaveis | ser a fonte autoral dos blocos de quiz |

Direcao atual de dependencias:

```text
quiz-content -> grading -> quiz
quiz-content ------------> quiz
quiz-surface ------------> quiz + quiz-content
quiz --------------------> nenhum package de grading/UI
```

O package `quiz` possui um teste arquitetural que proibe dependencia de
`@game-guild/grading`. `quiz-surface` tambem nao importa grading. A composicao
ocorre em `quiz-content` e no adapter quiz de grading.

## Fontes de verdade

- tipos centrais: [`packages/features/grading/src/types.ts`](../../packages/features/grading/src/types.ts)
- normalizacao e validacao: [`packages/features/grading/src/config.ts`](../../packages/features/grading/src/config.ts)
- embedding no content body: [`packages/features/grading/src/content-storage.ts`](../../packages/features/grading/src/content-storage.ts)
- contrato de adapter: [`packages/features/grading/src/adapters/types.ts`](../../packages/features/grading/src/adapters/types.ts)
- registry de adapters: [`packages/features/grading/src/adapters/registry.ts`](../../packages/features/grading/src/adapters/registry.ts)
- classificacao e items de quiz: [`packages/features/grading/src/adapters/quiz/items.ts`](../../packages/features/grading/src/adapters/quiz/items.ts)
- answer key de quiz: [`packages/features/grading/src/adapters/quiz/answer-key.ts`](../../packages/features/grading/src/adapters/quiz/answer-key.ts)
- redaction de quiz: [`packages/features/grading/src/adapters/quiz/redaction.ts`](../../packages/features/grading/src/adapters/quiz/redaction.ts)
- resposta estruturada de quiz: [`packages/features/grading/src/adapters/quiz/structured-answer.ts`](../../packages/features/grading/src/adapters/quiz/structured-answer.ts)
- avaliacao deterministica de quiz: [`packages/features/grading/src/adapters/quiz/grading.ts`](../../packages/features/grading/src/adapters/quiz/grading.ts)
- integracao no documento de quiz: [`packages/features/quiz-content/src/grading.ts`](../../packages/features/quiz-content/src/grading.ts)
- tipos do documento de quiz: [`packages/features/quiz-content/src/types.ts`](../../packages/features/quiz-content/src/types.ts)
- projecao web para Assessment: [`apps/web/src/components/learning/console/courses/[course]/content/[contentId]/content-item-editor.tsx`](../../apps/web/src/components/learning/console/courses/%5Bcourse%5D/content/%5BcontentId%5D/content-item-editor.tsx)
- entidade Assessment: [`apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/Assessment.cs`](../../apps/api/Source/Modules/GameGuild.Learning.Assessments/Entities/Assessment.cs)
- configuracao EF: [`apps/api/Source/Modules/GameGuild.Learning.Assessments/Configuration/AssessmentsModelConfiguration.cs`](../../apps/api/Source/Modules/GameGuild.Learning.Assessments/Configuration/AssessmentsModelConfiguration.cs)

## JSON autoral de grading

O contrato raiz atual e `ContentGradingDefinition`:

```ts
interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  score: ScorePolicy;
  attempts: AttemptPolicy;
  feedback: FeedbackPolicy;
  presentation: PresentationPolicy;
  items: Record<string, GradedItemConfig>;
}
```

Esse formato e estado atual, nao o ownership final. O plano canonico substitui
o contrato atomicamente, sem dual-read:

```ts
interface ContentGradingDefinitionV2 {
  schemaVersion: 2;
  items: Record<string, GradingItemAuthoringV2>;
}

interface GradingItemAuthoringV2 {
  rubricRef?: string;
}

interface AssessmentExecutionPolicyV1 {
  schemaVersion: 1;
  passingScore?: ScoreValue;
  maxAttempts?: number;
  attemptContribution?: AttemptContributionPolicyV1;
  timeLimitMinutes?: number;
  availability: AssessmentAvailabilityPolicyV1;
  completion: AssessmentContentCompletionPolicyV1;
  resultRelease: AssessmentResultReleasePolicyV1;
  presentation: AssessmentPresentationPolicyV1;
  review: AssessmentReviewPolicyV1;
}

type AssessmentContentCompletionMode =
  | "on-submit"
  | "on-finalize"
  | "on-release"
  | "on-release-and-pass";

type AssessmentResultReleasePolicyV1 =
  | { mode: "immediate" }
  | { mode: "manual" }
  | { mode: "scheduled"; scheduledFor: string };
```

No quiz, ele e persistido como `grading`, irmao de `schemaVersion`, `order` e
`blocks` no `QuizContentDocumentV1`:

```json
{
  "schemaVersion": 1,
  "order": [["q1", "quiz"], ["q2", "quiz"]],
  "blocks": {
    "q1": { "type": "TRUE_FALSE", "stem": "...", "correctAnswer": true, "points": 2, "settings": {} },
    "q2": { "type": "ESSAY", "stem": "...", "points": 8, "settings": {} }
  },
  "grading": {
    "enabled": true,
    "schemaVersion": 1,
    "score": { "maxScore": 10, "passingScore": 6 },
    "attempts": { "maxAttempts": 2, "timeLimitMinutes": 30 },
    "feedback": { "mode": "after-submit" },
    "presentation": { "mode": "continuous" },
    "items": {
      "q1": { "contentBlockId": "q1", "points": 2, "gradingKind": "deterministic" },
      "q2": { "contentBlockId": "q2", "points": 8, "gradingKind": "manual" }
    }
  }
}
```

Grading desativado nao e persistido pelo `quiz-content`: o campo `grading` e
omitido. `createDisabledGradingDefinition()` existe como valor de trabalho em
memoria, com `enabled: false`, `maxScore: 0` e maps vazios.

## Politicas centrais

### ScorePolicy

```ts
interface ScorePolicy {
  maxScore: number;
  passingScore?: number;
}
```

- `maxScore` deve ser finito e maior que zero quando grading esta ativo.
- `passingScore`, quando presente, deve estar entre zero e `maxScore`.
- `maxScore` e a escala total do conteudo.
- `items[*].points` e a distribuicao interna dos pontos.
- o contrato permite que `maxScore` seja diferente da soma dos items.

`passingScore` no package e um valor absoluto nessa mesma escala. Na API atual,
o resultado `Passed` usa `Program.PassingScore`, que e percentual de curso. Os
dois campos nao sao hoje a mesma fonte de verdade.

O plano canonico fecha a semantica alvo assim:

- todo score academico usa `ScoreValue` de largura fixa com quatro casas no
  PostgreSQL e no TypeScript/JSON; o dominio da API faz parse e calculo exato;
- `Assessment.PassingScore` e absoluto na escala do assessment e decide
  `AssessmentSubmission.Passed`;
- `Program.PassingScore` e `AssessmentGroup.WeightPercent` usam
  `PercentValue` de largura fixa e decidem consolidacao ponderada e aprovacao
  global.

Essa decisao ainda nao descreve o codigo atual: enquanto a fase de persistencia
nao for aplicada, os campos de assessment continuam inteiros e o service usa o
percentual do programa para a submission.

O formato alvo de score e `^\d{8}\.\d{4}$`, por exemplo
`"00000012.5000"`; percentuais usam `^\d{3}\.\d{4}$`, limitados no dominio a
`"100.0000"`. O banco nao usa `numeric/decimal` nem cast numerico. Largura fixa
permite filtrar e ordenar lexicalmente; soma, media, mediana e ponderacao
pertencem ao dominio da API e alimentam projecoes precomputadas. Colunas e
indices ordenaveis usam collation binaria/invariante explicita.

### AttemptPolicy

```ts
interface AttemptPolicy {
  maxAttempts?: number | null;
  timeLimitMinutes?: number | null;
  availableFrom?: string | null;
  availableUntil?: string | null;
  dueAt?: string | null;
  allowLateSubmissions?: boolean;
  lateSubmissionDeadline?: string | null;
}
```

Ausencia significa que a politica nao foi definida pelo conteudo. `null` e
preservado para os campos nullable. As strings de data nao possuem formato ou
ordenacao validados pelo package.

### FeedbackPolicy

```ts
type FeedbackMode = "immediate" | "after-submit" | "after-close" | "manual";
interface FeedbackPolicy { mode?: FeedbackMode }
```

Esse campo define quando o resultado/feedback pode ser apresentado. Ele nao e o
texto de feedback de uma pergunta e nao decide participacao no gradebook.

### PresentationPolicy

```ts
type PresentationMode = "continuous" | "single-step";
interface PresentationPolicy { mode?: PresentationMode }
```

Esse e o modo portavel do conteudo. Na API ele e projetado para
`AssessmentPresentationMode.Continuous | SingleStep`.

### GradedItemConfig

```ts
type GradingKind = "deterministic" | "manual" | "external" | "unsupported";

interface GradedItemConfig {
  contentBlockId: string;
  points: number;
  gradingKind: GradingKind;
  answerKeyRef?: string;
  rubricRef?: string;
}
```

A chave de `items` e o ID logico do item de grading. No adapter de quiz ela e
igual ao `block.id`, e `contentBlockId` repete esse ID para enderecar respostas.

Semantica de `gradingKind`:

| Valor | Significado |
| --- | --- |
| `deterministic` | existe answer key completo e avaliador confiavel |
| `manual` | a tentativa entra no fluxo confiavel, mas aguarda avaliacao humana |
| `external` | o resultado deve vir de um motor externo |
| `unsupported` | o item nao pode produzir nota oficial na implementacao atual |

`answerKeyRef` e `rubricRef` sao referencias genericas. O adapter atual de quiz
nao as produz e a sincronizacao dos items recria os objetos, portanto nao as
preserva automaticamente.

## Normalizacao e validacao runtime

As funcoes publicas sao:

| Funcao | Entrada | Saida/comportamento |
| --- | --- | --- |
| `createDisabledGradingDefinition()` | nenhuma | objeto desativado canonico em memoria |
| `normalizeGradingDefinition()` | `Partial`/null | aplica defaults e descarta valores nao reconhecidos |
| `validateGradingDefinition()` | `unknown` | normaliza e lanca `GradingConfigValidationError` em invariantes invalidas |
| `tryParseGradingDefinition()` | `unknown` | retorna somente definicao ativa valida; caso contrario `null` |
| `sumGradedItemPoints()` | definicao tipada | soma `items[*].points` |

Regras importantes:

- `schemaVersion` invalida e normalizada para `1`; qualquer inteiro positivo e
  aceito, inclusive uma versao futura desconhecida.
- modos desconhecidos de feedback/apresentacao sao removidos em vez de gerar
  issue.
- `gradingKind` desconhecido e normalizado para `manual`.
- item points invalido e normalizado para zero; pontos negativos sao depois
  rejeitados, mas zero e permitido.
- campos desconhecidos sao descartados pela normalizacao.
- nao ha schema Zod/JSON Schema publicado para esse contrato.
- datas sao copiadas como strings opacas.
- `answerKeyRef` e `rubricRef` apenas precisam ser truthy; formato e existencia
  nao sao validados.

`GradingConfigValidationError.issues` e uma lista de mensagens, sem codigo e sem
path estruturado separado.

## Embedding no content body

`CONTENT_GRADING_STORAGE_KEY` vale literalmente `"grading"`.

- `readContentGradingDefinition(body)` aceita somente objeto ja parseado; uma
  string JSON retorna `null`.
- `writeContentGradingDefinition(body, grading)` clona superficialmente o root.
- grading ativo e validado antes de ser escrito.
- grading ausente ou desativado remove a chave.
- esses helpers sao genericos e nao validam o restante do content body.

No quiz, `quiz-content` aplica uma camada mais forte:

- `enableQuizContentGrading()` cria items a partir dos blocos;
- `updateQuizContentGrading()` valida e sincroniza items;
- `syncQuizContentGrading()` remove grading desativado e elimina items orfaos;
- `parseQuizContentDocument()` reporta `invalid-grading` se a chave existir mas
  nao contiver uma definicao ativa valida;
- `quizContentItemsToDocument()` sincroniza grading antes do round-trip final.

## Contrato generico de adapter

```ts
interface GradingAdapter<TAuthoringPayload = unknown> {
  contentType: string;
  extractItems(payload: TAuthoringPayload): Record<string, GradedItemConfig>;
  extractAnswerKey(payload: TAuthoringPayload, grading: ContentGradingDefinition): AnswerKey;
  redactLearnerPayload(payload: TAuthoringPayload, grading: ContentGradingDefinition): unknown;
  buildStructuredAnswerPayload(input: unknown, grading: ContentGradingDefinition): StructuredAnswerPayload;
}
```

O core conhece apenas esse contrato. `registerGradingAdapter()` e
`getGradingAdapter()` usam um `Map<string, GradingAdapter>`. O registry nao faz
registro automatico; atualmente nenhum host chama `registerGradingAdapter()`.
O `quizGradingAdapter` e exportado, mas a implementacao atual chama seus helpers
dedicados diretamente.

Para um futuro tipo de conteudo, o adapter deve ser o unico lugar que conhece:

- como enumerar items autorais;
- onde esta o answer key;
- como gerar payload learner-safe;
- como converter input do aluno para o vocabulario estruturado.

## Adapter de quiz

### Formas de entrada aceitas

O adapter aceita:

```ts
type QuizAdapterPayload = readonly QuizBlockLike[] | QuizBlockStorageLike;

interface QuizBlockLike { id: string; type: string; data?: unknown }
interface QuizBlockStorageLike {
  order?: readonly (readonly [string, string])[];
  blocks?: Record<string, unknown>;
}
```

`QuizBlockStorageLike` e deliberadamente estrutural para aceitar o documento de
conteudo sem importar `quiz-content`. Apenas blocos cujo tipo externo e `quiz`
viram items de grading.

### Classificacao por pergunta

| `QuizEntryType` | Kind quando completo | Condicao principal |
| --- | --- | --- |
| `SINGLE_CHOICE` | `deterministic` | `correctOptionId` nao vazio e pertencente as options |
| `MULTIPLE_CHOICE` | `deterministic` | ao menos um `correctOptionId`, todos existentes |
| `TRUE_FALSE` | `deterministic` | `correctAnswer` booleano |
| `FILL_IN_THE_BLANK` | `deterministic` | todas as lacunas possuem key valida para seu input |
| `SHORT_ANSWER` | `deterministic` | ao menos uma resposta aceita nao vazia |
| `ESSAY` | `manual` | sempre entra como avaliacao manual |
| `MATCHING` | `deterministic` | todos os pares possuem id, left e right |
| `ORDERING` | `deterministic` | posicoes inteiras, unicas e dentro do intervalo |
| `CATEGORIZATION` | `deterministic` | categorias existem e todos os items apontam para IDs validos |
| `RATING` | `deterministic` | `correctRating` finito e dentro da escala, quando ela existe |
| `NUMERIC` | `unsupported` | avaliador oficial ainda nao implementado |
| `FORMULA` | `unsupported` | avaliador oficial ainda nao implementado |
| `HOTSPOT` | `deterministic` | dimensoes, coordenadas e zonas validas |
| `HIGHLIGHT` | `deterministic` | spans inteiros validos dentro de `plainText` |

Perguntas incompletas que seriam deterministicas sao classificadas como
`unsupported`, nunca promovidas para `manual` silenciosamente.

`buildQuizGradingItemsFromBlocks()` usa `question.points` positivo ou o default
`1`. `createQuizGradingDefinition()` usa a soma dos items como `maxScore` quando
o autor nao fornece outro total.

### AnswerKey

```ts
interface AnswerKey {
  items: Record<gradedItemId, unknown>;
}
```

O valor e `unknown` no core porque cada adapter possui formato proprio. Para
quiz, cada item conserva apenas material necessario para avaliar:

| Tipo | Material extraido |
| --- | --- |
| single/multiple/true-false | discriminante e resposta(s) correta(s) |
| fill blank | id da lacuna e key especifica do input |
| short answer | respostas aceitas e case sensitivity |
| essay | resposta esperada/plana e regra de formatacao |
| matching | `pair.id` e `pair.right` |
| ordering | `item.id` e `correctPosition` |
| categorization | `item.id` e `correctCategoryIds` |
| rating | `correctRating` |
| numeric/formula | variables, formula, tolerancia e casas decimais |
| hotspot | dimensoes e hotspots |
| highlight | spans corretos |

O answer key nao e parte de `ContentGradingDefinition`, nao e enviado ao aluno
e nao pertence a `StructuredAnswerPayload`. Hoje ele e extraido em memoria do
conteudo autoral; nao existe storage server-side versionado dedicado.

### Redaction learner-safe

Para perguntas que satisfazem a forma completa do dominio, o adapter delega a
`toQuizLearnerEntry()` de `@game-guild/quiz`. Para formas parciais, possui uma
redaction defensiva propria.

Principios:

- remove campos de answer key e feedback correto/incorreto;
- preserva apenas `feedback.general`;
- transforma matching, ordering e categorization em formas renderizaveis sem key;
- remove formula/tolerancia onde elas definem correcao;
- preserva apenas attachments learner-visible quando usa a projecao do dominio;
- em tipos desconhecidos, remove uma denylist de nomes comuns de answer key.

`redactQuizBlockStorage()` preserva o envelope e blocos nao quiz, e escreve a
definicao de grading validada no payload learner. Cada adapter futuro continua
responsavel por redigir somente o seu proprio tipo.

## Resposta estruturada atual

O vocabulario generico submetido pelo aluno e:

```ts
interface StructuredAnswer {
  selectedOptionIds?: string[];
  textAnswers?: Record<string, string>;
  categorizations?: Record<string, string[]>;
  ordering?: string[];
  rating?: number;
}

interface StructuredAnswerPayload {
  answers: Record<contentBlockId, StructuredAnswer>;
}
```

Exemplo:

```json
{
  "answers": {
    "q1": { "selectedOptionIds": ["true"] },
    "q2": { "textAnswers": { "main": "Resposta do aluno" } },
    "q3": { "ordering": ["item-2", "item-1", "item-3"] }
  }
}
```

`buildQuizStructuredAnswerPayload()`:

- aceita `{ answers: ... }` ou diretamente o record de respostas;
- quando grading esta ativo, aceita somente `contentBlockId` configurado;
- cria resposta vazia para item configurado sem input;
- remove IDs duplicados e strings vazias;
- converte valores de `textAnswers` para string;
- aceita `rating` apenas se finito;
- descarta campos de score, grade, correctness, feedback e answer key.

Mapeamento `QuizAnswer -> StructuredAnswer` pertence a `@game-guild/quiz`, em
[`answers.ts`](../../packages/features/quiz/src/answers/answers.ts). O adapter de
grading normaliza o envelope e aplica a whitelist; ele nao deve redefinir o
estado de UI da resposta.

Alguns tipos usam codificacao textual dentro do vocabulario generico:

- true/false: `selectedOptionIds: ["true" | "false"]`;
- matching: strings `pairId:rightValue`;
- essay: rich text serializado em `textAnswers.main` e plain text em `main_plain`;
- hotspot: `hotspot_x` e `hotspot_y` em `textAnswers`;
- highlight: JSON string em `textAnswers.highlight_spans`.

### Alvo aprovado para respostas

Na `SEQ-01`, esse vocabulário é removido sem dual-read. O core passa a persistir
`AssessmentResponseEnvelopeV1 { schemaVersion, contentType, payloadSchema,
payload }`, com payload opaco. `@game-guild/grading-adapter-quiz` possui
`QuizAnswerEnvelopeV1`, discriminado pelos 14 tipos de `QuizAnswer`, e elimina
delimitadores, JSON embutido em strings e coerção textual de coordenadas. O
manifest resolve o decoder exato, e TypeScript e C# compartilham fixtures.

## Resultado de grading atual

```ts
interface GradeItemResult {
  contentBlockId: string;
  status: "graded" | "pending" | "unsupported";
  score: number | null;
  maxScore: number;
  isCorrect?: boolean;
  feedback?: string;
}

interface GradeResult {
  status: "graded" | "pending" | "unsupported";
  score: number | null;
  maxScore: number;
  passed?: boolean;
  items?: GradeItemResult[];
  feedback?: string;
}
```

`GradeSubmissionArgs` junta definicao, payload, answer key opcional e
`contentBody` opcional. `gradeDeterministicQuizSubmission()` normaliza novamente
a resposta antes de avaliar, para impedir bypass da whitelist.

Agregacao atual:

- item deterministico correto recebe todos os seus pontos; incorreto recebe zero;
- item manual gera `pending` e torna o resultado total `pending` com score `null`;
- item unsupported torna o total `unsupported` com score `null`;
- `passed` so e calculado quando todos os items foram avaliados e
  `score.passingScore` esta definido;
- nao existe partial credit na agregacao oficial atual, mesmo que alguns tipos
  de quiz possuam flags autorais de partial credit.

`GradeResult` e um contrato de dominio/transporte. Ele nao e o mesmo objeto que
`AssessmentSubmission`; a integracao deve projetar seus campos para a entidade
persistida.

### Alvo aprovado para resultados

Rounds e stages passam a usar somente `GradeResultV1` e `GradeItemResultV1`
genéricos, com `itemId`, `ScoreValue`, estado, feedback, evidências e proveniência
do review. O core não referencia resultado específico de quiz,
`contentBlockId` ou outro
tipo específico. Evidência de quiz é versionada pelo adapter e referenciada pelo
resultado genérico.

## Projecao para Assessment

Ao salvar um quiz, `content-item-editor.tsx` le o grading do JSON e executa a
projecao atual:

| `ContentGradingDefinition` | Assessment atual | Situacao |
| --- | --- | --- |
| `enabled: false`/ausente | Assessment ligado e soft-deleted | implementado no host web |
| `enabled: true` | cria/restaura/atualiza Assessment por `ContentId` | implementado no host web |
| `score.maxScore` | `Assessment.MaxScore` | arredondado para inteiro e limitado ao minimo 1 |
| `score.passingScore` | nenhum campo de request atual | nao projetado |
| `attempts.maxAttempts` | `Assessment.MaxAttempts` | projetado |
| `attempts.timeLimitMinutes` | `Assessment.TimeLimitMinutes` | projetado |
| availability/due/late fields | campos equivalentes de Assessment | existem na API, mas nao sao copiados nessa reconciliacao de quiz |
| `feedback.mode` | nenhum campo de Assessment | nao projetado |
| `presentation.mode` | `Assessment.PresentationMode` | mapeado para enum C# |
| `items` | nenhuma tabela/item relacional | fica apenas no content JSON |
| `gradingKind` | `Assessment.GradingMethods` | nao e projetado por item; host usa `AutoGraded,InstructorGraded` fixo, sem escolha explicita do professor |
| `answerKeyRef`/`rubricRef` | `DefinitionPayload`/`RubricId` nao automaticos | sem mapeamento atual |

A projecao cria quiz com `SubmissionModalities = StructuredAnswer`. Titulo,
descricao e `IsRequired` vem do `ProgramContent`, nao de grading.

### O que pertence somente a Assessment

`Assessment` possui identidade e operacao que nao devem entrar no JSON de quiz:

- `CourseId`, `ContentId` e `AssessmentGroupId`;
- titulo/descricao operacional e ordem;
- `SubmissionModalities`;
- `GradingMethods` flags: `PeerReview`, `AIGraded`, `AutoGraded`,
  `InstructorGraded`;
- `GroupSetId`, `RubricId` e politica de peer review;
- disponibilidade operacional e regras de atraso;
- no modelo atual, `DefinitionPayload` para definicoes operacionais especificas;
  no alvo, o payload generico e removido e uma policy sem coluna so pode ter
  fonte mutavel tipada, com nome e ownership proprios.

No codigo atual, `GradingMethods` possui somente as quatro flags acima e o
backend ainda nao as executa como um pipeline completo. O plano canonico em
`docs/plans/quiz-grading-end-to-end` separa a fonte da revisao do efeito de
grading:

- **review** define quem ou o que analisa a submissao;
- **grading** transforma a analise em score, feedback e resultado oficial.

Por isso, o modelo alvo renomeia `GradingMethods` para `ReviewMethods`, adota
`AssessmentReviewMethod` e preserva os bits existentes:

| Nome atual | Nome alvo | Valor | Semantica alvo |
| --- | --- | ---: | --- |
| `PeerReview` | `PeerReview` | 1 | alunos analisam submissoes de outros alunos; as revisoes recebidas sao agregadas |
| `AIGraded` | `AIReview` | 2 | um provider de IA produz a revisao primaria |
| `AutoGraded` | `AutomatedReview` | 4 | o sistema executa correcao deterministica |
| `InstructorGraded` | `InstructorReview` | 8 | o instrutor revisa diretamente ou finaliza uma revisao primaria |
| inexistente | `SelfReview` | 16 | o proprio aluno realiza uma autoavaliacao estruturada |

Sao validos um unico metodo primario ou um metodo primario seguido de
`InstructorReview`:

```text
PeerReview
AIReview
AutomatedReview
InstructorReview
SelfReview
PeerReview,InstructorReview
AIReview,InstructorReview
AutomatedReview,InstructorReview
SelfReview,InstructorReview
```

Como a persistencia e bitmask, a ordem nao e armazenada. A precedencia final de
`InstructorReview` e uma regra canonica do dominio. Grupo e peso nao alteram a
escolha do workflow.

`PeerReview` participa do mesmo pipeline dos demais metodos. O codigo atual ja
persiste cada revisao individual em `AssessmentPeerReview`, mas ainda precisa
agregar os scores recebidos e projetar o resultado em `AssessmentSubmission`.
O limite atual `PeerReviewsRequiredCount` indica quantas revisoes cada aluno
deve realizar; ele nao substitui a quantidade minima de revisoes recebidas por
submissao nem a politica de agregacao necessarias ao resultado oficial.

O plano implementa a infraestrutura dos cinco metodos. `InstructorReview`,
`AutomatedReview` e `SelfReview` ganham fluxo integral; `PeerReview` reaproveita
a infraestrutura existente e recebe a agregacao oficial; `AIReview` recebe
contratos, registro de providers e bloqueios operacionais, sem incluir um
provider concreto. Sem provider registrado e compativel, `AIReview` nao pode
publicar. Provider temporariamente indisponivel no runtime mantem o estagio
pendente para retry e nao produz uma nota sintetica.

Essa e uma direcao alvo, nao uma descricao dos serializers atuais. Ate a
alteracao coordenada dos contratos e do baseline limpo de schema, os DTOs reais
continuam usando `GradingMethods`, os nomes `AIGraded`, `AutoGraded` e
`InstructorGraded`, e nao reconhecem o bit `16`. O plano nao preve migration
incremental, backfill ou compatibilidade legacy.

`ReviewMethods` pertence a `Assessment`, nao ao JSON autoral do quiz. O quiz
mantem questoes, respostas, criterios deterministas e definicoes de score; o
assessment mantem workflow, politicas operacionais e estado das revisoes. Um
contrato operacional versionado pode carregar configuracoes por metodo, como o
provider de IA, a agregacao de peers e as exigencias da autoavaliacao, sem
incluir segredos de provider. Sua fonte mutavel na API nao e um payload generico:
se alguma policy nao possuir coluna, ela exige contrato tipado e persistencia de
nome especifico aprovada no `SCHEMA-GATE`.

`Assessment.DefinitionPayload` e outro campo `jsonb`, versionado por
`DefinitionSchemaVersion`. Ele nao e automaticamente o
`ContentGradingDefinition` nem o `QuizContentDocument`; atualmente e uma area
separada usada por definicoes como coding assessment.

No modelo alvo, esse payload, sua versao e o setter generico sao removidos.
Policies complexas sem coluna relacional podem possuir somente uma fonte tipada
e nomeada, sem replicar tentativas, tempo, datas, passing score ou apresentacao.
A API materializa
`AssessmentExecutionPolicyV1` dessas fontes unicas. Prepare/test cria uma
`AssessmentDefinitionRevision` candidata e imutavel; publish ativa a mesma
revision quando o source hash ainda coincide. Submission oficial aponta para a
ativa e test run pode apontar para candidata ou ativa. A visibility do content
nao substitui esse lifecycle.

### Gradebook e peso

`AssessmentGroup` possui:

```text
Id, CourseId, Name, Description, WeightPercent, Order
```

`WeightPercent` pertence ao grupo, nao ao quiz e nao ao grading do conteudo.
Grupos com peso zero podem representar atividades com resultado confiavel que
nao contribuem para a nota global. Portanto nao existem `resultUse`,
`feedbackOnly` ou `gradebook` em `ContentGradingDefinition`.

### Tentativa coletiva

`AssessmentGroup` acima e uma categoria ponderada do gradebook. Ele nao e o
`CourseGroup` usado por `GroupAssignment`.

No codigo atual, uma entrega coletiva e replicada em varias
`AssessmentSubmission` por `FanOutGroupSubmitAsync`. No modelo alvo:

- uma tentativa de grupo possui uma unica `AssessmentSubmission` ligada ao
  `CourseGroup`;
- os integrantes sao congelados em `AssessmentSubmissionParticipant` no start;
- grading recebe apenas a submission e produz uma unica rodada e resultado;
- depois de `GradeResultFinalized`, o subsistema de grupos/gradebook cria uma
  projecao idempotente por enrollment;
- regrade acontece uma vez e atualiza as projecoes, sem recriar submissions.

### Raiz da execucao de grading no modelo alvo

Stages, rodadas, evidencias e resultados nao pertencem diretamente ao JSON do
quiz nem ao lifecycle mutavel da submission. O plano canonico introduz
`GradingExecution` como raiz persistente compartilhada:

```text
GradingExecution
  AssessmentTestRunSubjectId nullable
  AssessmentSubmissionId nullable
  DefinitionRevisionId
  EvaluationState
  ActiveGradeRoundId nullable
  EvaluationPayload versionado
  CHECK exatamente um owner preenchido
  UNIQUE parcial por owner
```

O owner relacional e exatamente um `AssessmentTestRunSubject` ou uma
`AssessmentSubmission`. Um test run possui um ou mais subjects sinteticos, cada
um com sua propria execucao, o que evita misturar resultados de alvos de peer
review. Nao existe `ownerId` polimorfico. Somente a submission oficial pode
possuir `AssessmentResultRelease`. Test run produz resultado diagnostico e
nunca estado ou evento de liberacao academica.

## Persistencia de submission na API

`AssessmentSubmission` guarda:

| Familia | Campos |
| --- | --- |
| identidade/tentativa | `AssessmentId`, `EnrollmentId`, `UserId`, `CourseGroupId`, `AttemptNumber` |
| lifecycle | `StartedAt`, `SubmittedAt`, `GradedAt`, `Status`, `IsLate` |
| payload | `SubmittedModalities` e payloads Text/File/Url/Code/Media/Project/StructuredAnswer |
| resultado confiavel | `Score`, `Passed`, `GradedBy`, `Feedback`, `RubricScoresPayload` |

`StructuredAnswerPayload` e recebido como string, verificado apenas com
`JsonDocument.Parse` e gravado em coluna PostgreSQL `jsonb`. A API garante JSON
valido e consistencia com o bit `SubmissionModality.StructuredAnswer`, mas nao
valida a forma de `StructuredAnswerPayload` definida no package TypeScript.

Os checks relacionais garantem:

- tentativa maior que zero;
- score nao negativo;
- combinacao consistente entre modalidades e payloads;
- unicidade de `(AssessmentId, EnrollmentId, AttemptNumber)`;
- bitmasks dentro dos valores suportados.

O endpoint manual de grade recebe `Score`, `GradedBy`, `Feedback` e
`RubricScores`. `Passed` e calculado pela API usando o percentual
`Program.PassingScore` convertido para a escala de `Assessment.MaxScore`.

## Fluxos completos

### Authoring e persistencia

```text
QuizEntry[]
  -> quizContentItemsToDocument
    -> syncQuizGradingDefinition
      -> QuizContentDocumentV1 { order, blocks, grading }
        -> ProgramContent.JsonBody (jsonb)
          -> reconcileQuizAssessment
            -> Assessment relacional por ContentId
```

### Entrega learner-safe

```text
ProgramContent.JsonBody autoral
  -> parseQuizContentDocument
    -> grading.enabled ? server-graded : local-practice
      -> toQuizLearnerContentDocument (server-graded)
        -> QuizPlayer sem answer key
```

### Submission desejada pelo contrato

```text
Record<blockId, QuizAnswer>
  -> toStructuredGradingAnswer por pergunta
    -> buildQuizStructuredAnswerPayload
      -> AssessmentSubmission.StructuredAnswerPayload
        -> answer key server-owned + grading definition snapshot
          -> GradingExecution
            -> GradeRoundV1
              -> GradeResult
                -> projecao Score / Passed / Feedback / GradedAt
```

Na implementacao atual, `ActivityComponent` ja monta
`{ answers: Record<blockId, StructuredAnswer> }`, mas envia pelo endpoint de
`ProgramContent` (`submitActivity`), nao pelo lifecycle de
`AssessmentSubmission`. O backend C# tambem nao executa
`gradeDeterministicQuizSubmission()`. O fluxo confiavel completo acima ainda nao
esta conectado ponta a ponta.

## Matriz de ownership para evolucao

| Conceito | Dono correto |
| --- | --- |
| stem, options, answer correta e configuracao da pergunta | `quiz` |
| lista, IDs e ordem das perguntas | `quiz-content` |
| pontos autorais da pergunta | `quiz`, como string canônica e projetados pelo adapter |
| configuracao autoral adicional de grading por item | `ContentGradingDefinitionV2` em `grading`, sem copiar pontos ou capability |
| tentativas, tempo, disponibilidade, passing score, result release e apresentacao operacional | `AssessmentExecutionPolicyV1` no Assessment backend |
| capability deterministica de uma pergunta quiz | manifest, projetado por `@game-guild/grading-adapter-quiz` |
| conversao `QuizAnswerV1 <-> AssessmentResponseEnvelopeV1` | `@game-guild/grading-adapter-quiz` |
| extracao de answer key do quiz | `@game-guild/grading-adapter-quiz` |
| redaction da forma completa de QuizEntry | `quiz`; adapter apenas orquestra/fallback defensivo |
| embedding e sync do grading no documento quiz | `quiz-content` |
| controles visuais de grading do quiz | host/surface, consumindo contratos sem redefini-los |
| criacao e remocao da projecao Assessment | camada integradora/aplicacao |
| fonte e sequencia da revisao (`ReviewMethods`) | Assessment backend |
| politicas de peer, IA, autoavaliacao e revisao docente | Assessment backend, em contrato operacional versionado |
| avaliacao individual produzida por um aluno revisor | `AssessmentPeerReview` |
| agregacao das revisoes em score e feedback | pipeline de grading do Assessment backend |
| stages, rodadas, evidencias e resultado autoritativo | `GradingExecution` no Assessment backend |
| owner da execucao | exatamente um entre `AssessmentTestRunSubject` e `AssessmentSubmission` |
| liberacao academica | `AssessmentResultRelease` por `GradeRoundId` unico; submission derivada pelo owner da `GradingExecution` |
| grupos, pesos e participacao no gradebook | Assessment/gradebook backend |
| resolucao do CourseGroup e snapshot de participantes | Assessment/group integration antes do grading |
| projecao de resultado coletivo por enrollment | Assessment/gradebook integration depois do grading |
| tentativa, submission e resultado oficial | Assessment backend |
| preferencia visual ou estado React | surface, nunca no JSON autoral |

Regras praticas:

1. Um novo campo que descreve a pergunta entra em `quiz`.
2. Um novo campo que vale para qualquer atividade avaliavel entra em `grading`.
3. Uma traducao especifica de quiz entra em `adapters/quiz`, nao no core.
4. Um campo de turma, curso, grupo, peso ou tentativa entra em Assessment.
5. Um campo de layout do editor/player entra na surface e nao e persistido no
   documento, salvo se alterar semanticamente a entrega.
6. O package `quiz` nunca deve importar `grading`; seu tipo de resposta deve
   continuar util tambem para pratica local.

## Matriz de garantias

| Contrato | Tipagem estatica | Validacao runtime | Persistencia atual |
| --- | --- | --- | --- |
| `ContentGradingDefinition` | forte | normalize/validate, mas permissivo em versao e datas | `ProgramContent.JsonBody.grading` |
| `GradedItemConfig` | forte | valida ID, points e kind apos normalizacao | dentro do grading JSON |
| `AnswerKey` | core opaco (`unknown`) | validacao especifica indireta pela classificacao quiz | nao possui storage dedicado |
| learner payload | quiz possui uniao forte | projecao explicita + fallback defensivo | entregue, nao deve ser persistido como autoral |
| `StructuredAnswerPayload` | forte no TS | whitelist no adapter; API valida apenas JSON | `AssessmentSubmissions.StructuredAnswerPayload` jsonb quando usa esse endpoint |
| `GradeResult` | forte no TS | produzido por helpers puros | nao e persistido como objeto unico |
| Assessment | forte no C# e client gerado | entidade, service e constraints EF/DB | tabelas relacionais |
| Assessment result | forte no C# | score bounds e lifecycle | colunas de `AssessmentSubmissions` |

## Lacunas e riscos atuais

1. **`GradingMethods` ainda nao executa o pipeline no backend.** O submit C# nao
   inicia os estagios de peer, IA, correcao deterministica, autoavaliacao ou
   revisao docente conforme as combinacoes. O avaliador deterministico do
   package tambem nao esta conectado. O modelo alvo `ReviewMethods` ainda nao
   foi aplicado, e `SelfReview = 16` nao existe no enum atual.

2. **Nao existe snapshot server-owned da definicao e do answer key por
   tentativa.** A avaliacao futura precisa evitar que uma edicao posterior do
   quiz mude o significado de uma submission antiga.

3. **A API nao valida o schema de `ContentGradingDefinition` nem de
   `StructuredAnswerPayload`.** `JsonBody` e `StructuredAnswerPayload` sao JSON
   generico no backend.

4. **`passingScore` possui duas semanticas ativas no codigo atual.** O package
   usa valor absoluto por conteudo; a API usa percentual global de `Program`.
   O plano alvo reserva `Assessment.PassingScore` para pontos absolutos da
   submission e `Program.PassingScore` para o percentual global do curso.

5. **`score.maxScore` pode divergir da soma dos items.** A sincronizacao de quiz
   troca os items, mas preserva um max score positivo antigo. Isso pode ser
   intencional para escala, porem exige regra explicita de reescala dos pontos.

6. **Policies possuem ownership duplicado e nao sao totalmente projetadas.** O reconcile atual copia
   maxScore, maxAttempts, timeLimit e presentation; nao copia datas, atraso,
   feedback mode ou passing score.

7. **`schemaVersion` nao despacha schemas.** Qualquer inteiro positivo e aceito.
   Nao existe parser V1 fechado nem rejeicao de versao futura.

8. **Datas sao strings opacas.** O package nao valida ISO 8601, timezone,
   intervalos ou dependencia entre due date e late deadline.

9. **A sincronizacao recria items.** Metadados genericos futuros como
   `answerKeyRef` e `rubricRef` podem ser perdidos ao sincronizar quiz.

10. **O registry nao esta em uso.** O contrato extensivel existe, mas nenhum
    adapter e registrado no host; quiz e chamado por exports diretos.

11. **O core e o adapter quiz compartilham o mesmo package.** Como
    `@game-guild/grading` depende de `@game-guild/quiz`, cada novo adapter pode
    aumentar dependencias do package central. A resolução aprovada para a
    `SEQ-01` é extrair e reescrever essa integração em
    `@game-guild/grading-adapter-quiz`, removendo a dependência de quiz do core,
    sem aliases ou reexports de compatibilidade.

12. **Existe contrato duplicado no editor antigo.** O arquivo
    [`apps/web/src/components/block-content-editor/lib/assessment/assessment-contracts.ts`](../../apps/web/src/components/block-content-editor/lib/assessment/assessment-contracts.ts)
    redefine `StructuredAnswerPayload` e ainda importa `BlockStorage` do editor.
    Ele nao deve se tornar uma segunda fonte de verdade.

13. **O wire format possui codificacoes textuais.** Matching, essay rich text,
    hotspot e highlight exigem parsing dentro de strings, reduzindo a forca do
    contrato e aumentando risco entre linguagens.

14. **Partial credit nao chega ao resultado oficial.** Flags autorais existem em
    alguns tipos, mas `gradeQuizAnswer()` concede todos os pontos ou zero.

15. **`AnswerKey.items` e `unknown`.** Isso preserva genericidade, mas nao existe
    discriminante/versionamento generico para validar o key depois de
    serializado fora do processo.

16. **Scores de assessment e peer review ainda sao inteiros, enquanto pesos e
    percentuais usam `decimal`.** Media, mediana, partial credit e a politica de
    nao persistir decimais exigem a conversao coordenada para `ScoreValue` e
    `PercentValue` no baseline limpo.

17. **`FanOutGroupSubmitAsync` duplica a submission coletiva por integrante.**
    O modelo alvo exige uma submission e um resultado por grupo, com snapshot de
    participantes e fan-out apenas das projecoes posteriores.

18. **O historico ainda nao possui `GradingExecution` como raiz compartilhada.**
    O alvo exige exatamente um owner relacional entre subject sintetico de test
    run e submission, mantendo release academico exclusivamente na submission
    oficial.

## Avaliacao de cobertura

A separacao conceitual atual e boa: quiz nao depende de grading, o documento de
quiz compoe os dois, answer key e resposta possuem caminhos separados, e
gradebook fica fora do JSON autoral. A cobertura do adapter de quiz abrange os
14 tipos e e conservadora na classificacao.

Classificacao atual:

- **contrato autoral generico:** bom, mas com validacao permissiva;
- **fronteira quiz/grading:** boa, com duplicacao defensiva em redaction;
- **seguranca learner-safe no TypeScript:** alta para formas conhecidas;
- **normalizacao da resposta submetida:** alta no adapter;
- **persistencia backend do wire format:** parcial, valida apenas JSON;
- **grading oficial ponta a ponta:** incompleto;
- **separacao de gradebook:** alta no modelo, dependente da projecao Assessment;
- **extensibilidade para novos tipos de conteudo:** contratualmente boa, mas o
  empacotamento dos adapters deve ser revisto antes de acumular dependencias.

## Checklist para evoluir grading

Ao adicionar um novo tipo de conteudo avaliavel:

1. manter o dominio autoral no package desse conteudo;
2. implementar um `GradingAdapter` sem importar UI;
3. definir IDs estaveis de items e regra de pontos;
4. definir classificacao deterministic/manual/external/unsupported;
5. definir answer key server-owned e seu versionamento;
6. definir learner projection sem material de correcao;
7. mapear a resposta tipada para `StructuredAnswer` ou versionar o vocabulario;
8. adicionar whitelist e testes contra campos de score/correctness enviados pelo cliente;
9. integrar embedding/sync no package de documento do conteudo;
10. integrar a projecao Assessment somente na camada de aplicacao;
11. definir snapshot de grading e answer key por tentativa;
12. implementar avaliacao confiavel no backend ou marcar como manual/external;
13. manter grupo/peso/gradebook fora de `ContentGradingDefinition`;
14. adicionar round-trips autoral -> storage -> learner e answer -> submission -> result;
15. atualizar este mapa e o mapa do tipo de conteudo.
