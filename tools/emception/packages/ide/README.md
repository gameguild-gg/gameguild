# @emception/ide

Reactive `<Ide>` React 19 component + `<emception-ide>` custom-element wrapper for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception).

## Install

```bash
npm install @emception/ide @emception/browser @emception/sysroot
```

Peer dependencies: `react ^19`, `react-dom ^19`.

## Quick start — React

```tsx
import { Ide } from '@emception/ide';

export default function Page() {
  return (
    <Ide
      manifestUrl="/cdn/manifest.json"
      workspaceName="my-project"
      title="C++ Playground"
    />
  );
}
```

## Quick start — plain HTML (custom element)

```html
<script type="module">
  import { registerEmceptionIde } from '@emception/ide';
  registerEmceptionIde(); // registers <emception-ide>
</script>

<emception-ide
  manifest-url="/cdn/manifest.json"
  workspace-name="my-project"
  title="C++ Playground"
></emception-ide>
```

## Injecting a pre-booted API

Skip auto-boot by passing an `api` prop. The component renders immediately and all VFS/run calls are delegated to the injected API — useful when you share one `EmceptionAPI` instance across multiple components.

```tsx
import { createEmception } from '@emception/browser';
import { Ide } from '@emception/ide';

const em = await createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none' });

<Ide api={em} enableTerminal={false} onStdout={(t) => console.log(t)} />
```

## Props reference (`IdeProps`)

| Prop | Type | Default | Description |
|---|---|---|---|
| `title` | `string` | — | Header bar title |
| `manifestUrl` | `string` | `/cdn/manifest.json` | Sysroot manifest URL |
| `api` | `InjectedEmceptionAPI` | — | Skip boot; delegate to this API |
| `workspaceConfig` | `WorkspaceConfig` | — | Static workspace descriptor |
| `workspaceUrl` | `string` | — | URL to a `.workspace.json` bundle |
| `workspaceName` | `string` | — | Logical name; derives IDB/localStorage key as `emception:ws:<name>` |
| `enableFileExplorer` | `boolean` | `true` | Show file-explorer sidebar |
| `enableTabs` | `boolean` | `true` | Show editor tab strip |
| `enableTerminal` | `boolean` | `true` | Mount xterm.js terminal panel |
| `enableCanvas` | `boolean` | `true` | Show SDL canvas panel |
| `enableDocking` | `boolean` | `true` | Enable drag-and-drop docking |
| `enableWorkspace` | `boolean` | `true` | Persist workspace to localStorage |
| `fullscreen` | `boolean` | `false` | Portal IDE into `document.body` (fills viewport) |
| `onFullscreenChange` | `(fs: boolean) => void` | — | Fullscreen state change callback |
| `showHiddenFiles` | `boolean` | `false` | Show dot-files in explorer |
| `showSolutionFiles` | `boolean` | `false` | Show `*.solution.*` files |
| `canvasPath` | `string` | `/user/sdl-canvas` | VFS path of SDL canvas placeholder |
| `onStdout` | `(text: string) => void` | — | Stdout callback when terminal disabled |
| `onStderr` | `(text: string) => void` | — | Stderr callback when terminal disabled |
| `stdin` | `() => Promise<number>` | — | Stdin supplier when terminal disabled |
| `readOnly` | `boolean` | `false` | Disable all editor + VFS writes |
| `theme` | `string` | `'vs-dark'` | Monaco editor theme |

## Custom-element attribute reference

The `<emception-ide>` element maps kebab-case attributes to the React props above. Boolean props are present/absent attributes; string props are attribute values.

| Attribute | Prop | Type |
|---|---|---|
| `title` | `title` | string |
| `manifest-url` | `manifestUrl` | string |
| `workspace-name` | `workspaceName` | string |
| `workspace-url` | `workspaceUrl` | string |
| `enable-file-explorer` | `enableFileExplorer` | boolean |
| `enable-tabs` | `enableTabs` | boolean |
| `enable-terminal` | `enableTerminal` | boolean |
| `enable-canvas` | `enableCanvas` | boolean |
| `enable-docking` | `enableDocking` | boolean |
| `enable-workspace` | `enableWorkspace` | boolean |
| `fullscreen` | `fullscreen` | boolean |
| `show-hidden-files` | `showHiddenFiles` | boolean |
| `show-solution-files` | `showSolutionFiles` | boolean |
| `canvas-path` | `canvasPath` | string |
| `read-only` | `readOnly` | boolean |
| `theme` | `theme` | string |

JS-only props (not settable as HTML attributes): `api`, `workspaceConfig`, `onStdout`, `onStderr`, `stdin`, `onFullscreenChange`. Set via the element's JS properties or call `element.update({ … })`.

## Custom element — programmatic update

```js
const el = document.querySelector('emception-ide');
el.workspaceConfig = { files: [{ path: '/user/hello.c', content: '#include<stdio.h>\nint main(){puts("hi");}' }] };
el.onStdout = (line) => console.log('[out]', line);
el.update(); // apply new JS props
```

## Exports

```ts
import { Ide } from '@emception/ide';                       // React component
import type { IdeProps, InjectedEmceptionAPI } from '@emception/ide'; // types
import { registerEmceptionIde, EmceptionIdeElement, ELEMENT_NAME } from '@emception/ide'; // custom element
import type { EmceptionAPI } from '@emception/ide';          // re-export from @emception/core
```

## Migration from `@gameguild/emception-ui`

Replace the old default import with named imports:

```ts
// Before
import EmceptionUI from '@gameguild/emception-ui';
// After
import { Ide } from '@emception/ide';
```

The `workspaceName` prop now generates an isolated storage key (`emception:ws:<name>`) instead of sharing the single legacy key — existing state in `localStorage` under the legacy key is preserved on the first mount if you omit `workspaceName`.

## See also

- [`@emception/browser`](../browser/README.md) — `createEmception()`, boot options
- [`@emception/webcomponent`](../webcomponent/README.md) — lighter `<emception-run>` without the IDE chrome
- [`@emception/react`](../react/README.md) — `<EmceptionRun>`, `useEmception()` hooks
- [DX overhaul plan](../../docs/dx-overhaul-plan.md)
