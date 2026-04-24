# `tools/emception/packages/` — `@emception/*` monorepo

9 packages implementing the [DX overhaul plan](../docs/dx-overhaul-plan.md).

| Package                                              | Tier    | Purpose                                                                                          |
| ---------------------------------------------------- | ------- | ------------------------------------------------------------------------------------------------ |
| [`@emception/core`](core/)                           | Core    | Types, EmceptionCore, VFS, tool registry, build resolver, test engine, presets, RuntimeAdapter.  |
| [`@emception/sysroot`](sysroot/)                     | Assets  | `.tar.br` toolchain bundles + `manifest.json` + `coi-serviceworker.js`. JS-free.                 |
| [`@emception/browser`](browser/)                     | Adapter | Web Worker spawn, IDB workspace store, OffscreenCanvas + SDL helpers, COI preflight.             |
| [`@emception/node`](node/)                           | Adapter | `worker_threads` spawn, fs workspace store, disk manifest.                                       |
| [`@emception/xterm`](xterm/)                         | I/O     | xterm.js stdin/stdout/stderr bridge.                                                             |
| [`@emception/react`](react/)                         | UI      | `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception()`.                  |
| [`@emception/webcomponent`](webcomponent/)           | UI      | `<emception-run>` custom element. No framework deps.                                             |
| [`@emception/ide`](ide/)                             | UI      | Reactive `<Ide>` React 19 + `<emception-ide>` custom-element wrapper (light DOM).                |
| [`@emception/cli`](cli/)                             | Tool    | `doctor`, `cdn-export`, `run`, `test`.                                                           |

The unscoped meta-package [`emception`](../package.json) at `tools/emception/` re-exports `@emception/browser` + `@emception/xterm` for back-compat.

## Build order (topological)

```
sysroot
  → core
    → browser, node (parallel)
      → xterm
        → react, webcomponent, ide (parallel)
          → cli
            → emception (meta)
```

## Status

- **Phase 0.1 — workspace bootstrap**: ✅ all 9 package skeletons created, typecheck cleanly.
- **Phase 0.2 — source migration (first slice)**: ✅ pure runtime-agnostic pieces moved into `@emception/core`:
  - `vfs/interface.ts`, `vfs/overlay.ts`, `vfs/manifest.ts` (extracted types)
  - `tty/io-provider.ts`, `tty/line-buffer.ts`
  - `worker-protocol.ts`
  - `TTYBridge` moved into [`@emception/xterm`](xterm/src/bridge.ts).
  - Legacy IDE source staged in [`@emception/ide/src/components`](ide/src/components/) for the Phase 8 rewrite.
- **Phase 0.5 — changesets**: ✅ [`tools/emception/.changeset/config.json`](../.changeset/config.json) configured for lock-step versioning of all `@emception/*` + `emception` (sysroot independent).
- **Phase 0.8 — topological build pipeline**: ✅ `npm run build:packages --workspace=tools/emception` builds all 8 packages in dependency order. Companion: `typecheck:packages`, `clean:packages`.
- **Phase 0.2 — remaining**: 🚧 large slices still pending — `LazyFS` / `IDBFS` / `createVFSManager` → `@emception/browser`; `ToolRunner`, `boot()`, `worker-client`, `worker-entry` → `@emception/browser`. Once those land, Phase 0.3 can flip the meta `emception` package into a re-export wrapper.

## Scripts

```bash
# from repo root
npm run typecheck:packages --workspace=tools/emception
npm run build:packages     --workspace=tools/emception
npm run clean:packages     --workspace=tools/emception
```

See [`tools/emception/docs/dx-overhaul-plan.md`](../docs/dx-overhaul-plan.md) for the full plan.
