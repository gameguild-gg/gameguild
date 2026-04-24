# `tools/emception/packages/` — `@emception/*` monorepo

9 packages implementing the [DX overhaul plan](../docs/dx-overhaul-plan.md).

| Package                                    | Tier    | Purpose                                                                                         |
| ------------------------------------------ | ------- | ----------------------------------------------------------------------------------------------- |
| [`@emception/core`](core/)                 | Core    | Types, EmceptionCore, VFS, tool registry, build resolver, test engine, presets, RuntimeAdapter. |
| [`@emception/sysroot`](sysroot/)           | Assets  | `.tar.br` toolchain bundles + `manifest.json` + `coi-serviceworker.js`. JS-free.                |
| [`@emception/browser`](browser/)           | Adapter | Web Worker spawn, IDB workspace store, OffscreenCanvas + SDL helpers, COI preflight.            |
| [`@emception/node`](node/)                 | Adapter | `worker_threads` spawn, fs workspace store, disk manifest.                                      |
| [`@emception/xterm`](xterm/)               | I/O     | xterm.js stdin/stdout/stderr bridge.                                                            |
| [`@emception/react`](react/)               | UI      | `<EmceptionRun>`, `<EmceptionTerminal>`, `<EmceptionCanvas>`, `useEmception()`.                 |
| [`@emception/webcomponent`](webcomponent/) | UI      | `<emception-run>` custom element. No framework deps.                                            |
| [`@emception/ide`](ide/)                   | UI      | Reactive `<Ide>` React 19 + `<emception-ide>` custom-element wrapper (light DOM).               |
| [`@emception/cli`](cli/)                   | Tool    | `doctor`, `cdn-export`, `run`, `test`.                                                          |

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

- **Phase 0.1 — workspace bootstrap**: ✅ all 9 package skeletons, typecheck cleanly.
- **Phase 0.2 — source migration**: ✅ complete.
  - `@emception/core` owns: `vfs/{interface,overlay,manifest}`, `tty/{io-provider,line-buffer}`, `worker-protocol`, presets, errors, full type surface.
  - `@emception/xterm` owns: `TTYBridge`.
  - `@emception/browser` owns: `LazyFS`, `IDBFS`, `mountVFSFS`, `createVFSManager`, `ToolRunner`, `MiniShell`, `worker-client`, `worker-entry` (+ side-effect subpath `@emception/browser/worker`), `boot()`, `bootInWorker()`, `createEmception()`, plus the `emscripten/`, `loader/`, `net/` adapters.
  - Legacy `@emception/ide/src/components/*` staged for the Phase 8 reactive rewrite.
- **Phase 0.3 — meta wrapper**: ✅ `tools/emception/src/index.ts` is now `export * from '@emception/browser'; export * as xterm from '@emception/xterm';`. Worker entry shim is `import '@emception/browser/worker';`. Bundle ~250 bytes; full toolchain pulled in by downstream bundlers via the scoped packages.
- **Phase 0.5 — changesets**: ✅ [`tools/emception/.changeset/config.json`](../.changeset/config.json) — lock-step versioning for all `@emception/*` + `emception` (sysroot independent).
- **Phase 0.8 — topological build pipeline**: ✅ `npm run build:packages --workspace=tools/emception` builds all 8 in dependency order (core → xterm → browser → node → react → webcomponent → ide → cli). Companion: `typecheck:packages`, `clean:packages`.
- **Phase 1.6 — typed errors**: ✅ `EmceptionError` hierarchy in `@emception/core/src/errors.ts`.
- **Phase 1.7 — SSR safety**: ✅ all packages mark `"sideEffects": false`; `@emception/core` has zero DOM/browser imports.
- **Phase 0.6 — sysroot asset migration**: ⏳ deferred. The 1.3 GB CDN content is gitignored and produced by the existing 17-step build pipeline writing to `tools/emception/public/cdn/`. Needs the build scripts (`populate-sysroot.ts`, `generate-manifest.ts`, `generate-bundles.ts`, `deploy-cdn.ts`) reworked to target `packages/sysroot/` instead. Track in plan, tackle as a focused task later.

## Scripts

```bash
# from repo root
npm run typecheck:packages --workspace=tools/emception
npm run build:packages     --workspace=tools/emception
npm run clean:packages     --workspace=tools/emception

# meta package (thin wrapper)
cd tools/emception && npm run build:lib
```

See [`tools/emception/docs/dx-overhaul-plan.md`](../docs/dx-overhaul-plan.md) for the full plan.
