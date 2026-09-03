# Quiz Surface: mapa de tipos e serializacao

## Objetivo

Este documento mapeia os contratos completos de `@game-guild/quiz`, `@game-guild/quiz-content` e `@game-guild/quiz-surface`, o envelope JSON persistido pela aplicacao e os dados relacionados que nao pertencem ao documento autoral. Ele deve ser usado para:

- auditar cobertura de serializacao e desserializacao das perguntas;
- localizar a fonte de verdade de cada discriminante e campo;
- distinguir conteudo autoral, payload learner-safe, respostas e grading;
- verificar o que e persistido em `ProgramContent.JsonBody`;
- identificar garantias apenas estaticas e validacoes realmente executadas em runtime.

## Limites entre packages

| Package | Responsabilidade | Possui formato persistido proprio? |
| --- | --- | --- |
| `@game-guild/quiz` | dominio: perguntas, respostas, projecao learner-safe, validacao autoral e avaliacao local | sim, no nivel de `QuizEntry`; nao define o envelope de colecao |
| `@game-guild/quiz-content` | documento versionado, colecao ordenada, parse de `unknown`, grading e projecao learner do documento inteiro | sim, `QuizContentDocumentV1` |
| `@game-guild/quiz-surface` | editores, players, drag-and-drop e adaptadores de apresentacao | nao; recebe perguntas de `quiz` e items de `quiz-content` |
| `@game-guild/block-list` | envelope generico ordenado de blocos | sim, `BlockStorage<TType, TData>` |
| `@game-guild/grading` | configuracao de grading, answer key, redaction e resposta estruturada | sim, como campo irmao `grading` no content body |
| `@game-guild/assets` | IDs, metadados, blobs e resolucao de assets | o quiz persiste apenas `asset://<uuid>` |

Fontes principais:

- perguntas autorais: [`packages/features/quiz/src/questions/question-types.ts`](../../packages/features/quiz/src/questions/question-types.ts)
- schemas runtime das perguntas: [`packages/features/quiz/src/questions/question-schemas.ts`](../../packages/features/quiz/src/questions/question-schemas.ts)
- documento e colecao: [`packages/features/quiz-content/src/types.ts`](../../packages/features/quiz-content/src/types.ts)
- parser do documento: [`packages/features/quiz-content/src/parsing.ts`](../../packages/features/quiz-content/src/parsing.ts)
- conversoes de storage: [`packages/features/quiz-content/src/storage.ts`](../../packages/features/quiz-content/src/storage.ts)
- composicao de grading: [`packages/features/quiz-content/src/grading.ts`](../../packages/features/quiz-content/src/grading.ts)
- projecao learner do documento: [`packages/features/quiz-content/src/learner.ts`](../../packages/features/quiz-content/src/learner.ts)
- contratos learner-safe: [`packages/features/quiz/src/contracts/contracts.ts`](../../packages/features/quiz/src/contracts/contracts.ts)
- respostas: [`packages/features/quiz/src/answers/answers.ts`](../../packages/features/quiz/src/answers/answers.ts)
- editor de colecao: [`packages/features/quiz-surface/src/editor/quiz-collection-editor.tsx`](../../packages/features/quiz-surface/src/editor/quiz-collection-editor.tsx)
- integracao e envelope persistido: [`apps/web/src/components/learning/console/courses/[course]/content/[contentId]/quiz-content-editor.tsx`](../../apps/web/src/components/learning/console/courses/%5Bcourse%5D/content/%5BcontentId%5D/quiz-content-editor.tsx)
- storage generico: [`packages/features/block-list/src/types.ts`](../../packages/features/block-list/src/types.ts)
- grading: [`packages/features/grading/src/types.ts`](../../packages/features/grading/src/types.ts)

## JSON persistido pela aplicacao

`quiz-surface` edita `QuizContentItem[]`. `@game-guild/quiz-content` e o unico dono da conversao dessa lista para o documento persistido:

```ts
interface QuizContentDocumentV1 {
  schemaVersion: 1;
  order: readonly [id: string, type: "quiz"][];
  blocks: Record<string, QuizEntry>;
  grading?: ContentGradingDefinition;
}
```

Exemplo:

```json
{
  "schemaVersion": 1,
  "order": [
    ["1", "quiz"],
    ["2", "quiz"]
  ],
  "blocks": {
    "1": {
      "type": "TRUE_FALSE",
      "stem": "The Earth revolves around the Sun.",
      "correctAnswer": true,
      "points": 1,
      "settings": {
        "allowRetry": true,
        "showFeedback": true,
        "showCorrectAnswer": true
      }
    },
    "2": {
      "type": "SHORT_ANSWER",
      "stem": "Largest planet?",
      "acceptedAnswers": ["Jupiter"],
      "caseSensitive": false,
      "settings": {
        "allowRetry": true
      }
    }
  },
  "grading": {
    "enabled": true,
    "schemaVersion": 1,
    "score": {
      "maxScore": 2,
      "passingScore": 1
    },
    "attempts": {},
    "feedback": {
      "mode": "immediate"
    },
    "presentation": {
      "mode": "continuous"
    },
    "items": {
      "1": {
        "contentBlockId": "1",
        "points": 1,
        "gradingKind": "deterministic"
      },
      "2": {
        "contentBlockId": "2",
        "points": 1,
        "gradingKind": "deterministic"
      }
    }
  }
}
```

Regras do envelope:

- `schemaVersion` e obrigatoriamente `1`; payloads sem versao ou com versao futura sao recusados.
- `order` define ordem, ID e tipo; o payload nao repete o ID.
- `blocks[id]` contem diretamente um `QuizEntry`, sem `{ type: "quiz", data: ... }` externo.
- o discriminante do bloco e o segundo item de `order`: `"quiz"`.
- o discriminante da pergunta e `blocks[id].type`, com valores em caixa alta.
- `grading` so e gravado quando `enabled === true`; os helpers de [`grading.ts`](../../packages/features/quiz-content/src/grading.ts) omitem grading desativado e sincronizam `grading.items` com os IDs validos.
- IDs sao strings estaveis nao vazias. `nextQuizContentItemId()` gera strings numericas por conveniencia, mas o contrato nao exige IDs sequenciais.
- campos desconhecidos no root ou nas perguntas sao reportados como invalidos.
- duplicatas, payloads ausentes, entradas invalidas e orfaos sao reportados e removidos do documento normalizado.

O mapa concreto `quiz -> QuizEntry` e `QuizBlockDataMap`, definido em `quiz-content`. O antigo editor de blocos apenas referencia esse tipo enquanto ainda suporta blocos quiz; ele nao e mais fonte de verdade. `block-list` permanece generico e nao conhece quiz.

## Contrato comum de pergunta

Todos os membros de `QuizEntry` possuem:

```ts
interface QuizEntryBase {
  stem: string;
  points?: number;
  feedback?: QuizFeedback;
  settings: QuizSettings;
  attachments?: QuizAuthoringAttachments;
}

interface QuizFeedback {
  correct?: string;
  incorrect?: string;
  general?: string;
}

interface QuizSettings {
  allowRetry: boolean;
  shuffleOptions?: boolean;
  showFeedback?: boolean;
  showCorrectAnswer?: boolean;
}
```

Esse é o tipo atual. Na `SEQ-01`, `points` passa atomicamente para string
canônica compatível com `ScoreValue`; números deixam de ser aceitos sem
dual-read. O valor continua pertencendo ao quiz e é projetado pelo
`@game-guild/grading-adapter-quiz`.

`QuizEntry` e uma uniao discriminada fechada com 14 tipos. A fonte de verdade e [`question-types.ts`](../../packages/features/quiz/src/questions/question-types.ts).

## Tipos autorais de pergunta

### Escolhas e respostas curtas

| `QuizEntryType` | Campos especificos | Resposta correta autoral |
| --- | --- | --- |
| `SINGLE_CHOICE` | `options: { id, text }[]` | `correctOptionId: string` |
| `MULTIPLE_CHOICE` | `options: { id, text }[]`, `selectionLimit?` | `correctOptionIds: string[]` |
| `TRUE_FALSE` | nenhum outro campo de prompt | `correctAnswer: boolean` |
| `SHORT_ANSWER` | `caseSensitive?` | `acceptedAnswers: string[]` |

### Fill in the blank

`FILL_IN_THE_BLANK` persiste `blanks: FillBlankField[]`:

```ts
interface FillBlankField {
  id: string;
  position: number;
  input: FillBlankInput;
}
```

`input` tambem e uma uniao discriminada:

| `FillBlankInputType` | Campos |
| --- | --- |
| `TEXT` | `acceptedAnswers: string[]`, `caseSensitive?` |
| `NUMBER` | `correctValue`, `tolerance?`, `requiredPrecision?`, `unit?`, `requireUnit?`, `allowNegative?` |
| `DROPDOWN` | `options: string[]`; por convencao autoral, o primeiro item e correto |
| `WORDBANK` | `words: string[]`; por convencao autoral, o primeiro item e correto |

O `stem` usa marcadores `___` ou `_answer_`, enquanto `blanks.position` liga a configuracao a uma lacuna detectada.

### Essay

`ESSAY` persiste:

```ts
{
  minWordCount?: number;
  maxWordCount?: number;
  showWordCount?: boolean;
  correctAnswer?: Record<string, unknown> | null;
  correctAnswerPlain?: string;
  requireFormatting?: boolean;
}
```

`correctAnswer` comporta um estado rich text, atualmente tipado de forma opaca como `SerializedRichTextPayload`. Ele pode conter um `SerializedEditorState` Lexical, mas o package de quiz evita depender diretamente dos tipos do Lexical.

### Matching, ordering e categorization

| `QuizEntryType` | Campos especificos |
| --- | --- |
| `MATCHING` | `pairs: { id, left, right }[]`, `rightOptions?`, `distractors?`, `allowPartialCredit?` |
| `ORDERING` | `items: { id, text, correctPosition }[]`, `allowPartialCredit?` |
| `CATEGORIZATION` | `categories: { id, name, description? }[]`, `items: { id, text, correctCategoryIds[] }[]` |

### Rating

`RATING` persiste:

```ts
{
  scale: {
    min: number;
    max: number;
    step: number;
    minLabel?: string;
    maxLabel?: string;
  };
  correctRating?: number;
}
```

Sem `correctRating`, qualquer valor da escala e aceito no modo de pratica local. Para grading oficial, a ausencia da resposta correta torna o item `unsupported`.

### Numeric e formula

`NUMERIC` e `FORMULA` compartilham:

```ts
interface FormulaVariable {
  id: string;
  name: string;
  min: number;
  max: number;
  decimals: number;
}

{
  variables: FormulaVariable[];
  formula: string;
  toleranceType: "absolute" | "percentage";
  tolerance: number;
  decimalPlaces: number;
}
```

Em `NUMERIC`, a formula calcula a resposta numerica esperada. Em `FORMULA`, o aluno fornece uma expressao que deve produzir os mesmos resultados da formula autoral.

A projecao learner de `FORMULA` pode receber um prompt gerado pelo servidor:

```ts
interface FormulaLearnerPrompt {
  variables: Record<string, number>;
  expectedResult: number;
  decimalPlaces?: number;
}
```

### Hotspot

`HOTSPOT` persiste:

```ts
{
  imageAssetUri: AssetUri | null;
  imageWidth: number;
  imageHeight: number;
  hotspots: Array<{
    id: string;
    x: number;
    y: number;
    zones: Array<{
      radius: number;
      label: string;
    }>;
  }>;
}
```

`x`, `y` e `radius` sao percentuais. `imageWidth` e `imageHeight` guardam as dimensoes naturais usadas no calculo geometrico.

### Highlight

`HIGHLIGHT` persiste:

```ts
{
  sourceText: string;
  plainText: string;
  highlights: Array<{
    start: number;
    end: number;
  }>;
}
```

`sourceText` conserva a sintaxe autoral com marcadores. `plainText` e o texto apresentado. `start` e inclusivo e `end` e exclusivo, ambos relativos a `plainText`.

## Attachments e assets

Qualquer pergunta pode possuir:

```ts
interface QuizAttachment {
  assetUri: AssetUri;
  role: "question" | "answer" | "feedback" | "source";
  label?: string;
  altText?: string;
}

interface QuizAuthoringAttachments {
  learnerVisible?: QuizAttachment[];
  authorOnly?: QuizAttachment[];
}
```

O coletor [`collect-quiz-asset-uris.ts`](../../packages/features/quiz/src/assets/collect-quiz-asset-uris.ts) encontra:

- todos os attachments `learnerVisible`;
- todos os attachments `authorOnly`;
- `imageAssetUri` de uma pergunta `HOTSPOT`.

O JSON guarda somente `asset://<uuid>`. Metadados e blobs ficam no IndexedDB gerenciado por `@game-guild/assets`; object URLs sao efemeras e nao pertencem ao quiz.

## Projecao learner-safe

`QuizAuthoringEntry` e um alias de `QuizEntry`. `toQuizLearnerEntry()` cria a uniao `QuizLearnerEntry`, removendo resposta correta e anexos autorais. A implementacao esta em [`contracts.ts`](../../packages/features/quiz/src/contracts/contracts.ts).

Regras comuns:

- `feedback.correct` e `feedback.incorrect` sao removidos; somente `feedback.general` pode permanecer.
- `attachments.authorOnly` e removido; `learnerVisible` e clonado. A verificacao de cada `AssetUri` pertence a `validateQuizAuthoringEntry()`.
- configuracoes de apresentacao e `points` permanecem.

| Tipo | Removido/transformado na projecao learner-safe |
| --- | --- |
| `SINGLE_CHOICE` | remove `correctOptionId` |
| `MULTIPLE_CHOICE` | remove `correctOptionIds`; preserva `selectionLimit` |
| `TRUE_FALSE` | remove `correctAnswer` |
| `FILL_IN_THE_BLANK` | text remove respostas; number remove `correctValue` e `tolerance`; dropdown/word bank rotacionam a primeira opcao para nao denunciar a correta pela posicao |
| `SHORT_ANSWER` | remove `acceptedAnswers` e `caseSensitive` |
| `ESSAY` | remove `correctAnswer`, `correctAnswerPlain` e `requireFormatting` |
| `MATCHING` | pares ficam `{ id, left }`; valores direitos e distractors viram `rightOptions` reordenadas |
| `ORDERING` | itens ficam `{ id, text }`, sem `correctPosition` |
| `CATEGORIZATION` | itens ficam `{ id, text }`, sem `correctCategoryIds` |
| `RATING` | remove `correctRating` |
| `NUMERIC` | remove tolerancia, mas atualmente preserva `formula` como parte do prompt numerico |
| `FORMULA` | remove formula e tolerancia; pode receber `prompt` gerado pelo servidor |
| `HOTSPOT` | remove `hotspots`; preserva imagem e dimensoes |
| `HIGHLIGHT` | remove `sourceText` e `highlights`; preserva `plainText` |

`QuizPracticeEntry` e intencionalmente autoral: pratica local precisa da resposta correta no cliente. `QuizPlayer` recebe somente `QuizLearnerEntry` e nao possui acesso ao answer key.

Na entrega atual da aplicacao, [`prepareQuizContentForLearner()`](../../apps/web/src/lib/courses/server-actions.ts) aplica a redaction quando `grading.enabled` esta ativo. Conteudo de pratica sem grading retorna autoral para permitir avaliacao local; portanto, nesse modo as respostas corretas estao disponiveis no cliente por design.

## Respostas do usuario

`QuizAnswer` e outra uniao discriminada, definida em [`answers.ts`](../../packages/features/quiz/src/answers/answers.ts):

| Tipo | Forma da resposta |
| --- | --- |
| `SINGLE_CHOICE` | `{ optionId: string | null }` |
| `MULTIPLE_CHOICE` | `{ optionIds: string[] }` |
| `TRUE_FALSE` | `{ value: boolean | null }` |
| `FILL_IN_THE_BLANK` | `{ values: Record<blankId, string> }` |
| `SHORT_ANSWER` | `{ value: string }` |
| `ESSAY` | `{ richText: SerializedRichTextPayload, plainText: string }` |
| `MATCHING` | `{ matches: Record<pairId, rightValue> }` |
| `ORDERING` | `{ itemIds: string[] }` |
| `CATEGORIZATION` | `{ categoryIdsByItem: Record<itemId, categoryId[]> }` |
| `RATING` | `{ value: number | null }` |
| `NUMERIC` | `{ value: string }` |
| `FORMULA` | `{ expression: string }` |
| `HOTSPOT` | `{ point: { x, y } | null }` |
| `HIGHLIGHT` | `{ spans: { start, end }[] }` |

Funcoes de round-trip:

- `createEmptyQuizAnswer(type)` cria o estado inicial correto para cada discriminante.
- `normalizeQuizAnswer(type, unknown)` faz normalizacao defensiva de estado de resposta desconhecido.
- `toStructuredGradingAnswer()` converte para o vocabulario de transporte.
- `fromStructuredGradingAnswer()` reconstrui `QuizAnswer` a partir desse vocabulario.

## Payload estruturado de grading atual

O contrato de transporte intencionalmente pequeno e:

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

Mapeamentos especiais:

- true/false usa `selectedOptionIds: ["true" | "false"]`;
- essay serializa `richText` com `JSON.stringify` em `textAnswers.main` e texto plano em `main_plain`;
- matching codifica cada par como `"pairId:rightValue"` em `selectedOptionIds`;
- hotspot usa `textAnswers.hotspot_x` e `hotspot_y`;
- highlight usa JSON string em `textAnswers.highlight_spans`;
- formula pode usar JSON string em `textAnswers.formula_values` apenas no adaptador interno do renderer.

O adaptador de grading em [`structured-answer.ts`](../../packages/features/grading/src/adapters/quiz/structured-answer.ts) normaliza valores, remove duplicatas, restringe respostas aos `contentBlockId` configurados e bloqueia nomes de campos que poderiam transportar answer key, score ou alegacoes de correcao.

Na arquitetura alvo, esse formato é removido. O core recebe
`AssessmentResponseEnvelopeV1`, e `@game-guild/grading-adapter-quiz` fornece o
payload `QuizAnswerEnvelopeV1` discriminado pelos 14 tipos, sem strings
delimitadas, JSON embutido ou coordenadas textualizadas.

## Grading atual no content body

O mapa completo dos contratos, adapters, projecao para Assessment, submissions
e fronteiras de ownership esta em
[`grading-serialization-map.md`](./grading-serialization-map.md).

`ContentGradingDefinition`, de [`grading/src/types.ts`](../../packages/features/grading/src/types.ts), e persistido como campo irmao de `order` e `blocks`:

```ts
interface ContentGradingDefinition {
  enabled: boolean;
  schemaVersion: number;
  score: {
    maxScore: number;
    passingScore?: number;
  };
  attempts: {
    maxAttempts?: number | null;
    timeLimitMinutes?: number | null;
    availableFrom?: string | null;
    availableUntil?: string | null;
    dueAt?: string | null;
    allowLateSubmissions?: boolean;
    lateSubmissionDeadline?: string | null;
  };
  feedback: {
    mode?: "immediate" | "after-submit" | "after-close" | "manual";
  };
  presentation: {
    mode?: "continuous" | "single-step";
  };
  items: Record<string, {
    contentBlockId: string;
    points: number;
    gradingKind: "deterministic" | "manual" | "external" | "unsupported";
    answerKeyRef?: string;
    rubricRef?: string;
  }>;
}
```

Na `ContentGradingDefinitionV2` alvo, cada valor de `items` contém somente
configuração autoral adicional, como `rubricRef`. ID, pontos, tipo e
`gradingKind` não são copiados: o ID é a chave, os pontos vêm de `QuizEntry` e
capabilities executáveis pertencem ao manifest.

Nao existe `resultUse`, `feedbackOnly` ou `gradebook` nesse JSON. Esses termos nao pertencem mais ao contrato de grading. A decisao sobre uso do resultado e feita pelas estruturas de assessment, grupos e pesos, fora do `QuizEntry` e fora de `ContentGradingDefinition`.

O adaptador [`items.ts`](../../packages/features/grading/src/adapters/quiz/items.ts) sincroniza cada bloco com um item de grading:

- `ESSAY` e `manual`;
- tipos com answer key completo sao `deterministic`, quando suportados;
- `NUMERIC` e `FORMULA` ainda sao `unsupported` no grading oficial;
- perguntas incompletas sao classificadas conservadoramente como `unsupported`.

O answer key e extraido separadamente por [`answer-key.ts`](../../packages/features/grading/src/adapters/quiz/answer-key.ts). Ele nao pertence ao payload submetido pelo aluno.

## Estado do quiz-surface que nao e persistido no documento

| Dado | Tipo/arquivo | Persistencia |
| --- | --- | --- |
| item controlado do editor | `QuizContentItem { id, entry }` em [`quiz-content/src/types.ts`](../../packages/features/quiz-content/src/types.ts) | convertido para `order` + `blocks`; wrapper nao e persistido diretamente |
| modo de submissao | `local-practice | server-graded` | prop de UI |
| `readOnly` e drag state | props/estado React | runtime apenas |
| resposta ativa e fase | `QuizSessionState` em [`quiz-session-reducer.ts`](../../packages/features/quiz-surface/src/player/quiz-session-reducer.ts) | runtime; nao pertence ao authoring JSON |
| estado do renderer | `RendererAnswerState` em [`renderer-answer-adapter.ts`](../../packages/features/quiz-surface/src/player/renderer-answer-adapter.ts) | adaptador interno; nao e API publica persistida |
| resultado visual | `QuizSubmissionResult` em [`quiz-player.tsx`](../../packages/features/quiz-surface/src/player/quiz-player.tsx) | controlado pelo host/runtime |
| tamanho do modal | IndexedDB em [`quiz-editor-preferences.ts`](../../packages/features/quiz-surface/src/editor/chrome/quiz-editor-preferences.ts) | preferencia local, fora de `jsonBody` |
| blobs de attachments | IndexedDB do package assets | fora do quiz; referenciados por `AssetUri` |

As preferencias usam o banco IndexedDB `quiz-surface-editor-preferences`, store `preferences`, chave `modal-size`. Valores validos: `compact | widescreen | ultrawide | fullscreen`.

## Fluxo de serializacao autoral

1. `QuizEditorSurface` recebe e edita um `QuizEntry` controlado.
2. `QuizCollectionEditor` recebe e emite a lista ordenada de `QuizContentItem`.
3. `quizContentItemsToDocument()` converte os items, acrescenta `schemaVersion: 1` e sincroniza grading.
4. `serializeQuizContentDocument()` exige que o documento completo seja canonico.
5. `quiz-content-editor.tsx` mantem esse documento como estado, sem importar estruturas do editor antigo.
6. `content-item-editor.tsx` envia o documento final diretamente como `jsonBody`.
7. A API recebe `JsonElement`, serializa para string e grava em `ProgramContent.JsonBody` (`jsonb`).

## Fluxo de desserializacao autoral

1. A API devolve `JsonBody` como objeto JSON generico.
2. `parseQuizContentDocument()` valida versao, root, ordem, IDs, payloads, perguntas e grading a partir de `unknown`.
3. `quizEntrySchema` discrimina e valida estruturalmente todos os 14 tipos e seus objetos aninhados.
4. O parser devolve o documento normalizado junto de uma lista precisa de issues.
5. `quizDocumentToContentItems()` produz a colecao consumida pela surface.
6. `QuizEditorSurface` usa `entry.type` para escolher o editor e o player no registry.
7. Ao salvar, `serializeQuizContentDocument()` valida novamente o formato canonico.

## Fluxo learner e submission

```text
QuizEntry autoral
  -> toQuizLearnerEntry
    -> QuizLearnerEntry sem answer key
      -> QuizPlayer + QuizAnswer
        -> toStructuredGradingAnswer
          -> StructuredAnswerPayload por contentBlockId
            -> grading adapter / API
```

No modo de pratica:

```text
QuizPracticeEntry (= QuizAuthoringEntry)
  + QuizAnswer
    -> evaluateQuizAnswer
      -> correct | incorrect | pending | unsupported
```

O resultado de avaliacao local e definido em [`evaluate-answer.ts`](../../packages/features/quiz/src/evaluation/evaluate-answer.ts). Ele nao e persistido no documento autoral.

## Matriz de garantias

| Camada | Tipagem estatica | Validacao runtime | Round-trip |
| --- | --- | --- | --- |
| `QuizEntry` | forte, uniao fechada por `type` | `quizEntrySchema` faz parse estrutural de `unknown`; `validateQuizAuthoringEntry()` valida completude semantica | sim para objetos validos |
| `QuizLearnerEntry` | forte, uniao fechada | construido por funcao de projecao explicita | unidirecional por seguranca |
| `QuizAnswer` | forte, uniao fechada | `normalizeQuizAnswer()` aceita `unknown` defensivamente | sim com structured answer, com codificacoes especiais |
| `QuizContentDocumentV1` | forte, versionado e especializado | parser estrito com issues e normalizacao previsivel | sim para documentos canonicos |
| `grading` | contrato forte e versionado | normalize/validate/tryParse em runtime | sim |
| `AssetUri` | branded em TypeScript | UUID validado por `isAssetUri()` | sim; blobs externos ao JSON |
| API `JsonBody` | `JsonElement` generico | nao valida `QuizContentDocumentV1` ou cada `QuizEntry` | preserva JSON aceito |

## Lacunas e riscos atuais

1. **A API armazena JSON generico.** O backend nao aplica o schema de quiz ao gravar `ProgramContent.JsonBody`; a garantia estrutural hoje esta nos consumidores TypeScript de `quiz-content`.

2. **Issues de parse ainda nao possuem telemetria de produto.** O parser retorna caminhos e codigos precisos, mas o editor atualmente abre o documento normalizado sem apresentar esses problemas ao autor.

3. **Nao existem migrations de documento.** Isso e intencional enquanto o produto nao foi lancado: somente V1 e aceito. Uma futura V2 exigira dispatcher explicito, sem parser permissivo.

4. **Rich text e JSON aninhado perdem precisao.** `SerializedRichTextPayload` e `Record<string, unknown> | null`; no transporte de essay ele ainda e convertido para string JSON dentro de `textAnswers.main`.

5. **Algumas respostas usam codificacao textual.** Matching usa `left:right`, hotspot usa strings numericas e highlight usa JSON string. Isso reduz a forca estrutural do wire format e exige parse adicional.

6. **Pratica local expoe answer key ao cliente.** Esse comportamento e necessario para avaliacao local e e intencional, mas nao deve ser confundido com entrega learner-safe de um quiz com grading oficial.

7. **`NUMERIC` e `FORMULA` nao possuem grading oficial completo.** O dominio e a pratica local suportam avaliacao, mas o adapter oficial os marca `unsupported` ate existir avaliador confiavel no servidor.

## Avaliacao de cobertura

O dominio tem boa cobertura horizontal: os 14 tipos aparecem nas unioes autorais, learner-safe e de resposta; os registries de editor/player cobrem os mesmos discriminantes; existe redaction explicita; attachments usam IDs; e grading possui normalizacao, validacao, answer key separado e whitelist de payload submetido.

A borda TypeScript de persistencia agora e estrita e versionada. A fragilidade restante e que o backend continua aceitando `JsonElement` generico e depende dos consumidores para aplicar o contrato. Hoje a melhor classificacao e:

- **completude funcional dos tipos de pergunta:** alta;
- **separacao autoral/learner-safe:** alta;
- **normalizacao de respostas submetidas:** alta;
- **validacao de JSON autoral arbitrario:** alta na camada TypeScript;
- **versionamento do documento:** alto para V1, sem migrations futuras ainda;
- **separacao entre documento e assets:** alta;
- **grading oficial por tipo:** alta para tipos deterministas comuns, incompleta para numeric/formula.

## Checklist para evoluir o schema

Ao adicionar ou alterar um tipo de pergunta:

1. atualizar `QuizEntryType` e a uniao `QuizEntry`;
2. atualizar o membro correspondente de `quizEntrySchema` e seus schemas aninhados;
3. definir a forma autoral e seus campos de answer key;
4. adicionar a projecao correspondente em `QuizLearnerEntry` e `toQuizLearnerEntry()`;
5. atualizar `validateQuizAuthoringEntry()`;
6. adicionar `QuizAnswer`, empty state, normalizacao e conversores structured answer;
7. registrar editor e player em `quiz-surface`;
8. atualizar avaliacao local e classificacao do grading adapter;
9. atualizar extracao de answer key, redaction e inventario de grading;
10. atualizar coleta de `AssetUri` quando houver novos campos de asset;
11. adicionar fixtures e testes de autoral -> documento -> learner, answer -> wire -> answer e items -> storage -> items;
12. introduzir uma nova versao de documento caso a mudanca nao seja compativel com V1;
13. atualizar este mapa com todos os campos e arquivos fonte.
