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
  type BaseMediaData,
  type LexicalSurfaceAdapters,
  type LexicalSurfaceFeatures,
  type MermaidData,
  type VegaLiteData,
} from '@game-guild/lexical-surface';
```

`LexicalSurface` accepts serialized Lexical state and emits serialized state in
`onChange`. Read-only surfaces strip persisted selection state before mounting to
avoid page scroll on hydration.

Feature controls are independent: `toolbar` controls the complete top toolbar,
`insertMenu` controls its Insert dropdown, and `picker` controls the `/` menu.
Document feature flags such as `table`, `mermaid`, and `media` control both the
command plugin and whether that option appears in either insertion menu.
As long as either insertion menu is enabled, its available feature plugins stay
mounted. When both `insertMenu` and `picker` are disabled, the document-feature
plugins exclusive to those catalogs are disabled as well.

```tsx
<LexicalSurface
  features={{
    toolbar: true,
    insertMenu: false,
    picker: true,
    mermaid: true,
  }}
/>
```

This configuration hides the toolbar Insert dropdown while keeping `/Mermaid`
available.

Document-feature payload types such as `MermaidData`, `VegaLiteData`, and
`BaseMediaData` are exported as rich-document data contracts. They are not block
array storage nodes.

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
