# @gameguild/emception-webcomponent

`<emception-run>` custom element for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Framework-free, no React/Vue/Svelte runtime required.

## Live Demo

Try it at [gameguild-gg.github.io/gameguild/](https://gameguild-gg.github.io/gameguild/) — features a live IDE with working templates for C++, SDL3, Raylib, CMake, and Python.

## Install

```bash
npm install @gameguild/emception-webcomponent @gameguild/emception-browser
```

`@gameguild/emception-browser` is a peer of the orchestration story. The
custom element itself only depends on `emception` (core) for the typed
attribute schema and event registry.

## Use

Importing the package once auto-registers the element under
`<emception-run>`:

```html
<script type="module">
  import '@gameguild/emception-webcomponent';
</script>

<emception-run preset="cpp" autorun source="int main(){return 0;}"></emception-run>
```

Pair the element with a pre-built `EmceptionAPI`:

```ts
import '@gameguild/emception-webcomponent';
import { createEmception } from '@gameguild/emception-browser';

const el = document.querySelector('emception-run')!;
const api = await createEmception({ tty: 'none' });
(el as any).api = api;
```

## Attributes

Every kebab-case attribute in `ATTRIBUTE_SCHEMA` (exported from
`emception`) is mirrored on the element. Common ones:

| attribute                                  | type                | meaning                                |
| ------------------------------------------ | ------------------- | -------------------------------------- |
| `preset`                                   | string              | `'cpp' \| 'c' \| 'sdl' \| ...`         |
| `manifest-url`                             | URL                 | sysroot manifest override              |
| `source`                                   | string              | inline single-source convenience       |
| `seed-url` / `build-url`                   | URL                 | remote workspace seed / build config   |
| `autorun`                                  | boolean (presence)  | auto-execute on ready                  |
| `canvas`                                   | boolean (presence)  | show the `<slot name="canvas">` region |
| `flags` / `ldflags` / `libs`               | space-or-comma list | folded into the workspace build config |
| `include-paths` / `lib-paths`              | list                | folded into `workspace.build`          |
| `output`                                   | string              | output filename                        |

Unknown attributes are ignored.

## Slots

```html
<emception-run preset="cpp" canvas>
  <textarea slot="stdin">my stdin payload</textarea>
  <canvas slot="canvas" width="640" height="480"></canvas>
</emception-run>
```

The slot regions auto-hide when no slotted child (or no `canvas`
attribute) is present.

## Events

The element dispatches bubbling and composed lifecycle events for its own
compile-and-run cycle:

| event             | payload                            |
| ----------------- | ---------------------------------- |
| `emception-ready` | `{}` after the first successful run |
| `emception-exit`  | `{ exitCode, finalPhase }`          |

```ts
el.addEventListener('emception-exit', (ev) => {
  console.log((ev as CustomEvent).detail.exitCode);
});
```

## Properties + methods

- `el.api` — get/set the attached `EmceptionAPI`. Setting it enables
  compile-and-run operations; clearing it detaches the runtime.
- `el.readConfig()` — snapshot the current attributes as a parsed
  `ViewConfigInput`.
- `el.run()` — compile and run the configured source imperatively.

## Custom tag name

```ts
import { EmceptionRunElement, registerEmceptionRun } from '@gameguild/emception-webcomponent';
registerEmceptionRun('my-emception');
```

`registerEmceptionRun` is idempotent and returns the constructor that
ended up registered.

## SSR

The module's auto-register block is gated on `typeof customElements`,
so importing it under Node is a no-op (no errors). For React 19 use
`@gameguild/emception-react` which renders the element declaratively without
forcing the side-effect import on the server bundle.
