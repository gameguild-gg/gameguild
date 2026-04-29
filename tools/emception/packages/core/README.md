# @emception/core

Runtime-agnostic core for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Pure TypeScript, **DOM-free**, no Web Worker / `worker_threads` / fetch / fs assumptions. Consumed by every other `@emception/*` package.

## What's in here

| Subsystem                | Module path                                                             | Surface                                                                                                                                                                                     |
| ------------------------ | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Public types             | `@emception/core`                                                       | `EmceptionAPI`, `EmceptionEventMap`, `ToolResult`, `RunOptions`, `CompileOptions`, `WorkspaceHandle`, …                                                                                     |
| Errors                   | `@emception/core`                                                       | `EmceptionError`, `TimeoutError`, `WorkspaceConflictError`, `TestFailureError`, `BuildConfigError`, `RuntimeFeatureUnavailableError`, `CrossOriginIsolationError`, `CanvasUnavailableError` |
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

`@emception/browser` provides the implementation. `@emception/core` itself never instantiates one; it just defines the contract and ships pure helpers that adapters compose.

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
import { EmceptionError, TimeoutError } from '@emception/core';

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
npm test --workspace=@emception/core
```

159 zero-dep `node:test` cases covering the build resolver, seed hashing, in-memory workspace, compile-argv, cancellation, clang-query matcher, doctest parser, test-engine handlers, ToolResult contract, attribute parsing, view-config validator, ZIP writer/parser, workspace transfer, runtime feature guards, and event-map shape.
