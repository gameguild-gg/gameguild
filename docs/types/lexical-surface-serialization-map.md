# Lexical Surface: mapa de tipos e serializacao

## Objetivo

Este documento mapeia o formato completo de conteudo editado por `@game-guild/lexical-surface`, os arquivos que definem cada parte do JSON e os dados relacionados que ficam fora do documento. Ele deve ser usado para:

- auditar cobertura de serializacao e desserializacao;
- localizar a fonte de verdade de cada `type` presente no JSON;
- distinguir estado persistido de estado derivado ou apenas visual;
- avaliar compatibilidade ao adicionar, remover ou alterar nodes;
- identificar os pontos em que existe apenas tipagem estatica, sem validacao em runtime.

## Resumo do contrato

O documento persistido e o `SerializedEditorState` do Lexical. O package nao cria um envelope adicional:

```ts
interface SerializedEditorState<
  T extends SerializedLexicalNode = SerializedLexicalNode,
> {
  root: SerializedRootNode<T>;
}
```

Na aplicacao web, esse objeto e salvo diretamente em `ProgramContent.JsonBody`, uma coluna PostgreSQL `jsonb`.

```text
ProgramContent.JsonBody
  -> SerializedEditorState
    -> root: SerializedRootNode
      -> children: SerializedLexicalNode[]
        -> nodes nativos ou customizados, discriminados por type
```

Fontes principais:

- API publica da surface: [`packages/features/lexical-surface/src/surface/lexical-surface.tsx`](../../packages/features/lexical-surface/src/surface/lexical-surface.tsx)
- schema registrado: [`packages/features/lexical-surface/src/schema/nodes.ts`](../../packages/features/lexical-surface/src/schema/nodes.ts)
- entrada do estado inicial: [`packages/features/lexical-surface/src/schema/initial-editor-state.ts`](../../packages/features/lexical-surface/src/schema/initial-editor-state.ts)
- saida por `editorState.toJSON()`: [`packages/features/lexical-surface/src/surface/editor-body.tsx`](../../packages/features/lexical-surface/src/surface/editor-body.tsx)
- integracao com `jsonBody`: [`apps/web/src/components/learning/console/courses/[course]/content/[contentId]/content-item-editor.tsx`](../../apps/web/src/components/learning/console/courses/%5Bcourse%5D/content/%5BcontentId%5D/content-item-editor.tsx)
- armazenamento da API: [`apps/api/Source/Modules/GameGuild.Learning.Courses/Entities/ProgramContent.cs`](../../apps/api/Source/Modules/GameGuild.Learning.Courses/Entities/ProgramContent.cs)

## Envelope raiz

Exemplo minimo:

```json
{
  "root": {
    "children": [
      {
        "children": [
          {
            "detail": 0,
            "format": 0,
            "mode": "normal",
            "style": "",
            "text": "Hello",
            "type": "text",
            "version": 1
          }
        ],
        "direction": null,
        "format": "",
        "indent": 0,
        "textFormat": 0,
        "textStyle": "",
        "type": "paragraph",
        "version": 1
      }
    ],
    "direction": null,
    "format": "",
    "indent": 0,
    "type": "root",
    "version": 1
  }
}
```

### Campos base

Os tipos base pertencem ao package `lexical`, na versao instalada atualmente (`0.49.0`).

| Base | Campos serializados |
| --- | --- |
| `SerializedLexicalNode` | `type: string`, `version: number`, `$?: Record<string, unknown>`, `$slots?: Record<string, SerializedLexicalNode>` |
| `SerializedElementNode` | todos os campos base mais `children`, `direction`, `format`, `indent`, `textFormat?`, `textStyle?` |
| `SerializedTextNode` | todos os campos base mais `detail`, `format`, `mode`, `style`, `text` |
| `SerializedRootNode` | a mesma estrutura de `SerializedElementNode`; e o valor de `SerializedEditorState.root` |
| `SerializedDecoratorBlockNode` | campos base mais `format`, usado pelos embeds alinhaveis |

Observacoes:

- `type` e o discriminante usado pelo Lexical para encontrar a classe registrada.
- `version` existe em todos os nodes. A propria documentacao interna do Lexical nao recomenda depender apenas desse numero para evolucao de schema.
- `format` de texto e um bitmask numerico; negrito, italico, sublinhado e outros formatos nao aparecem como booleanos separados.
- estilos inline de texto, como familia, tamanho e cores, ficam em `SerializedTextNode.style` como string CSS.
- `$` e `$slots` sao campos reservados pelo framework e podem surgir mesmo sem uso explicito do package.

## Nodes nativos

Todos os nodes abaixo sao registrados pela lista em [`schema/nodes.ts`](../../packages/features/lexical-surface/src/schema/nodes.ts), exceto os nodes centrais que o Lexical registra por padrao. Os contratos concretos pertencem aos packages oficiais indicados.

| `type` no JSON | Tipo base | Campos adicionais | Dono do contrato |
| --- | --- | --- | --- |
| `root` | element | nenhum | `lexical` |
| `paragraph` | element | `textFormat`, `textStyle` | `lexical` |
| `text` | text | `detail`, `format`, `mode`, `style`, `text` | `lexical` |
| `linebreak` | node | nenhum | `lexical` |
| `tab` | text | campos de text | `lexical` |
| `heading` | element | `tag: h1 | h2 | h3 | h4 | h5 | h6` | `@lexical/rich-text` |
| `quote` | element | `shadowRoot?: boolean` | `@lexical/rich-text` |
| `list` | element | `listType: number | bullet | check`, `start`, `tag: ul | ol` | `@lexical/list` |
| `listitem` | element | `checked: boolean | undefined`, `value` | `@lexical/list` |
| `link` | element | `url`, `rel?`, `target?`, `title?` | `@lexical/link` |
| `autolink` | link | campos de link mais `isUnlinked` | `@lexical/link` |
| `code` | element | `language: string | null | undefined`, `theme?` | `@lexical/code` / `@lexical/code-core` |
| `code-highlight` | text | campos de text mais `highlightType` | `@lexical/code` / `@lexical/code-core` |
| `table` | element | `colWidths?`, `rowStriping?`, `frozenColumnCount?`, `frozenRowCount?` | `@lexical/table` |
| `tablerow` | element | `height?` | `@lexical/table` |
| `tablecell` | element | `colSpan?`, `rowSpan?`, `headerState`, `width?`, `backgroundColor?`, `verticalAlign?` | `@lexical/table` |

Os feature flags de UI nao removem esses nodes do schema. Essa decisao e importante: um documento continua desserializavel mesmo quando o botao ou comando que cria determinado node esta desativado.

## Nodes customizados

Esta e a lista completa dos nodes customizados registrados atualmente.

### Estruturais

| `type` | Tipo base | Campos proprios | Fonte de verdade |
| --- | --- | --- | --- |
| `custom-list` | `SerializedListNode` | `listStyleType?`, `markerColor?` | [`schema/custom-list-node.tsx`](../../packages/features/lexical-surface/src/schema/custom-list-node.tsx) |
| `layout-container` | element | `templateColumns`, `borderAlwaysVisible?`, `borderStyle?: "solid"`, `borderColor?` | [`features/layout/layout-container-node.ts`](../../packages/features/lexical-surface/src/features/layout/layout-container-node.ts) |
| `layout-item` | element | nenhum alem dos campos de element | [`features/layout/layout-item-node.ts`](../../packages/features/lexical-surface/src/features/layout/layout-item-node.ts) |
| `collapsible-container` | element | `open`, `borderAlwaysVisible?`, `borderStyle?: "solid"`, `borderColor?` | [`features/collapsible/collapsible-container-node.ts`](../../packages/features/lexical-surface/src/features/collapsible/collapsible-container-node.ts) |
| `collapsible-title` | element | nenhum alem dos campos de element | [`features/collapsible/collapsible-title-node.ts`](../../packages/features/lexical-surface/src/features/collapsible/collapsible-title-node.ts) |
| `collapsible-content` | element | nenhum alem dos campos de element | [`features/collapsible/collapsible-content-node.ts`](../../packages/features/lexical-surface/src/features/collapsible/collapsible-content-node.ts) |
| `page` | element | nenhum alem dos campos de element | [`features/page/page-node.ts`](../../packages/features/lexical-surface/src/features/page/page-node.ts) |
| `page-content` | element | nenhum alem dos campos de element | [`features/page/page-content-node.ts`](../../packages/features/lexical-surface/src/features/page/page-content-node.ts) |

`custom-list.listStyleType` e normalizado para `decimal | upper-alpha | lower-alpha | upper-roman | lower-roman | decimal-leading-zero | disc | circle | square | greek-upper | circled | arrow | star`. `markerColor` aceita apenas cores hexadecimais validas e volta ao default quando o valor importado e invalido.

### Equacao, desenho e embeds

| `type` | Campos proprios | Fonte de verdade |
| --- | --- | --- |
| `equation` | `equation`, `inline`, `fontSize?`, `align?: left | center | right` | [`features/equation/equation-node.tsx`](../../packages/features/lexical-surface/src/features/equation/equation-node.tsx) |
| `excalidraw` | `data`, `width?`, `height?` | [`features/excalidraw/excalidraw-node.tsx`](../../packages/features/lexical-surface/src/features/excalidraw/excalidraw-node.tsx) |
| `youtube` | `videoID` mais `format` do DecoratorBlock | [`features/embeds/youtube-node.tsx`](../../packages/features/lexical-surface/src/features/embeds/youtube-node.tsx) |
| `tweet` | `id` mais `format` do DecoratorBlock | [`features/embeds/tweet-node.tsx`](../../packages/features/lexical-surface/src/features/embeds/tweet-node.tsx) |
| `figma` | `documentID` mais `format` do DecoratorBlock | [`features/embeds/figma-node.tsx`](../../packages/features/lexical-surface/src/features/embeds/figma-node.tsx) |

`excalidraw.data` e uma string contendo outro JSON, normalmente `{ appState, elements, files }`. Portanto, o JSON do documento valida apenas que `data` e string; a estrutura interna exige um segundo parse e pertence ao Excalidraw. Dimensoes com valor runtime `"inherit"` sao omitidas no `exportJSON()`.

### Sticky, admonition, button e divider

| `type` | Campos proprios | Fonte de verdade |
| --- | --- | --- |
| `sticky` | `text`, `color`, `style`, `size`, `xOffset`, `yOffset` | [`features/sticky/sticky-node.tsx`](../../packages/features/lexical-surface/src/features/sticky/sticky-node.tsx) |
| `lexical-admonition` | `admonitionType`, `title`, `content`, `design`, `customBorderColor`, `customTextColor` | [`features/admonition/admonition-node.tsx`](../../packages/features/lexical-surface/src/features/admonition/admonition-node.tsx) |
| `lexical-button` | `text`, `url`, `actionType`, `variant`, `btnSize`, configuracao de icone, paleta, cores customizadas e fonte | [`features/button/button-node.tsx`](../../packages/features/lexical-surface/src/features/button/button-node.tsx) |
| `lexical-divider` | `style`, `thickness`, `spacing`, `colorPalette`, `customColor` | [`features/divider/divider-node.tsx`](../../packages/features/lexical-surface/src/features/divider/divider-node.tsx) |

Valores fechados:

- `sticky.style`: `classic | formal | modern`; `sticky.size`: `wide | compact`.
- `admonitionType`: `note | abstract | info | tip | success | question | warning | failure | danger | bug | example | quote | important | caution | attention | hint | check | summary`.
- `design`: `default | compact | bordered | vertical-bar`.
- `button.actionType`: `url | download | copy | email`.
- `button.variant`: `solid | outline | soft | minimal`; `btnSize`: `sm | md | lg | xl | xxl`.
- `button.iconVariant`: `0 | 1 | 2`; `iconPosition`: `left | right | top | bottom`; `iconSize`: `sm | md | lg`.
- `button.colorPalette`: `blue | green | orange | red | custom`.
- `button.customColors`: `{ primary, secondary, text, hoverPrimary, hoverSecondary, hoverText } | null`.
- `button.fontFamily`: `sans | display | roboto`; `fontSize`: `sm | md | lg`.
- `divider.style`: `simple | double | dashed | dotted | gradient`.
- `divider.thickness`: `thin | medium | thick`; `spacing`: `xs | sm | md | lg | xl`.
- `divider.colorPalette`: `blue | green | orange | red | purple | custom`.

### Mermaid

O node persistido `lexical-mermaid` usa `SerializedMermaidLexicalNode`:

```ts
{
  type: "lexical-mermaid";
  version: number;
  code: string;
  diagramType: MermaidDiagramType;
  theme: MermaidThemeName;
  themeMode: MermaidThemeMode;
  title: string;
  caption: string;
  size: number;
}
```

Fontes:

- node e contrato persistido: [`features/mermaid/lexical/mermaid-node.tsx`](../../packages/features/lexical-surface/src/features/mermaid/lexical/mermaid-node.tsx)
- contrato do editor isolado: [`features/mermaid/mermaid-data.ts`](../../packages/features/lexical-surface/src/features/mermaid/mermaid-data.ts)

Tipos de diagrama: `flowchart`, `class`, `sequence`, `xyChart`, `radar`, `quadrant`, `sankey`, `state`, `c4context`, `architecture`, `er`, `gantt`, `pie`, `gitgraph`, `mindmap`, `journey`, `timeline`, `quadrantChart`, `requirement`, `c4Context`, `c4Container`, `c4Component`, `c4Dynamic`, `c4Deployment`, `treemap-beta`, `kanban`.

Temas: `default`, `dark`, `forest`, `neutral`, `base` e suas variantes `*-dark`. `themeMode`: `system | light | dark | both`.

Atencao: `MermaidData` usa o campo `type` para o tipo de diagrama e tambem declara `direction?` e `fontFamily?`. O node usa `diagramType` e nao persiste `direction` nem `fontFamily`. A conversao editor/node cobre o primeiro nome diferente, mas os dois campos opcionais nao fazem parte do round-trip do node atual.

### Vega-Lite

O node `lexical-vega-lite` serializa todos os campos de `VegaLiteData`:

```ts
interface VegaLiteData {
  spec: string;
  title?: string;
  caption?: string;
  size?: number;
  theme?: VegaTheme;
  themeMode?: "system" | "only-light" | "only-dark";
  layout?: "square" | "rectangular";
  attachments?: Record<string, VegaDataAttachment>;
}

interface VegaDataAttachment {
  name: string;
  assetUri: AssetUri;
  mimeType: "text/csv" | "application/json";
  size: number;
}
```

Fontes:

- dados: [`features/vega-lite/vega-lite-data.ts`](../../packages/features/lexical-surface/src/features/vega-lite/vega-lite-data.ts)
- node: [`features/vega-lite/lexical/vega-lite-node.tsx`](../../packages/features/lexical-surface/src/features/vega-lite/lexical/vega-lite-node.tsx)

`spec` e uma string contendo outro JSON. Os datasets nao sao embutidos: `attachments` guarda `asset://<uuid>` e metadados suficientes para identificar CSV ou JSON. O blob e resolvido pelo package de assets.

Temas Vega persistiveis: `default | excel | ggplot2 | quartz | vox | fivethirtyeight | latimes | urbaninstitute | googlecharts | powerbi`.

### Midia e galeria

O node `lexical-media` e definido em [`features/media/media-node.tsx`](../../packages/features/lexical-surface/src/features/media/media-node.tsx) e persiste:

```ts
{
  mediaType: "image" | "video" | "audio";
  src: string;
  alt: string;
  caption: string;
  size: number;
  videoType: string;
  embedType?: "direct" | "youtube" | "vimeo" | "dailymotion";
  audioType: string;
  embedAudioType?: "direct" | "youtube" | "spotify" | "soundcloud";
  galleryItems: BaseMediaData[];
  galleryColumns: number;
  galleryCaption: string;
  galleryAspect: "square" | "landscape" | "classic" | "auto";
  showCellCaptions?: boolean;
  showGalleryCaption?: boolean;
  showCaption?: boolean;
}
```

Cada item de galeria segue [`features/media/media-data.ts`](../../packages/features/lexical-surface/src/features/media/media-data.ts):

```ts
interface BaseMediaData {
  type: "image" | "video" | "audio";
  src: string;
  alt?: string;
  caption?: string;
  size?: number;
  isNew?: boolean;
  videoType?: string;
  embedType?: "direct" | "youtube" | "vimeo" | "dailymotion";
  audioType?: string;
  embedAudioType?: "direct" | "youtube" | "spotify" | "soundcloud";
  isPlaceholder?: boolean;
  isStatic?: boolean;
  gridPosition?: number;
}
```

`src` pode ser uma URL externa ou um `AssetUri`. No caso de asset gerenciado, o documento persiste somente `asset://<uuid>`. Os campos `isNew`, `isPlaceholder` e `isStatic` parecem transitorios pelo nome, mas atualmente pertencem ao tipo persistido de `galleryItems` e podem sair no JSON.

## Assets fora do documento

O documento Lexical guarda referencias, nao blobs. O contrato `AssetUri` tem o formato `asset://<uuid>` e e definido em [`packages/features/assets/src/core/asset-uri.ts`](../../packages/features/assets/src/core/asset-uri.ts).

O browser repository usa IndexedDB com tres stores, definidas em [`browser-storage-schema.ts`](../../packages/features/assets/src/browser/browser-storage-schema.ts):

| Store | Conteudo | Relacao com o documento |
| --- | --- | --- |
| `assets` | `AssetRecord`, localizacao, MIME, hash, disponibilidade e escopo | indexada pelo UUID do `asset://` |
| `objects` | `Blob`, tamanho e `refCount`, indexados por hash SHA-256 | nao entra no JSON do documento |
| `usages` | referencias por scope, consumidor e URI | inventario auxiliar; nao entra no JSON do documento |

O contrato completo do repositorio esta em [`asset-repository.ts`](../../packages/features/assets/src/repository/asset-repository.ts). URLs `blob:` retornadas por `createObjectUrl()` sao efemeras e nunca devem ser persistidas.

## Dados que nao pertencem ao JSON Lexical

| Dado | Onde vive | Persistencia |
| --- | --- | --- |
| `plainText` | calculado com `$getRoot().getTextContent()` em `editor-body.tsx` | derivado; emitido por callback, nao faz parte do documento |
| `PageSettings` | contexto da toolbar, recebido por `initialPageSettings` | atualmente nao e emitido por `LexicalSurface` nem incorporado ao `SerializedEditorState` |
| feature flags | props `LexicalSurfaceFeatures` | configuracao da instancia, nao documento |
| `readOnly`, `namespace`, classes e slots React | props da surface | runtime apenas |
| selecao | estado interno do Lexical | nao faz parte do contrato atual; `stripSelection()` remove uma selecao externa/antiga antes do read-only |
| preferencias Monaco/Shiki | IndexedDB `editor-preferences` | preferencia local do navegador, fora de `ProgramContent.JsonBody` |

`PageSettings` contem:

- `size`: `pageless | a4 | letter | legal | tabloid | a3 | a5 | b4 | b5 | statement | executive | folio`;
- `orientation`: `portrait | landscape`;
- `margin`: `none | narrow | normal | moderate | wide`.

O contrato esta em [`features/page/page-settings.ts`](../../packages/features/lexical-surface/src/features/page/page-settings.ts). Como a surface recebe apenas `initialPageSettings`, uma aplicacao que queira persistir essas escolhas precisa hoje criar um envelope externo ou adicionar um callback explicito.

As preferencias de editores Mermaid/Vega ficam em [`shared/ui/editor-preferences.ts`](../../packages/features/lexical-surface/src/shared/ui/editor-preferences.ts), no banco IndexedDB `editor-preferences`, store `preferences`, chave `editor-prefs`. Elas incluem tamanho de modal, tema Shiki, tamanho de fonte, line numbers, wrapping, minimap, tab size, whitespace e line highlight.

## Fluxo de serializacao

1. `OnChangePlugin` entrega um `EditorState` para `EditorBody`.
2. `EditorBody` chama `editorState.toJSON()`.
3. `LexicalSurface.onChange` recebe `SerializedEditorState`; `onContentChange` recebe o mesmo estado e o texto plano derivado.
4. `content-item-editor.tsx` mantem o ultimo estado em uma ref.
5. Ao salvar uma lesson com formato `Lexical`, a aplicacao envia esse objeto como `jsonBody`, sem envelope adicional.
6. A API recebe `JsonElement`, serializa para string e o EF Core grava em `program_contents.JsonBody` como `jsonb`.

## Fluxo de desserializacao

1. A API le a coluna `jsonb` e entrega `JsonBody` como `JsonElement` no DTO.
2. A aplicacao trata o objeto como `SerializedEditorState` para uma lesson Lexical.
3. `LexicalSurface` passa o estado para `buildInitialEditorState()`.
4. O editor executa `editor.parseEditorState(state)` e `editor.setEditorState(parsed)`.
5. O parser resolve cada `node.type` usando `LEXICAL_SURFACE_NODES`.
6. Cada node customizado executa seu `importJSON()`; o proximo `toJSON()` usa seu `exportJSON()`.

## Matriz de garantias

| Camada | Tipagem estatica | Validacao runtime | Round-trip |
| --- | --- | --- | --- |
| envelope `SerializedEditorState` | forte, mas generica | parser do Lexical | sim |
| nodes nativos | forte, mantida pelo Lexical | `importJSON` do Lexical | sim |
| nodes customizados | tipos concretos por arquivo | desigual; alguns normalizam, outros confiam no payload | sim, quando o payload e valido |
| registro de nodes | lista central unica | o parser falha para `type` desconhecido | sim |
| assets referenciados | `AssetUri` branded em TS | `parseAssetUri` valida UUID | sim; blobs ficam fora do documento |
| Mermaid e Vega spec | string no JSON externo | validacao ocorre no renderer/editor apos segundo parse | parcial no nivel estrutural interno |
| Excalidraw scene | string no JSON externo | biblioteca interpreta apos segundo parse | parcial no nivel estrutural interno |
| `PageSettings` | tipo forte | normalizacao visual | nao faz parte do documento atual |

## Lacunas e riscos atuais

1. **Nao existe uma uniao publica de todos os serialized nodes.** A surface aceita o tipo amplo `SerializedEditorState`, cujo parametro default e `SerializedLexicalNode`. Os tipos concretos existem, mas em sua maioria sao internos e nao compoem um `LexicalSurfaceSerializedNode` exportado.

2. **Nao existe um parser de schema do package para JSON desconhecido.** A garantia runtime principal e `editor.parseEditorState()`. Isso prova que o Lexical consegue montar o estado, mas nao produz um relatorio de validacao de dominio antes da montagem.

3. **`version` nao representa uma versao unica do documento.** Cada node possui seu proprio `version`; nao ha `schemaVersion` no envelope raiz nem uma politica de upgrade documentada para alteracoes customizadas.

4. **Ha JSON aninhado como string.** `excalidraw.data` e `vega.spec` exigem um segundo parse. TypeScript nao comprova a estrutura interna dessas strings.

5. **Ha divergencia entre `MermaidData` e o node persistido.** `type` vira `diagramType`, enquanto `direction` e `fontFamily` nao sao persistidos pelo node.

6. **Configuracao de pagina nao tem round-trip.** Nodes `page` e `page-content` preservam a arvore paginada, mas `PageSettings` e somente estado do contexto da toolbar.

7. **Alguns campos de galeria parecem estado de edicao.** `isNew`, `isPlaceholder` e `isStatic` estao no tipo serializado por transitividade e precisam ser assumidos como parte do contrato enquanto nao forem removidos explicitamente.

8. **A API armazena JSON generico.** `ProgramContent.JsonBody` nao valida o schema Lexical no backend; a API preserva qualquer objeto JSON que passe pelo DTO.

## Avaliacao de cobertura

O mapeamento de round-trip dentro do editor e amplo: todos os nodes customizados que a UI produz estao centralmente registrados e possuem caminho de `importJSON`/`exportJSON`; os feature flags nao comprometem leitura; e assets sao referenciados por IDs estaveis em vez de blobs ou object URLs.

A cobertura ainda nao e forte como contrato externo hostil. Ela depende do parser do Lexical e de casts na integracao web, sem uma uniao publica fechada, um schema runtime ou uma versao global de documento. Hoje a melhor classificacao e:

- **completude funcional interna:** alta;
- **seguranca de round-trip de documentos validos:** alta;
- **validacao de JSON arbitrario:** media/baixa;
- **evolucao versionada do schema customizado:** media/baixa;
- **separacao entre documento e blobs/assets:** alta.

## Checklist para evoluir o schema

Ao adicionar ou alterar um node:

1. definir seu `Serialized...Node` no mesmo modulo da classe;
2. implementar e testar `importJSON()` e `exportJSON()`;
3. registrar a classe em `LEXICAL_SURFACE_NODES` sem condicionar o registro a feature flags;
4. garantir defaults para campos novos e leitura de payloads sem esses campos;
5. registrar qualquer `AssetUri` em coletores de uso/portabilidade;
6. evitar strings com JSON aninhado quando um objeto tipado puder ser armazenado diretamente;
7. adicionar fixture de round-trip `JSON -> parseEditorState -> toJSON`;
8. atualizar este mapa com `type`, base, campos e arquivo fonte.
