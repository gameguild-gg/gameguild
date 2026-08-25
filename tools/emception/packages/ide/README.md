# @gameguild/emception-ide

A vanilla React 19 IDE component with editor, file explorer, tabs, terminal,
canvas, workspace persistence, and a custom-element wrapper.

The package is product-agnostic. It contains no host-application integration
and does not patch or instantiate generated WebAssembly glue. Compilation,
testing, workspace I/O, and canvas execution go through the public
`@gameguild/emception-browser` API.

## Install

```bash
npm install @gameguild/emception-ide @gameguild/emception-browser react react-dom
```

The package declares Monaco, resizable panels, and terminal packages through
its peer dependency contract.

## React

```tsx
import { Ide } from '@gameguild/emception-ide';

export default function Playground() {
  return (
    <Ide
      title="C++ Playground"
      workspaceName="my-project"
      enableCanvas
    />
  );
}
```

Omit `manifestUrl` to inherit the Browser package's version-matched toolchain.
Set it only when serving the same canonical artifacts from your own origin.

## Inject a shared Browser API

```tsx
import { createEmception } from '@gameguild/emception-browser';
import { Ide } from '@gameguild/emception-ide';

const emception = await createEmception({ tty: 'none' });

<Ide api={emception} enableTerminal={false} />;
```

The caller owns disposal of an injected API. When no API is provided, the IDE
creates and disposes its own `BrowserEmceptionAPI` instance.

## Custom element

```html
<script type="module">
  import { registerEmceptionIde } from '@gameguild/emception-ide';
  registerEmceptionIde();
</script>

<emception-ide workspace-name="my-project" enable-canvas></emception-ide>
```

## Main props

| Prop | Purpose |
| --- | --- |
| `api` | inject an existing `BrowserEmceptionAPI` |
| `manifestUrl` | opt into a self-hosted canonical release |
| `workspaceConfig` / `workspaceUrl` | provide the vanilla workspace |
| `workspaceName` | isolate browser storage as `emception:ws:<name>` |
| `enableFileExplorer`, `enableTabs`, `enableTerminal`, `enableCanvas` | select IDE panels |
| `readOnly`, `showHiddenFiles`, `showSolutionFiles` | control editing and visibility |
| `onStdout`, `onStderr`, `stdin` | use terminal-free stream callbacks |

## Exports

```ts
import {
  Ide,
  registerEmceptionIde,
  EmceptionIdeElement,
  ELEMENT_NAME,
} from '@gameguild/emception-ide';
import type {
  IdeProps,
  InjectedEmceptionAPI,
  WorkspaceConfig,
} from '@gameguild/emception-ide';
```

Workspace presets are convenience data; all execution is delegated to the
Browser API. Host-specific grading, permissions, private tests, and product UI
belong in a separate consumer package.
