# GameGuild Lexical Surface

`@game-guild/lexical-surface` owns GameGuild's reusable Lexical rich-document
editing surface. It provides the editor composer, document nodes, document
plugins, toolbar, slash picker, page layout, and serialization helpers.

## Boundary

The package owns rich-document editing only. It does not know `BlockArrayEditor`,
block storage, grading, quiz, code-studio, markdown blocks, HTML blocks, courses,
or assessment routes.

Generic block insertion was intentionally removed from the surface. There is no
`+Block` control, block insert menu, or generic block embed node. The block-array
editor remains the only owner of inserting concrete content blocks.

## Public API

```ts
import {
  LexicalSurface,
  type LexicalSurfaceAdapters,
  type LexicalSurfaceFeatures,
} from '@game-guild/lexical-surface';
```

`LexicalSurface` accepts serialized Lexical state and emits serialized state in
`onChange`. Read-only surfaces strip persisted selection state before mounting to
avoid page scroll on hydration.

## Host Adapters

Mermaid, Vega-Lite, and asset-backed media need host-specific facilities such as
Monaco, project asset storage, upload dialogs, and app preferences. Those are
injected through `LexicalSurfaceProps.adapters`; the package never imports the
web block-content-editor to obtain them.

The adapter contract covers:

- Mermaid editor and viewer;
- Vega-Lite editor and viewer;
- media upload dialog;
- `asset://` URL detection and resolution.

A host can omit an adapter. The corresponding rich-document node still preserves
its serialized data and falls back to a simple source preview instead of loading
host implementation code.

## Essay Integration

Feature packages can use `LexicalSurface` for focused rich-text editing. Quiz
essay UI should use the public serialized state boundary and keep Lexical types
out of its domain contracts. The quiz React adapter supplies a focused feature
set and does not enable any block-array behavior.

## Validation

```bash
pnpm --filter @game-guild/lexical-surface typecheck
pnpm --filter @game-guild/lexical-surface test
```
