# emception

A complete C/C++ toolchain — clang, lld, wasm-opt, emcc — running entirely in the browser as WebAssembly. Compile, link, and execute C/C++ from a web page. No local toolchain, no server.

**Runtime-agnostic core**: Pure TypeScript, **DOM-free**, no Web Worker / `worker_threads` / fetch / fs assumptions. This package is consumed by the `@gameguild/emception-*` adapters. Generated artifacts are owned by `@gameguild/emception-toolchain`.

## Which package do I need?

Most consumers do **not** build from source. Pick a published package:

| I want to…                                      | Install                                                                  | Entry point                               |
| ----------------------------------------------- | ------------------------------------------------------------------------ | ----------------------------------------- |
| Drop a "compile + run" widget into static HTML  | `@gameguild/emception-webcomponent` + `@gameguild/emception-browser`     | `<emception-run>` custom element          |
| Embed in a React 19 app (Next.js, Vite, CRA)    | `@gameguild/emception-react` + `@gameguild/emception-webcomponent` + `@gameguild/emception-browser` | `<EmceptionRun>` + `useEmception()`       |
| Add a real terminal UI                          | `@gameguild/emception-xterm` + `@xterm/xterm`                            | `fromXterm()` / `toXterm()` adapters      |
| Build a full IDE shell (editor + tabs + canvas) | `@gameguild/emception-ide`                                               | `<Ide>` React component, `<emception-ide>` custom element |
| Implement a custom runtime adapter              | `emception` + any adapter (browser, Node, Electron)                      | `RuntimeAdapter` interface, presets, UI config |

Toolchain payload:

- **`@gameguild/emception-toolchain/cdn/*`** — canonical manifest, Brotli bundles, generated glue, and matching WASM. The `emception/cdn/*` export is retained only as a compatibility copy of the same release.

Optional peer:

- **`@xterm/xterm`** — needed if you mount a terminal UI.

## Quick start

### Headless API (no UI)

```ts
import { createEmception } from '@gameguild/emception-browser';

const em = await createEmception({
  manifestUrl: '/cdn/manifest.json',
  tty: 'none'
});

await em.writeFile('/home/user/main.c', `
  #include <stdio.h>
  int main(){ puts("hi"); return 0; }
`);

const compile = await em.run('clang', ['/home/user/main.c', '-o', '/home/user/a.out']);
if (compile.exitCode !== 0) console.error(compile.stderr);

const run = await em.run('/home/user/a.out', []);
console.log(run.stdout);

em.dispose();
```

### Self-host the toolchain payload

Copy the published `cdn/` directory from `@gameguild/emception-toolchain` into your app's public `/cdn/` directory and point the Browser runtime at `/cdn/manifest.json`.

### Drop-in IDE (React)

```tsx
import { Ide } from '@gameguild/emception-ide';

<Ide
  manifestUrl="/cdn/manifest.json"
  workspaceName="lesson-3"
  workspaceConfig={{ files: [{ path: '/home/user/main.c', content: source }] }}
  enableCanvas // SDL3 / raylib graphics
/>;
```

### Read-only tutorial widget

```tsx
<Ide
  title="Lesson 3 — Pointers"
  manifestUrl="/cdn/manifest.json"
  workspaceUrl="/lessons/3/workspace.json"
  enableFileExplorer={false}
  enableCanvas={false}
  showSolutionFiles={false}
  readOnly
/>
```

## Technology stack

| Layer             | Tech                                              |
| ----------------- | ------------------------------------------------- |
| Compiler backend  | LLVM + Clang (compiled to WASM)                   |
| Optimiser         | Binaryen (compiled to WASM)                       |
| Emscripten driver | CPython (compiled to WASM) running `emcc.py`      |
| Build systems     | CMake + Ninja (compiled to WASM)                  |
| Graphics          | SDL3, raylib, Dear ImGui (WebGL2 + GLFW3)         |
| Kernel            | TypeScript (process manager, VFS, IPC, scheduler) |
| Terminal          | xterm.js                                          |

Each tool runs as an **isolated WASM process** with its own 2 GB linear memory. The TypeScript kernel mediates filesystem and IPC. Cross-browser async I/O works via Asyncify (no JSPI required).

## Core API Reference

### What's in here

| Subsystem                | Module path                                                             | Surface                                                                                                                                                                                     |
| ------------------------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Public types             | `emception`                                                             | `EmceptionAPI`, `EmceptionEventMap`, `ToolResult`, `RunOptions`, `CompileOptions`, `WorkspaceHandle`, …                                                                                     |
| Errors                   | `emception`                                                             | `EmceptionError`, `TimeoutError`, `WorkspaceConflictError`, `TestFailureError`, `BuildConfigError`, `RuntimeFeatureUnavailableError`, `CrossOriginIsolationError`, `CanvasUnavailableError` |
| Runtime adapter contract | `runtime/adapter`                                                       | `RuntimeAdapter` interface (browser + node implement it)                                                                                                                                    |
| Runtime helpers          | `runtime/cancellation`, `runtime/tool-result`, `runtime/feature-guards` | `withCancellation`, `withTimeoutOrThrow`, `assertToolResult`, `isToolResult`, `assertCanvasUnsupported`, `assertXtermStdinUnsupported`, `assertNoBrowserOnlyFeatures`                       |
| Tools registry           | tool registry                                                           | `TOOL_REGISTRY`, `ToolName`, `Tools`, `createTools(adapter)`                                                                                                                                |
| Build presets            | `build-presets`                                                         | `BUILD_PRESETS`, `BuildPresetName`, `BuildPreset`; compiler/linker preset definitions                                                                                                       |
| VFS                      | `vfs/overlay`, `vfs/manifest`                                           | `OverlayFS`, `IFileSystem`, `FSManifest`, `ManifestBundle`                                                                                                                                  |
| Workspace                | `workspace/{manager,seed,build-resolver,compile-argv,zip,transfer}`     | `WorkspaceManager`, `WorkspaceHandle`, `hashSeed`, `resolveBuild`, `buildArgv`, `createZip`/`readZip`, `exportWorkspace`/`importWorkspace`                                                  |
| In-memory store          | `workspace/store-memory`                                                | `MemoryWorkspaceManager` (full `WorkspaceManager` impl)                                                                                                                                     |
| Test engine              | `testing/engine`, `testing/clang-query`, `testing/doctest`              | `runTests`, matcher engine, doctest console parser                                                                                                                                          |
| TTY                      | `tty/headless`, `tty/line-buffer`, `tty/io-provider`                    | `HeadlessIOProvider`, `LineBuffer`, `IOProvider`                                                                                                                                            |
| UI helpers (DOM-free)    | `ui/adapters`, `ui/config`                                              | `kebabToCamel`/`camelToKebab`, `parseAttributesToInput`, `EVENT_DOM_NAMES`, `ATTRIBUTE_SCHEMA`, `normalizeViewConfig`, `toAttributes`, `diffViewConfigs`                                    |

## Constraints

- **No DOM**. Compiles under `lib: ['esnext']`. Uses platform-neutral types (`unknown[]` instead of `Transferable[]`, duck-typed feature guards, etc.). Don't import `lib.dom` types in this package.
- **Pure ESM**. Relative imports use `.js` extensions so emitted output runs under raw Node.
- **No side effects**. `sideEffects: false`. Importing a symbol only pulls in what tree-shaking can't elide.

## Adapter pattern

Every adapter implements `RuntimeAdapter`:

```ts
export interface RuntimeAdapter {
  readonly id: string;
  spawnWorker(opts): WorkerLike;
  loadManifest(opts): Promise<FSManifest>;
  openWorkspaceStore(opts): Promise<WorkspaceManager>;
  transferable(value): Transferable[];
  hasSharedArrayBuffer(): boolean;
}
```

`@gameguild/emception-browser` provides the implementation. `emception` (core) itself never instantiates one; it defines the contract and ships pure helpers that adapters compose.

## Events

`EmceptionAPI.on(name, fn)` returns an unsubscribe function. The full event map:

| name                | payload                                 |
| ------------------- | --------------------------------------- |
| `ready`             | `{}`                                    |
| `bundle-loaded`     | `{ name, sizeBytes }`                   |
| `progress`          | `{ phase, current?, total?, message? }` |
| `stdout` / `stderr` | `{ chunk: string \| Uint8Array }`       |
| `exit`              | `{ code: number, signal?: string }`     |
| `test-report`       | `TestReport`                            |
| `test-case`         | one item from a `TestReport`            |
| `error`             | `{ error: EmceptionError }`             |

`EVENT_DOM_NAMES` maps each name → `'emception-<name>'` for the webcomponent / React adapters.

## Errors

```ts
import { EmceptionError, TimeoutError } from 'emception';

try {
  await api.run('clang', ['-c', 'main.c']);
} catch (err) {
  if (err instanceof TimeoutError) {
    /* … */
  }
  if (err instanceof EmceptionError) {
    /* base + .cause */
  }
}
```

Adapters **do not** throw for tool failures — non-zero exit, crash, and timeout still resolve to a `ToolResult`. Throws are reserved for adapter failures (worker died, COI not enabled, …). Use `assertToolResult(value)` to defensively narrow values that come from arbitrary adapters.

## Tests

```bash
npm test --workspace=emception
```

The zero-dependency `node:test` suite covers the build resolver, seed hashing, in-memory workspace, compile argv, cancellation, clang-query matcher, doctest parser, test-engine handlers, ToolResult contract, attribute parsing, view-config validation, ZIP transfer, runtime feature guards, and event-map shape.
