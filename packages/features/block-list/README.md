# GameGuild Block List

`@game-guild/block-list` is the framework-independent foundation for an ordered
collection of typed content blocks. It owns structure and data conversion only;
the application that uses it owns the block catalog, payload schemas, UI, and
rendering.

## Architecture

The package keeps one runtime representation and two derived representations:

```text
Consumer-owned type map
        |
        v
Block[]  <---- list.ts ---->  Block[]
   |                              |
   | storage.ts                   | view.ts
   v                              v
BlockStorage                   BlockView[]
{ order, blocks }               { id, type, data, version }
```

`Block[]` is the mutable-in-practice, immutable-by-helper runtime model used by
an editor or application state. `BlockStorage` is the compact persisted model.
`BlockView` is the canonical read model for renderers and previews.

The package never decides which block types exist. A consumer creates that
knowledge with a map from a type identifier to its payload type; `TypedBlock`
then converts the map into a discriminated union. This retains TypeScript
narrowing without coupling the package to quiz, markdown, media, or any other
feature.

```ts
interface EditorBlockDataMap {
  quiz: QuizEntry;
  markdown: MarkdownData;
}

type EditorBlock = TypedBlock<EditorBlockDataMap>;
type EditorBlocks = TypedBlockList<EditorBlockDataMap>;
```

With `EditorBlock`, checking `block.type === 'quiz'` narrows `block.data` to
`QuizEntry`. The package only sees a generic `{ id, type, data }` block.

## Module Boundaries

| Module | Responsibility |
| --- | --- |
| `types.ts` | Generic contracts and type-map helpers. |
| `ids.ts` | Monotonic numeric ID generation. |
| `list.ts` | Immutable ordered-list operations. |
| `storage.ts` | Runtime/storage conversion and JSON serialization. |
| `validation.ts` | Guards and normalization for untrusted storage input. |
| `view.ts` | Generic `BlockView` projection for read-only consumers. |

The dependency boundary is intentional. This package does not import React,
Next.js, Lexical, quiz, grading, editor UI, concrete block nodes, or icons.
Consumers may use it from those layers, but no dependency flows back into this
package.

## Contract

```ts
interface Block<TType extends string = string, TData = unknown> {
  id: string;
  type: TType;
  data: TData;
}

type BlockList<TBlock extends Block = Block> = TBlock[];

interface BlockStorage<TType extends string = string, TData = unknown> {
  order: Array<readonly [id: string, type: TType]>;
  blocks: Record<string, TData>;
}
```

### Storage Format and Invariants

`order` is the source of truth for display order and block type. `blocks` is an
ID-indexed payload map. Separating them keeps the persisted JSON stable and
means moving a block only changes ordering metadata.

```json
{
  "order": [["1", "markdown"], ["2", "quiz"]],
  "blocks": {
    "1": { "content": "Welcome" },
    "2": { "question": "..." }
  }
}
```

Normalization accepts unknown data but keeps only entries that have a string ID,
a string type, and a matching payload in `blocks`. Orphaned payloads and invalid
order entries are discarded. `deserializeBlockList` returns `[]` for malformed
JSON rather than throwing.

`nextBlockId` returns one greater than the highest non-negative integer ID in a
list. IDs are not recycled after deletion.

### Read Model

`blockToView` and `blocksToViews` produce the same shape for every block type:

```ts
interface BlockView<TType extends string = string, TData = unknown> {
  id: string;
  type: TType;
  data: TData;
  version: number;
}
```

The payload is always in `data`; consumers must not create type-specific fields
such as `entry` for one block kind. Type-specific adaptation belongs in the
consumer that knows that domain.

## Responsibilities

- Generate monotonic sequential block IDs.
- Insert, update, remove, move, find, and query blocks without mutating the
  input array.
- Convert between runtime `BlockList` and persisted `BlockStorage`.
- Serialize and deserialize storage JSON.
- Normalize unknown storage-like input before reconstructing blocks.
- Produce a generic `{ id, type, data, version }` read model.

## Non-Responsibilities

- Concrete block type catalogs or payload schemas.
- Block editor and preview UI.
- Quiz, grading, React, Next.js, or Lexical behavior.
- Type-specific payload reshaping or rendering adapters.

## Usage

```ts
import {
  blocksToStorage,
  blocksToViews,
  insertBlock,
  nextBlockId,
  storageToBlocks,
  type TypedBlockList,
} from '@game-guild/block-list';

const blocks: TypedBlockList<EditorBlockDataMap> = [];
const id = nextBlockId(blocks);
const next = insertBlock(blocks, 0, {
  id,
  type: 'markdown',
  data: { content: 'Welcome' },
});

const persisted = blocksToStorage(next);
const restored = storageToBlocks(persisted);
const views = blocksToViews(restored);
```

List helpers always return a new array. `updateBlock` retains object identity for
unchanged blocks, which makes it suitable for state-management and UI rendering
layers. Insert and move indices are bounded to the list; an invalid/non-finite
insert destination appends.

## Validation

The package tests cover ID generation, list operations, storage roundtrips,
normalization, invalid input, and generic `BlockView` projection.

```bash
pnpm --filter @game-guild/block-list test
pnpm --filter @game-guild/block-list typecheck
```
