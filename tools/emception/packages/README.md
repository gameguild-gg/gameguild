# `tools/emception/packages/` — emception monorepo

6 published packages:

| Package                                              | Tier    | Purpose                                                                                         |
| ---------------------------------------------------- | ------- | ----------------------------------------------------------------------------------------------- |
| [`emception`](core/)                                 | Core    | Types, EmceptionCore, VFS, tool registry, build resolver, test engine, presets, RuntimeAdapter, and published `cdn/*` runtime assets. |
| [`@gameguild/emception-browser`](browser/)           | Adapter | Web Worker spawn, IDB workspace store, OffscreenCanvas + SDL helpers, COI preflight.            |
| [`@gameguild/emception-xterm`](xterm/)               | I/O     | xterm.js stdin/stdout/stderr bridge.                                                            |
| [`@gameguild/emception-react`](react/)               | UI      | `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception()`.                 |
| [`@gameguild/emception-webcomponent`](webcomponent/) | UI      | `<emception-run>` custom element. No framework deps.                                            |
| [`@gameguild/emception-ide`](ide/)                   | UI      | Reactive `<Ide>` React 19 + `<emception-ide>` custom-element wrapper (light DOM).               |

The unscoped meta-package [`emception`](../package.json) at `tools/emception/` forwards exports from `@gameguild/emception-browser` + `@gameguild/emception-xterm`.

## Live Demo

Try the packages in action at [gameguild-gg.github.io/gameguild/](https://gameguild-gg.github.io/gameguild/) — features a live IDE with working templates for C++, SDL3, Raylib, CMake, and Python.

## Build order (topological)

```
emception (core + cdn)
  → emception-browser, node (parallel)
    → emception-xterm
      → emception-react, emception-webcomponent, emception-ide (parallel)
        → emception (meta)
```

## Scripts

```bash
npm run typecheck:packages --workspace=tools/emception
npm run build:packages     --workspace=tools/emception
npm run clean:packages     --workspace=tools/emception

# meta package
cd tools/emception && npm run build:lib
```
