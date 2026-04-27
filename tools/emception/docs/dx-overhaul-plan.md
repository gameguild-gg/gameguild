# Plan: Emception DX overhaul — configurable embedder API

Scope: public API + DX. Presets = config. Dual host (jsDelivr default + self-host). Embed: web component + React + Node + IDE. Monorepo `@emception/*` + unscoped meta `emception`. Adds: I/O streams, canvas bind, multi-file workspaces, named IDB workspaces w/ seed semantics, file visibility, per-workspace build config, declarative test runner, headless Node runtime, **reactive IDE component w/ React + raw-HTML custom-element wrappers** (existing `packages/emception`).

## Design pillars

1. **One core, many skins.** `EmceptionCore` = programmatic. UI (`<emception-run>`, `<EmceptionRun>`, `<Ide>`) = views. Props/attrs = 1:1 mirror of core opts.
2. **Streams over callbacks.** stdin/stdout/stderr = WHATWG streams. Shorthands (string, fn, xterm) normalize to streams.
3. **Workspaces first-class.** Named, mounted at `/workspace/<name>`. No more flat `/home/user`.
4. **Visibility = metadata.** `public | hidden | solution` tags in workspace manifest. UI filters; runtime sees all.
5. **Build config travels w/ workspace.** Compiler/std/cflags/ldflags/defines/includes/libs/sources/output/env persist next to files. Call-site overrides layer on top.
6. **Tests = declarative plans.** `runTests(plan)` → structured report. Pluggable kinds.
7. **Runtime-agnostic core.** Same `EmceptionCore` runs browser (Web Worker + IDB) and Node (worker_threads + fs). Platform bits behind `RuntimeAdapter`.
8. **IDE = reactive composition of optional panels.** Canvas, terminal, workspace, docking, tabs all toggleable. File visibility honored. Single-file mode supported.

---

## Critique of current state

### Foundation DX gaps

1. No zero-config path. `createEmception` needs `manifestUrl` + `container`. No "just works" after `npm install`. Runno = one tag.
2. Terminal coupled to API. `container` mandatory; headless invents hidden div.
3. CDN distribution = manual chore. No `postinstall`. README assumes `/cdn/manifest.json` served.
4. No public CDN URL. Non-self-hosters stuck.
5. No web component. Plain HTML = hand-write boot + xterm + FS.
6. No framework adapter exposed. `@gameguild/emception-ui` private + undocumented.
7. No preset notion. Every consumer pays for clang+lld+python+cmake+ninja+sdl3+imgui.
8. Tools stringly-typed. `run('clang', […])` no autocomplete.
9. COOP/COEP implicit. `coi-serviceworker.js` shipped but not exported, not in quick-start. SAB errors cryptic.
10. No download progress / lifecycle events. Big bundles, no `onProgress`.
11. Errors opaque. Fetch retries 3× → string throw. No typed classes.
12. README build-pipeline-first. ~150 lines of toolchain build before "how to embed".
13. No live playground. Demos require monorepo clone.
14. Versioning unclear. `3.0.1` vs manifest's own LLVM/Python versions. No pin guarantee.
15. Worker URL fragility. Each bundler differs. No recipe.
16. `@xterm/xterm` peer dep awkward. Optional but main example uses it.
17. No SSR guard. Importing breaks Next.js server components.
18. Naming inconsistent. `emception` unscoped vs `@gameguild/*` demos/UI.
19. No examples in tarball. Just `dist/` + README.
20. No CLI diagnostics. `npx emception doctor` would save hours.

### Configurable-embed gaps

21. stdin not first-class. `RunOptions` no typed stdin. stdout = single string blob.
22. Canvas/GL no host binding. SDL3 in sysroot but no API to render into _this_ `<canvas>`.
23. No workspace notion. Single flat `/home/user`. Two widgets = IDB collision.
24. No idempotent seeding. No `seed: {}` w/ content hash guard.
25. No file visibility. Can't ship `tests/hidden/grader.cpp` invisible in tree.
26. No test runner. Every LMS reinvents w/ different bugs.
27. stdout/stderr interleaving lossy. Merged via xterm; can't separate reliably.
28. No structured exit info. Missing `signal`, `durationMs`, `peakMemory`, `timedOut`.
29. No timeouts/abort. Infinite loop kills whole worker.
30. clang-query / doctest latent. In sysroot but no API.
31. Build flags scattered. `compileAndRun({ flags })` vs `TestPlan.build.flags` vs preset. Same workspace builds differently per entry point.
32. Browser-only. Assumes `window`/`Worker`/`IndexedDB`/`fetch`. CI graders, backend services blocked. No `emception/node`, no `worker_threads`, no fs workspace store.

### IDE gaps (existing `packages/emception`)

33. **IDE not reactive.** Ide.tsx hardcodes file explorer + tabs + terminal + canvas. No way for embedder to disable any panel.
34. **No single-file mode.** Always shows tree + tabs even when irrelevant.
35. **No fullscreen toggle.** Embedder can't promote IDE to fullscreen reactively.
36. **Workspace storage hardcoded.** `WORKSPACE_STORAGE_KEY = 'gameguild.emception.workspace.v1'` — single global key, no named workspaces.
37. **File visibility ignored.** `WorkspaceFile` has no `visibility` field; explorer shows everything.
38. **Tight coupling to `bootInWorker`.** Ide.tsx imports `from 'emception'` directly; can't inject a pre-built core or swap runtime.
39. **Terminal always present.** `TerminalPanel` mounted unconditionally; no headless display mode.
40. **Docking always on.** `DockGroup`/`DockDropOverlay` always render; no way to lock layout.
41. **Tab nav always on.** Even single-file workspaces show tab strip.
42. **Canvas slot hardcoded path.** `SDL_CANVAS_PATH = '/user/sdl-canvas'` — no opt-out, no rebind.
43. **Wrong package name.** Lives at `packages/emception` as `@gameguild/emception-ui` — collides w/ proposed `@emception/ide`.

---

## Target DX

### Headless (graders)

```ts
import { createEmception, files } from 'emception';

const em = await createEmception({
  preset: 'cpp',
  workspace: {
    name: 'assignment-42',
    seed: files({
      'main.cpp': { content: starter, visibility: 'public' },
      'tests.hpp': { content: hidden, visibility: 'hidden' },
      'solution.cpp': { content: solution, visibility: 'solution' },
    }),
    seedPolicy: 'once',
    build: {
      std: 'c++20',
      cflags: ['-O1', '-Wall', '-Wextra', '-Werror'],
      defines: { STUDENT_MODE: true },
      includePaths: ['include'],
      sources: ['main.cpp', 'solution.cpp'],
      output: 'a.out',
    },
  },
});

const result = await em.run('./a.out', [], { stdin: 'hello\n5\n', stdout: 'capture', stderr: 'capture', timeoutMs: 5000, signal: ac.signal });

const report = await em.runTests({
  build: { sources: ['main.cpp', 'tests/grader.cpp'], output: 'grader.out' },
  cases: [
    { kind: 'stdio', stdin: '1 2\n', expectedStdout: '3\n' },
    { kind: 'stdio-file', inFile: 'tests/in/01.txt', expectedOutFile: 'tests/out/01.txt' },
    { kind: 'clang-query', matcher: 'hasRecordDecl(hasName("LinkedList"))', expect: 'found' },
    { kind: 'doctest', sourceFiles: ['tests/doctest_main.cpp'] },
  ],
});
```

### Node (CI graders / backend)

```ts
import { createEmception, files } from '@emception/node';

const em = await createEmception({
  preset: 'cpp',
  manifestPath: require.resolve('@emception/sysroot/manifest.json'),
  workspaceStore: { kind: 'fs', path: '/var/lib/emception' },
  workspace: {
    name: `submission-${submissionId}`,
    seed: files({ 'main.cpp': { content: studentCode, visibility: 'public' }, 'grader.cpp': { content: graderCode, visibility: 'hidden' } }),
    seedPolicy: 'overwrite',
    build: { std: 'c++20', cflags: ['-O1', '-Wall', '-Werror'], sources: ['main.cpp', 'grader.cpp'] },
  },
});

const report = await em.runTests({
  cases: [
    { kind: 'stdio-file', inFile: 'tests/in/01.txt', expectedOutFile: 'tests/out/01.txt' },
    { kind: 'doctest', sourceFiles: ['tests/doctest_main.cpp'] },
  ],
  timeoutMsPerCase: 5000,
  redactHidden: true,
});

await em.dispose();
process.exit(report.failed === 0 ? 0 : 1);
```

**Diff vs browser:** `manifestPath` not `manifestUrl`; `workspaceStore` selects backend (`fs` default, `memory` opt); no `container`/xterm — pass `process.std*` directly; `canvas` throws `RuntimeFeatureUnavailableError`; uses `node:worker_threads`.

### UI / HTML

```html
<emception-run
  preset="cpp"
  workspace="assignment-42"
  seed-url="/assignments/42/seed.json"
  seed-policy="once"
  build-url="/assignments/42/build.json"
  stdin="auto"
  stdout="auto"
  canvas
  show-hidden="false"
  autorun
></emception-run>
```

```tsx
<EmceptionRun
  preset="sdl"
  workspace={{ name: 'game-jam-a', seed, seedPolicy: 'once', build: { std: 'c++20', cflags: ['-O2'], libs: ['SDL3'] } }}
  stdin={xterm}
  stdout={xterm}
  canvas={canvasRef}
  onExit={(r) => …}
  onTestReport={(r) => …}
  hideHiddenFiles
/>
```

HTML attrs = kebab mirror; React props = camelCase mirror. No behavioral divergence.

### IDE (reactive composition)

```tsx
import { Ide } from '@emception/ide';

<Ide
  preset="cpp"
  // workspace toggles
  workspace={{ name: 'lesson-01', seed, seedPolicy: 'once', build }}
  enableWorkspace                          // false → single-file or tabs-only
  enableTabs                               // false → no tab strip (single-file mode)
  enableDocking                            // false → fixed layout, no drag-rearrange
  // panel toggles
  enableFileExplorer={hasWorkspace}
  enableTerminal                           // false → headless run, output via onStdout
  enableCanvas                             // false → no SDL slot
  // size / expand (separate concerns)
  resizable                                // user drags bottom edge to grow IDE vertically (in-page)
  expandable                               // adds toolbar button that toggles `expanded`
  expanded={isFs}                          // reactive; true = fill webpage area (not OS fullscreen)
  onExpandedChange={setIsFs}
  // theming + read-only
  theme="dark"                             // 'dark' | 'light' | custom token map
  readOnly={false}
  // visibility filter (driven by workspace metadata)
  showHiddenFiles={false}                  // 'hidden' files invisible in tree
  showSolutionFiles={false}                // 'solution' files invisible
  // I/O
  stdin="auto" stdout="auto"
  onReady={(em) => …}
  onExit={(r) => …}
/>
```

Single-file usage: `<Ide enableWorkspace={false} enableTabs={false} enableFileExplorer={false} enableDocking={false} files={{ 'main.c': src }} />`.

Tabs-only (no tree): `<Ide enableFileExplorer={false} enableDocking={false} />`.

Headless preview (canvas only): `<Ide enableTerminal={false} enableTabs={false} enableFileExplorer={false} canvas onReady={…} />`.

All toggles **reactive** — flipping `enableTerminal` at runtime mounts/unmounts the panel; `fullscreen` toggle re-parents the IDE root.

---

## Core API shapes

```ts
// Files & workspaces
interface FileEntry {
  content: string | Uint8Array;
  visibility?: 'public' | 'hidden' | 'solution';
  readonly?: boolean;
  executable?: boolean;
}
type WorkspaceSeed = Record<string, FileEntry | string>;

// Build config — persisted in .emception/build.json
interface WorkspaceBuildConfig {
  compiler?: 'clang' | 'clang++' | 'emcc' | 'em++';
  std?: string;
  cflags?: string[];
  cxxflags?: string[];
  ldflags?: string[];
  defines?: Record<string, string | true>;
  includePaths?: string[];
  libPaths?: string[];
  libs?: string[];
  sources?: string[];
  output?: string;
  env?: Record<string, string>;
  cmake?: {
    sourceDir?: string;
    buildDir?: string;
    configureArgs?: string[];
    buildArgs?: string[];
    /** Multi-binary CMake projects: list of target names to build. Resolver invokes
     *  `cmake --build <buildDir> --target <name>` per entry with shared flags.
     *  Per-target customization belongs in CMakeLists.txt, not here. */
    targets?: string[];
  };
}

interface WorkspaceOptions {
  name: string;
  seed?: WorkspaceSeed;
  seedPolicy?: 'once' | 'overwrite' | 'merge';
  mountPath?: string;
  build?: WorkspaceBuildConfig;
}

// I/O
type StdinInput =
  | string
  | Uint8Array
  | AsyncIterable<string | Uint8Array>
  | ReadableStream<Uint8Array>
  | (() => string | Uint8Array | null | Promise<string | Uint8Array | null>)
  | { xterm: Terminal; raw?: boolean }
  | 'none';

type StdoutSink = 'capture' | WritableStream<Uint8Array> | ((chunk: Uint8Array) => void | Promise<void>) | { xterm: Terminal } | 'none';

interface RunOptions {
  cwd?: string;
  env?: Record<string, string>;
  stdin?: StdinInput;
  stdout?: StdoutSink;
  stderr?: StdoutSink;
  timeoutMs?: number;
  signal?: AbortSignal;
  canvas?: HTMLCanvasElement | OffscreenCanvas;
  workspace?: string;
}

interface CompileOptions extends RunOptions {
  sources?: string[];
  build?: Partial<WorkspaceBuildConfig>;
  flags?: string[]; // legacy — appended to cflags
}

interface ToolResult {
  exitCode: number;
  stdout: string;
  stderr: string;
  durationMs: number;
  timedOut: boolean;
  signal?: string;
}

// Tests
type TestCase =
  | { kind: 'stdio'; stdin?: string; expectedStdout: string | RegExp; expectedStderr?: string | RegExp; expectedExit?: number; name?: string }
  | { kind: 'stdio-file'; inFile: string; expectedOutFile: string; name?: string }
  | { kind: 'clang-query'; matcher: string; expect: 'found' | 'not-found' | { minCount: number } }
  | { kind: 'doctest'; sourceFiles: string[] }
  | { kind: 'custom'; run: (em: EmceptionAPI) => Promise<TestCaseResult> };

interface TestPlan {
  build?: Partial<WorkspaceBuildConfig> & { sources?: string[]; output?: string };
  cases: TestCase[];
  timeoutMsPerCase?: number;
  redactHidden?: boolean;
}

interface TestCaseResult {
  name: string;
  passed: boolean;
  durationMs: number;
  diagnostic?: string;
}
interface TestReport {
  passed: number;
  failed: number;
  totalDurationMs: number;
  cases: TestCaseResult[];
}

// Core
interface EmceptionAPI {
  workspace: {
    list(): Promise<string[]>;
    switch(name: string): Promise<void>;
    reset(name?: string): Promise<void>;
    readFile(path: string): Promise<Uint8Array | null>;
    writeFile(path: string, data: Uint8Array | string, meta?: Partial<FileEntry>): Promise<void>;
    listFiles(opts?: { includeHidden?: boolean; includeSolution?: boolean }): Promise<Array<{ path: string } & FileEntry>>;
    setVisibility(path: string, v: FileEntry['visibility']): Promise<void>;
    getBuild(): Promise<WorkspaceBuildConfig>;
    setBuild(build: WorkspaceBuildConfig): Promise<void>;
    exportZip(): Promise<Blob>;
    importZip(blob: Blob): Promise<void>;
  };
  run(cmd: string, argv?: string[], opts?: RunOptions): Promise<ToolResult>;
  compileAndRun(sourceOrFiles?: string | string[], opts?: CompileOptions): Promise<ToolResult>;
  runTests(plan: TestPlan, opts?: { signal?: AbortSignal }): Promise<TestReport>;
  on(event: 'progress' | 'ready' | 'bundle-loaded' | 'stdout' | 'stderr' | 'exit' | 'test-case', fn: (ev: unknown) => void): () => void;
  dispose(): void;
}
```

### IDE component shape

```ts
interface IdeProps {
  // core wiring
  preset?: PresetName;
  manifestUrl?: string;
  workspace?: WorkspaceOptions;
  /** Inline file map, used when enableWorkspace=false. */
  files?: Record<string, string | FileEntry>;
  /** Pre-built core injection — bypass internal createEmception. */
  api?: EmceptionAPI;

  // panel toggles (all reactive)
  enableWorkspace?: boolean; // default true; false = inline files only
  enableFileExplorer?: boolean; // default = enableWorkspace
  enableTabs?: boolean; // default true; false = single-file
  enableTerminal?: boolean; // default true
  enableCanvas?: boolean; // default true
  enableDocking?: boolean; // default true; false = fixed layout

  // display
  fullscreen?: boolean;
  onFullscreenChange?: (v: boolean) => void;

  // visibility filter
  showHiddenFiles?: boolean; // default false
  showSolutionFiles?: boolean; // default false

  // I/O passthrough → core
  stdin?: StdinInput | 'auto';
  stdout?: StdoutSink | 'auto';
  stderr?: StdoutSink | 'auto';

  // events
  onReady?: (em: EmceptionAPI) => void;
  onExit?: (r: ToolResult) => void;
  onTestReport?: (r: TestReport) => void;

  title?: string;
}
```

### Build config resolution

Precedence low→high:

1. Preset defaults (`cpp` → `clang++ -std=c++20`).
2. Workspace `build` (persisted `.emception/build.json`).
3. Call-site overrides (`compileAndRun({ build, flags, sources })`, `runTests({ build })`).

Multi-binary projects: use CMake workspace (presets live as files inside the workspace).

Merge: arrays concat + dedup; scalars overwrite; objects (`defines`, `env`, `cmake`) merge by key, later wins.

---

## Phased plan

### Phase 0 — Repo restructure (multi-package monorepo)

**Goal:** split single `emception` into focused packages so heavy sysroot, browser-only DOM, Node-only, framework glue install/release independently. Unscoped `emception` stays as batteries-included meta; rest under `@emception/*`.

**Package map** (9 packages — canvas folded into `@emception/browser`)

| Package                   | Tier    | Purpose                                                                                                        | Runtime  | Heavy?       |
| ------------------------- | ------- | -------------------------------------------------------------------------------------------------------------- | -------- | ------------ |
| `@emception/core`         | Core    | Types, `EmceptionCore`, VFS, tool registry, build resolver, test engine, presets, `RuntimeAdapter`.            | Any      | No           |
| `@emception/sysroot`      | Assets  | `.tar.br` + `manifest.json` + `coi-serviceworker.js`. JS-free. Versioned by toolchain.                         | Any      | **~100s MB** |
| `@emception/browser`      | Adapter | Web Worker spawn, IDB workspace store, fetch manifest, **OffscreenCanvas + SDL/ImGui helpers**, COI preflight. | Browser  | No           |
| `@emception/node`         | Adapter | `worker_threads` spawn, fs workspace store, disk manifest, Node↔WHATWG bridges.                                | Node 20+ | No           |
| `@emception/xterm`        | I/O     | xterm.js stdin/stdout/stderr bridge. xterm peer dep lives **here only**.                                       | Browser  | No           |
| `@emception/react`        | UI      | `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception`. React 19 peer.                   | Browser  | No           |
| `@emception/webcomponent` | UI      | `<emception-run>` custom element. No framework deps.                                                           | Browser  | No           |
| `@emception/ide`          | UI      | Reactive `<Ide>` React 19 + `<emception-ide>` custom-element wrapper for raw HTML/Next.                        | Browser  | Med (Monaco) |
| `@emception/cli`          | Tool    | `doctor`, `cdn-export`, `run`, `test`. Ships `bin`.                                                            | Node     | No           |
| `emception` (unscoped)    | Meta    | Re-exports `@emception/browser` + `@emception/xterm`. Keeps `npm i emception` working.                         | Browser  | No           |

**Dep graph**

```
                  ┌─ @emception/ide ────────────┐
                  ├─ @emception/react ──────────┤
                  ├─ @emception/webcomponent ───┤
@emception/xterm ─┴──► @emception/browser ──────┤
                                                 ├─► @emception/core ─► (no deps)
                   @emception/node ──────────────┤
                                                 │
                          @emception/cli ────────┘

@emception/sysroot   (peer of @emception/browser + @emception/node, JS-free)
emception (meta)     ──► @emception/browser + @emception/xterm
```

**Steps**

0.1. Workspace bootstrap. Convert `tools/emception/` → npm/pnpm workspace root. Create `packages/{core,sysroot,browser,node,xterm,react,webcomponent,ide,cli}/`. Top-level `package.json` `"workspaces": ["packages/*"]`. Shared `tsconfig.base.json`.

0.2. Source migration:

- Runtime-agnostic core (`createEmception` refactor, `tool-runner`, `worker-protocol`, VFS, presets, errors, testing/\*, workspace/{manager,seed,zip,build-resolver}, io/streams) → `@emception/core`.
- `worker-client`, `bootInWorker`, `store-idb`, COI helper, **OffscreenCanvas + SDL/ImGui canvas helpers** → `@emception/browser`.
- New `runtime/node`, `store-fs`, `node-streams`, `worker-entry` → `@emception/node`.
- `io/xterm-adapter` → `@emception/xterm`.
- `web-component/emception-run` → `@emception/webcomponent`.
- `react/index` + `ui/config` → `@emception/react`.
- **Existing `packages/emception/src/*` (Ide, FileExplorer, DockGroup, TerminalPanel, ResizeHandle, DockDropOverlay, ide-types, ide-utils, workspace-presets, styles)** → `packages/ide/src/*` as `@emception/ide`. Rename the package json from `@gameguild/emception-ui` → `@emception/ide`.
- `cli/{doctor,cdn-export}` → `@emception/cli`.
- `public/cdn/**` + `public/coi-serviceworker.js` → `@emception/sysroot/`.

  0.3. Meta-package preserve. `emception` = thin wrapper depending on `@emception/browser` + `@emception/xterm`. Source = `export * from '@emception/browser'; export * as xterm from '@emception/xterm';`.

  0.4. Scope reg. Reserve `@emception` npm scope. `.npmrc` + CI tokens. `"publishConfig": { "access": "public" }` on every scoped package.

  0.5. Release tooling. `changesets`. **v1 = lock-step versioning** (all `@emception/*` + meta bump together). Independent post-1.0. `@emception/sysroot` always independent by toolchain (`0.20.x` for LLVM 20.x).

  0.6. Sysroot decoupling. `@emception/sysroot` builds via existing 17-step pipeline; `package.json` version tracks toolchain. `@emception/browser` + `@emception/node` declare `"peerDependencies": { "@emception/sysroot": "^0.20.0" }`. Resolution: `require.resolve('@emception/sysroot/manifest.json')` (Node) or configurable URL (browser, default jsDelivr).

  **Distribution rule:** brotli-compressed `*.tar.br` + `manifest.json` + `coi-serviceworker.js` are emitted by the build pipeline directly into `packages/sysroot/` and shipped via `npm publish`. **They are NEVER committed to git** — the package tarball IS the distribution channel. jsDelivr mirrors the npm package automatically; no separate CDN upload step. Publishing piggybacks on the existing `.github/workflows/main.yml` npm deployment flow (no separate tag-driven workflow).

  0.7. `scripts/sync-emception-cdn.mjs` → thin shim copying from `@emception/sysroot/` for local dev only (never used in release).

  0.8. Build pipeline. Workspace script `build:all` runs topologically: sysroot → core → browser/node (parallel) → xterm → react/webcomponent/ide → cli → meta.

  0.9. Examples. Each UI/runtime package ships `examples/` in tarball.

**Out of scope Phase 0:** API changes. Pure mechanical move.

### Phase 1 — Foundation DX

1.1. Decouple terminal: `container?` optional; `tty: 'none' | 'xterm'` or derive from stdin/stdout shape. xterm path → `@emception/xterm`.
1.2. Zero-config manifest: default `https://cdn.jsdelivr.net/npm/@emception/sysroot@<version>/manifest.json`, injected by tsup. Browser falls back to `@emception/sysroot` in `node_modules` if found.
1.3. `@emception/cli cdn-export <dir>` for self-hosters; copies from `@emception/sysroot/`.
1.4. `@emception/browser/coi` subpath + runtime preflight throwing `CrossOriginIsolationError`.
1.5. Event API (`progress`, `ready`, `bundle-loaded`) in `@emception/core`.
1.6. Typed error hierarchy (`EmceptionError` + `TimeoutError`, `WorkspaceConflictError`, `TestFailureError`, `BuildConfigError`, `RuntimeFeatureUnavailableError`) in `@emception/core`.
1.7. SSR safety. `"sideEffects": false` per package; `@emception/core` zero DOM imports.
1.8. **`RuntimeAdapter` interface in `@emception/core`** (pulled forward from Phase 7 — Phase 0 prereq). `spawnWorker(entry, opts)`, `loadManifest(source)`, `openWorkspaceStore(opts)`, `transferable(value)`. Browser impl in `@emception/browser`; Node impl in `@emception/node`.

### Phase 2 — Streams, canvas, timeouts

2.1. **stdin/stdout/stderr plumbing.** Rework `ToolRunner`: each invocation gets 3 independent byte streams. Sinks accept all `StdinInput`/`StdoutSink` shapes; normalize internally to `(ReadableStream, WritableStream, WritableStream)`. xterm = adapter, not core.
2.2. **Timeout + AbortSignal.** `timeoutMs` + `signal` in `RunOptions`. On fire → terminate tool WASM instance. `ToolResult.timedOut`.
2.3. **Canvas binding.** Pass `HTMLCanvasElement` (or `OffscreenCanvas`) into worker via `postMessage(..., [canvas.transferControlToOffscreen()])`. Wire Emscripten `Module.canvas` only when `opts.canvas` present. Validate transfer-once. Expose `detachCanvas()`.
2.4. Extended `ToolResult`: add `durationMs`, `timedOut`, `signal`.

### Phase 3 — Named workspaces + visibility + build config

3.1. **Workspace manager** (`src/workspace/manager.ts`). IDB key `emception:ws:<name>`, mount `/workspace/<name>` (active = symlink `/workspace/current`). Per-workspace IDBFS instance, replace single `/home/user`.
3.2. **Seed policy.** `'once' | 'overwrite' | 'merge'`. Seed-hash marker file. `once` skip if match; `merge` only new keys; `overwrite` rewrites + updates marker. `WorkspaceConflictError` on `once` mismatch.
3.3. **File metadata sidecar.** `.emception/meta.json` per workspace: `{ path, visibility, readonly, executable }`. `listFiles()` reads it; `writeFile(..., meta)` updates. Runtime ignores visibility; UI respects.
3.4. **Build sidecar.** `.emception/build.json` carries `WorkspaceBuildConfig` (incl `targets`). Loaded on switch; `getBuild()`/`setBuild()`. Included in seed + zip export.
3.5. **Build resolver** (`src/workspace/build-resolver.ts`). Precedence rules above → flat `ResolvedBuild` consumed by `compileAndRun` + test engine. Throws `BuildConfigError` on contradictions.
3.6. **IDB namespace.** Fixed DB name `'emception'` by default; optional `namespace` opt-in for callers needing per-origin isolation.
3.7. **Export/import.** ZIP including `.emception/{meta,build}.json` so instructors ship seed workspaces as static assets.

### Phase 4 — Presets (typed)

4.1. `Preset` defs: `c`, `cpp`, `python`, `sdl`, `cmake`, `full`. Each contributes `bundlesToPreload`, `defaultTools`, baseline `WorkspaceBuildConfig`.
4.2. `ToolName` union from `TOOL_REGISTRY`; `em.tools.clang(args)` typed wrappers.
4.3. Preset-aware: `compileAndRun(src, opts)` picks compiler per preset; `runScript(src)` for python.

### Phase 5 — Test runner

5.1. **Engine** (`src/testing/engine.ts`): iterate `plan.cases`, dispatch to kind-handler, aggregate `TestReport`. Emits `on('test-case', …)`. Resolves build via `build-resolver`.
5.2. **`stdio`**: build via resolved config; run binary w/ `stdin`; compare `stdout` (string eq or regex), optional `stderr`/`expectedExit`.
5.3. **`stdio-file`**: same but read `inFile`/`expectedOutFile` from workspace (hidden allowed).
5.4. **`clang-query`**: `clang -Xclang -ast-dump=json` over the resolved sources; TS matcher walks the AST tree. No clang-query WASM binary.
5.5. **`doctest`**: ship `doctest.h` under `/usr/include/doctest.h`. Compile student + doctest file w/ resolved build, parse machine-readable output.
5.6. **`custom`**: pass-through fn receives API → `TestCaseResult`.
5.7. **Visibility-aware reports.** Engine references hidden during build; UI never lists. Diagnostics referencing hidden paths redacted when `redactHidden === true`.

### Phase 6 — Embedding: web component + React

6.1. **`@emception/webcomponent`** publishes `<emception-run>`. Attrs = kebab mirror (incl `cflags`, `ldflags`, `std`, `output`, `build-url`). Slots: `<textarea slot="stdin">`, `<canvas slot="canvas">`. Events: `emception-{ready,stdout,stderr,exit,test-report,test-case}`. Deps: `@emception/browser` + `@emception/xterm`.
6.2. **`@emception/react`** publishes `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception(opts)`. Props = camelCase mirror. `workspace.build` accepted nested. React 19 peer. Deps: `@emception/browser` + `@emception/xterm`.
6.3. **Shared view-config layer** in `@emception/core` (`src/ui/config.ts`) — both webcomponent + react consume same validator. No drift.

### Phase 7 — Node runtime

7.1. `RuntimeAdapter` already in `@emception/core` (Phase 1.8). `@emception/browser` = browser impl; `@emception/node` here = Node impl.
7.2. **Worker-client extraction (BIG REFACTOR).** Move `packages/browser/src/worker-client.ts` (651 lines: orchestrator + message dispatch + lifecycle + tool-invocation pipeline) into `@emception/core` under `src/runtime/worker-orchestrator.ts`. Browser becomes a thin shim that supplies the Web-Worker `RuntimeAdapter`. **`@emception/node`** then wires `createEmception` against the same orchestrator + a `node:worker_threads` adapter. Worker entry = CJS+ESM dual at `dist/worker-entry.{cjs,mjs}`. **Validation: real WASM, real VFS, real workers — NO MOCKS.** Build the sysroot if CI lacks it. Both Node `worker_threads` and browser Web-Worker paths must run the same `compileAndRun(helloWorld)` smoke against the resolved orchestrator before merge.
7.3. **`store-fs`** (`packages/node/src/workspace/store-fs.ts`) mirrors IDB store API. Workspace at `<root>/<name>/` w/ `.emception/{meta,build}.json` on disk. Atomic writes via temp+rename. File-locking via `proper-lockfile` (optional dep).
7.4. **`store-memory`** in `@emception/core` (runtime-agnostic — useful in browser tests too). `seedPolicy: 'overwrite'` implicit default.
7.5. **Node stdio adapters** (`io/node-streams.ts`): `Readable.toWeb()`/`Writable.toWeb()` (Node 18+). `process.std*` accepted directly.
7.6. **Manifest from disk.** `loadManifest({ path })` reads via `node:fs/promises`; `loadManifest({ url })` falls back to global `fetch`. Default: `require.resolve('@emception/sysroot/manifest.json')`.
7.7. **No-canvas/no-xterm guards.** `RunOptions.canvas` + `StdinInput.{ xterm }` throw `RuntimeFeatureUnavailableError` w/ message pointing at `@emception/browser`.
7.8. **CLI parity.** `@emception/cli doctor` detects Node mode → checks `worker_threads`, `fetch`, `@emception/sysroot` resolvable, write perm on workspace store.
7.9. CI examples: `packages/node/examples/{grader,github-action}`.

### Phase 8 — IDE reactivity (`@emception/ide`) — BIG-BANG REWRITE

**Rollout:** single PR. Full rewrite + all panels + all toggles + custom-element wrapper + legacy migration shim land together. No incremental ship. Plan task list is the merge checklist.

**Goal:** rewrite existing `Ide.tsx` (currently in `packages/emception`) to a fully composable reactive component obeying all toggles in `IdeProps`.

8.1. **Rename + relocate.** `packages/emception` → `packages/ide` as `@emception/ide`. Update internal imports (`from 'emception'` → `from '@emception/browser'`).
8.2. **Decouple from `bootInWorker`.** Accept either `api?: EmceptionAPI` (pre-built) or `{preset, manifestUrl, workspace}` (auto-built). Internal-build path uses `@emception/browser`'s `createEmception`.
8.3. **Panel composition.** Refactor `Ide.tsx` into:

- `<IdeRoot>` — owns layout shell + fullscreen.
- `<FileExplorerPanel>` — mounted iff `enableFileExplorer`.
- `<TabsPanel>` — mounted iff `enableTabs`; when off, shows just the active file content w/o tab strip.
- `<TerminalPanel>` — mounted iff `enableTerminal`. When off, stdin/stdout route to props (`onStdout` / `stdin` callback).
- `<CanvasPanel>` — mounted iff `enableCanvas`. Otherwise canvas slot omitted; SDL runs throw `CanvasUnavailableError` unless caller passes `canvas` prop directly.
- `<DockHost>` — wraps panels; iff `enableDocking`, uses `DockGroup`/`DockDropOverlay`. Otherwise renders fixed `PanelGroup` w/ no drag handles.
  8.4. **Reactive toggles.** All `enable*` flags react at runtime — flipping unmounts/mounts the corresponding panel, preserves underlying `EmceptionAPI` state. Workspace files survive panel changes.
  8.5. **Fullscreen.** `fullscreen` prop reactive; when true, IDE root re-parents to `document.body` via portal w/ `position: fixed; inset: 0; z-index: <high>`. `onFullscreenChange` notifies parent. Optional internal toolbar button toggles + calls `onFullscreenChange(!fullscreen)`.
  8.6. **Workspace toggle.** `enableWorkspace=false` → bypass `WorkspaceManager`; use `files` prop as ephemeral memory-only file map. No IDB persistence. File explorer (if enabled) shows the inline list.
  8.7. **Tab toggle.** `enableTabs=false` → render single active file editor; no tab strip; no multi-file UI. Combined w/ `enableWorkspace=false` + single `files` entry → minimal single-file editor mode.
  8.8. **Docking toggle.** `enableDocking=false` → fixed layout (editor center, terminal bottom or right depending on `terminalPosition` prop, canvas right or hidden). No drag-rearrange. `DockGroup`/`DockDropOverlay` unmounted.
  8.9. **Visibility filter.** `FileExplorer` queries `workspace.listFiles({ includeHidden: showHiddenFiles, includeSolution: showSolutionFiles })`. `WorkspaceFile` extended w/ `visibility` field; tree-build skips entries based on flags. Reactive — flipping flag refreshes tree.
  8.10. **Storage key.** Replace fixed `WORKSPACE_STORAGE_KEY` w/ `emception:ws:<workspace.name>` derived from props. Multiple IDE instances on same page coexist.
  8.11. **Canvas slot.** `SDL_CANVAS_PATH` configurable via prop `canvasPath` (default `/user/sdl-canvas`). `enableCanvas=false` → no slot, no canvas tab.
  8.12. **Tests.** Component tests per panel toggle; snapshot of single-file mode + tabs-only mode + headless preview mode. Verify reactive flip mounts/unmounts w/o losing core state.

### Phase 9 — Docs, diagnostics, distribution ✅ DONE

9.1. ✅ README rewrite per package; consumer-first. Decision matrix at top of `tools/emception/README.md`: "which package do I need?" Cookbook in meta-package: "grade assignment (browser)", "grade assignment (Node CI)", "SDL canvas demo", "multi-file project w/ build config", "automated test plan", "named build targets", **"reactive IDE in tutorial site"**, **"single-file Monaco editor"**.
9.2. ✅ `npx @emception/cli doctor` (also `npx emception doctor` via meta-package `bin`) — auto-detects browser/Node; checks SAB, COOP/COEP, manifest reach, worker support, or worker_threads + fetch + fs perms.
9.3. ✅ JSDoc on every exported symbol; `tsd` type tests per package (`packages/core/index.test-d.ts`; `tsd` with `moduleResolution: node16` in `packages/core/package.json`).
9.4. ✅ Per-package `examples/`: `@emception/webcomponent/examples/html`, `@emception/react/examples/{basic,sdl-canvas,grader,multi-file,tests,multi-target}`, `@emception/ide/examples/{react-basic,next-basic,raw-html,single-file,tabs-only,headless,expanded,mobile,sdl-canvas}`, `@emception/node/examples/{grader,github-action}`. All shipped in tarballs.
9.5. ✅ Meta-package `emception` keeps unscoped name; README directs new users to either install meta or pick scoped packages.

---

## Execution ordering / parallelism

- **Phase 0 first (blocking).** No API changes; pure mechanical move into workspace + scope.
- Phase 1 second (blocking).
- Phase 2 (streams/canvas) + Phase 3 (workspaces+build) parallel — different subsystems (`@emception/core` tool-runner vs VFS + storage adapters in browser/node). Both depend on Phase 1.
- Phase 4 (presets) depends on Phase 3.
- Phase 5 (tests) depends on Phases 2, 3, 4.
- Phase 6 (webcomponent + react) depends on Phases 2, 3, 4.
- Phase 7 (`@emception/node`) depends on Phases 2, 3, 4. `RuntimeAdapter` already done in Phase 1.8.
- **Phase 8 (`@emception/ide`) runs in parallel w/ Phase 6** — both depend on Phases 2/3/4; ship IDE alpha + webcomponent + React together so users get full surface in one release.
- Phase 9 docs/diagnostics any time; final pass after Phases 6, 7, 8.

## Relevant files

**Workspace root** (Phase 0)

- `tools/emception/package.json` — workspace root, `"workspaces": ["packages/*"]`.
- `tools/emception/tsconfig.base.json` — shared compiler settings.
- `tools/emception/.changeset/` — release coordination.

**`packages/core/`** — `@emception/core`

- `src/createEmception.ts` (refactored), `src/tool-runner.ts`, `src/worker-protocol.ts`, `src/vfs/{overlay,lazy}.ts`.
- `src/runtime/adapter.ts` — `RuntimeAdapter` interface.
- `src/workspace/{manager,seed,zip,build-resolver,store-memory}.ts`.
- `src/io/streams.ts` — normalizers.
- `src/testing/engine.ts`, `src/testing/kinds/{stdio,stdio-file,function,clang-query,doctest,custom}.ts`, `src/testing/function-harness.ts`.
- `src/presets.ts`, `src/errors.ts`, `src/ui/config.ts` (shared validator).

**`packages/sysroot/`** — `@emception/sysroot`

- `manifest.json`, `*.tar.br`, `coi-serviceworker.js`. Migrated from [tools/emception/public/cdn/](../public/cdn/).
- `package.json` versioned by toolchain.

**`packages/browser/`** — `@emception/browser`

- `src/index.ts` — `createEmception` (browser).
- `src/runtime/browser.ts` — Web Worker spawn, fetch manifest.
- `src/worker-client.ts`, `src/worker-entry.ts` — current `bootInWorker` machinery.
- `src/workspace/store-idb.ts` — extracted from current IDBFS.
- `src/coi/index.ts` — COI preflight + SW registration helper.

**`packages/node/`** — `@emception/node`

- `src/index.ts` — `createEmception` (Node).
- `src/runtime/node.ts` — `worker_threads` spawn, fs manifest, sysroot resolution.
- `src/workspace/store-fs.ts`.
- `src/io/node-streams.ts` — Node ↔ WHATWG bridges.
- `src/worker-entry.ts`.

**`packages/xterm/`** — `@emception/xterm`

- `src/index.ts` — xterm.js bridge. xterm peer dep here only.

**`packages/react/`** — `@emception/react`

- `src/index.tsx` — `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception`.

**`packages/webcomponent/`** — `@emception/webcomponent`

- `src/emception-run.ts` — `<emception-run>`.

**`packages/ide/`** — `@emception/ide` (promoted from existing `packages/emception`)

- `src/Ide.tsx` (refactored into `IdeRoot` + composed panels).
- `src/IdeToolbar.tsx` — Run/Build/Test/Stop/Reset/Export/Import/Expand/TogglePanels.
- `src/panels/{FileExplorerPanel,TabsPanel,TerminalPanel,CanvasPanel,DockHost}.tsx`.
- `src/components/{DockGroup,DockDropOverlay,ResizeHandle}.tsx` — kept, conditionally mounted.
- `src/ide-types.ts` — extend `WorkspaceFile` w/ `visibility`; add `IdeProps`.
- `src/ide-utils.ts`, `src/workspace-presets.ts`.
- `src/legacy/adaptLegacyWorkspaceConfig.ts` — migration shim.
- `src/ui-state.ts` — `.emception/ui.json` sidecar load/save.
- `src/responsive.ts` — 768px breakpoint hook.
- `src/styles/theme.css` — dark + light token sets via CSS vars.
- `src/webcomponent/emception-ide.ts` — `<emception-ide>` wrapper (ReactDOM.createRoot internally). Subpath export `@emception/ide/webcomponent`.
- `examples/{react-basic,next-basic,raw-html,single-file,tabs-only,headless,expanded,mobile,sdl-canvas}/`.

**`packages/cli/`** — `@emception/cli`

- `src/{doctor,cdn-export,run,test}.ts`. `bin` field.

**Meta** — `emception` (unscoped, back-compat)

- [tools/emception/package.json](../package.json) — thin wrapper.
- `src/index.ts` — `export * from '@emception/browser'; export * as xterm from '@emception/xterm';`.

**Other**

- `packages/sysroot/usr/include/doctest.h` — vendored.
- `tools/emception/scripts/build-clang-query.ts` (potential) — add clang-query to `@emception/sysroot` if feasible.
- [scripts/sync-emception-cdn.mjs](../../../scripts/sync-emception-cdn.mjs) — thin shim.
- [demos/emception-react/](../../../demos/emception-react/) — migrate to `@emception/react` as dogfood.

## Verification

1. **Stream round-trip.** Headless: feed stdin async iterable yielding 3 chunks; assert stdout collected in order via `WritableStream` sink.
2. **Abort/timeout.** `run('./inf-loop', [], { timeoutMs: 100 })` → `timedOut: true`; worker survives next call.
3. **Canvas binding.** Playwright: SDL demo renders into supplied `<canvas>`; assert non-blank pixel buffer.
4. **Workspace isolation.** Two workspaces `a`/`b` w/ same path `main.c`; edits don't leak.
5. **Seed policies.** `once` (skip), `overwrite` (rewrite), `merge` (add only), `WorkspaceConflictError` on `once` content diff.
6. **Visibility.** `listFiles({ includeHidden: false })` omits hidden; build still succeeds using hidden file.
7. **Build resolver precedence.** Unit: preset → workspace scalar overwrite; arrays concat+dedup; `defines`/`env` merge by key; call-site beats workspace; `BuildConfigError` on impossible combos.
8. **Build persistence.** `setBuild()` writes `.emception/build.json`; reload from IDB equivalent; included in `exportZip()`.
9. **Test plan kinds.** One integration per kind: `stdio`, `stdio-file`, `clang-query` (AST-dump JSON), `doctest`, `custom`.
10. **UI ↔ headless parity.** Snapshot serializing `<EmceptionRun>` props (incl `workspace.build`) → `<emception-run>` attrs and back; assert semantic equivalence via shared validator.
11. **Bundler matrix.** Minimal Vite/Next/Webpack 5 apps build clean w/ new subpath exports.
12. **Tarball.** `npm pack --dry-run` — exports resolve, CDN subpath reachable, no missing files.
13. **`emception doctor`** — unit + CI integration.
14. **Type tests (`tsd`).** `run('clan', [])` = type error; `tools.clang([…])` autocompletes; `TestPlan.cases[0].kind` narrows.
15. **Node smoke.** `node --test` script imports `@emception/node`, runs `compileAndRun(helloWorld)`, asserts exit 0 + stdout `"hello\n"`. CI matrix Node 18/20/22.
16. **Node workspace persistence.** Create via `store-fs`, dispose, recreate core, assert files + build config survive. Concurrency: two cores writing same workspace serialize via lockfile.
17. **Cross-runtime parity.** Same `TestPlan` → same `TestReport` (modulo timing) browser (Playwright) vs Node `worker_threads`.
18. **Workspace topology.** `pnpm -r build` builds all in topo order. `pnpm -r --filter @emception/core test` runs only core. CI script fails if any `@emception/*` version drifts from meta `emception`.
19. **Back-compat smoke.** Install meta `emception` in fresh project; current README quick-start works w/o code change.
20. **Sysroot decoupling.** Bump `@emception/sysroot` (LLVM patch) w/o touching others; consumers pick up via peer-dep range.
21. **IDE reactive toggles.** Mount `<Ide>`, flip `enableTerminal` true→false→true; assert TerminalPanel unmounts/remounts; assert active workspace state preserved across flips. Repeat for `enableCanvas`, `enableFileExplorer`, `enableTabs`, `enableDocking`.
22. **IDE expand.** Toggle `expanded` prop; assert IDE re-parents to body portal w/ `position:fixed;inset:0`; assert `onExpandedChange` called on toolbar Expand click. Verify `expandable: false` hides the button.
23. **IDE resize handle.** `resizable: true` shows bottom-edge drag handle; dragging changes container height; height persists via `persistUiState`.
24. **IDE single-file mode.** `<Ide enableWorkspace={false} enableTabs={false} enableFileExplorer={false} enableDocking={false} files={{ 'main.c': src }} />` renders Monaco-only editor; no FileExplorer/Tabs/Dock in DOM.
25. **IDE inline-files reactivity.** Mount w/ `files`, type into editor, change `files` prop; assert untouched files updated, edited file's cursor + dirty state preserved.
26. **IDE visibility filter.** Workspace w/ public + hidden + solution files; default `<Ide>` shows only public; flipping `showHiddenFiles` reveals hidden in tree without re-mounting core.
27. **IDE multi-instance.** Two `<Ide>` w/ different `workspace.name` on same page coexist; IDB keys distinct; no state bleed.
28. **IDE custom-element.** Mount `<emception-ide preset="cpp" expanded enable-terminal="false">` in plain HTML page; assert internal React root in light DOM (no shadow), attribute changes propagate, `ide-ready` event fires.
29. **IDE mobile responsive.** Resize viewport <768px; assert FileExplorer becomes drawer, TerminalPanel becomes bottom sheet, dock drag disabled. Resize back >768px → desktop layout restored.
30. **IDE toolbar subset.** `toolbar: { run: true, test: false }` renders only Run button; `toolbar: false` renders no toolbar at all.
31. **IDE readOnly.** `readOnly: true` disables editor mutation, hides/disables Run/Test/Reset/Import buttons; Export still allowed.
32. **IDE theme.** `theme: 'light'` swaps CSS vars; custom token map (`{ '--ide-bg': '#fff' }`) applies; reactive change re-themes without remount.
33. **IDE UI state persistence.** Open tabs + drag dock layout + resize; reload page; assert layout + open tabs + active file restored from `.emception/ui.json`.
34. **IDE legacy migration.** Pass legacy `WorkspaceConfig` through `adaptLegacyWorkspaceConfig`; assert produces equivalent `WorkspaceOptions` + `IdeProps`; deprecation warning logged once.

## Decisions

- **Repo: 9-package monorepo** under `tools/emception/packages/*` (`core`, `sysroot`, `browser`, `node`, `xterm`, `react`, `webcomponent`, `ide`, `cli`). Scope `@emception/*`; unscoped `emception` meta kept as batteries-included alias (re-exports `@emception/browser` + `@emception/xterm`) so `npm i emception` keeps working. Canvas helpers folded into `@emception/browser` (no separate canvas package).
- **Lock-step versioning v1**, via `changesets`. All `@emception/*` + meta bump together. Independent post-1.0. `@emception/sysroot` always independent by toolchain.
- **Sysroot = own JS-free package** (`@emception/sysroot`), peer dep of `@emception/browser` + `@emception/node`. Solves install-size + toolchain-decoupling + open Q8.
- **Presets = configuration**, not separate npm packages.
- **Hosting**: jsDelivr default + self-host CLI.
- **I/O**: streams canonical; strings/functions/xterm = adapters → streams.
- **Workspaces**: per-name IDB, mount `/workspace/<name>`, sidecar `.emception/meta.json`.
- **Build config travels w/ workspace** in `.emception/build.json`; precedence preset → workspace → call-site; arrays concat, scalars overwrite, objects merge.
- **Multi-binary builds:** flat clang stays single-output (override via `CompileOptions.sources/output`). CMake workspaces opt-in to multi-target via `cmake.targets?: string[]` — list of target names; build resolver invokes `cmake --build --target <name>` per entry with shared flags. No per-target override map; per-target customization lives in the user's `CMakeLists.txt`.
- **Visibility = metadata tag**, not second filesystem.
- **Tests**: discriminated-union plan, 4 built-in kinds (`stdio`, `stdio-file`, `clang-query`, `doctest`) + `custom`. No `function` kind — use `stdio` or `doctest` instead.
- **UI parity**: HTML attrs = kebab mirror; React props = camelCase mirror. Shared validator in `@emception/core`.
- **Node runtime** in scope via `@emception/node`: `worker_threads`, fs workspace store, disk manifest from `@emception/sysroot`. Canvas + xterm browser-only (throw `RuntimeFeatureUnavailableError` in Node).
- **IDE in scope this iteration** as `@emception/ide` (promoted from existing `packages/emception`). Reactive panel composition (canvas/terminal/workspace/docking/tabs all toggleable), expand-to-webpage mode (not OS fullscreen), vertical resize handle, file-visibility filtering, theme prop, readOnly mode, built-in toolbar w/ subset opt-out, UI state persisted in workspace sidecar, mobile responsive (768px), `<emception-ide>` custom-element wrapper (light DOM, no shadow root) for raw HTML / Next, legacy `WorkspaceConfig` migration shim. React 19 peer. Phase 6 + Phase 8 ship in parallel.
- **Out of scope this iteration**: per-language sub-packages, scaffolder, Vue/Svelte adapters, Deno/Bun (likely work via Node compat but not formally tested), serverless cold-start optimizations.

## Testing discipline (project-wide)

**No mocks. Ever.** All tests in `@emception/*` and downstream packages drive real WASM toolchains, real VFS instances, and real workers. If a runtime piece needs the sysroot built, build it. Mock objects, fake adapters, and stubbed worker channels are forbidden — they hide the cross-thread bugs this overhaul exists to surface. The `node:test` zero-dep harness is the standard runner; `tsd` for type-only assertions.

## Open questions

All resolved. Decisions folded above. Historical record:

1. **clang-query — resolved.** `clang -Xclang -ast-dump=json` + TS matcher. No clang-query WASM binary.
2. **Function-kind marshaling — resolved.** `function` test kind dropped entirely. Use `stdio` or `doctest` for behavior checks.
3. **xterm raw mode — resolved.** Default line-buffered + local echo; opt-in raw via `stdin: { xterm, raw: true }`.
4. **Canvas lifecycle — resolved.** OffscreenCanvas only v1, transfer permanent (documented). Main-thread proxy deferred.
5. **IDB namespace — resolved.** Fixed DB name `'emception'` default; optional `namespace` opt-in.
6. **Multi-target builds — resolved (revised 2026-04-25).** Flat clang stays single-output. CMake workspaces support `cmake.targets?: string[]` — shared flags, one `cmake --build --target <name>` invocation per entry. No per-target flag overrides (those belong in `CMakeLists.txt`).
7. **Glob in `sources` — resolved.** No built-in glob expansion; CMake workspaces handle globbing via CMake itself.
8. **Sysroot CDN — resolved.** `@emception/sysroot` regular npm package; jsDelivr default for browser, normal dep for Node.
9. **Deno / Bun — resolved.** Deferred. WHATWG-first design lets users try at own risk.
10. **Canvas package — resolved.** Folded into `@emception/browser`.
11. **`@emception/cmake-presets` package — resolved.** Not creating one. CMake presets live as files inside the user's CMake workspace.
12. **IDE Monaco loading — resolved.** Lazy-load via `@monaco-editor/react` default; opt-in bundled via `@emception/ide/bundled` subpath.
13. **IDE expand semantics — resolved.** CSS portal (`position:fixed; inset:0`) fills webpage area; not OS fullscreen. `resizable` = bottom-edge drag for in-page vertical growth.
14. **`<emception-ide>` shadow DOM — resolved.** Light DOM. No shadow root. Host CSS leaks in by design (predictable, Monaco-safe). Theme via `theme` prop / CSS vars.
