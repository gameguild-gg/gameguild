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

The package exposes one public entry point. Internal feature, schema, and UI
paths are intentionally not exported.

```ts
import {
    LexicalSurface,
    type BaseMediaData,
    type LexicalSurfaceAdapters,
    type LexicalSurfaceFeatures,
    type MermaidData,
    type VegaLiteData,
} from "@game-guild/lexical-surface";
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

## Internal Architecture

The source tree is organized by responsibility:

- `surface` wires the public component, editable layout, and plugin host;
- `schema` owns registered nodes, theme, and initial-state handling;
- `capabilities` owns feature policy and the shared insertion catalog;
- `editor-ui` owns formatting, toolbars, picker, shortcuts, and editor menus;
- `features` keeps each document feature's node, component, plugin, and commands
  together;
- `integrations` defines host adapter contracts;
- `shared` contains cross-feature Lexical helpers and UI primitives.

Code inside the package uses relative imports. Only consumers outside the
package import `@game-guild/lexical-surface`.

## Host Adapters

Mermaid and Vega-Lite are complete package-owned features. Their editors,
Monaco integration, validators, templates, viewers, themes, data loaders, and
export utilities are bundled with `@game-guild/lexical-surface` and work
without host adapters.

The package also owns its Shiki-to-Monaco integration and syntax-theme
catalog. Mermaid and Vega-Lite share a persisted global Monaco theme while
resolving the appropriate light or dark Shiki variant for the active color
mode.

`LexicalSurfaceProps.adapters` remains available only for host-specific
facilities such as project asset storage and upload dialogs. These stay injected
because the package must not own an application's persistence policy.

The adapter contract covers the media upload dialog and `asset://` URL detection
and resolution. Mermaid and Vega-Lite do not participate in that contract.

A host can omit all adapters and still edit and render Mermaid and Vega-Lite
nodes. Asset-backed media preserves unresolved `asset://` values when no asset
resolver is supplied.

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
