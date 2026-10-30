# @gameguild/emception-browser

## 4.5.1

### Patch Changes

- [#1034](https://github.com/gameguild-gg/gameguild/pull/1034) [`0f61cbd`](https://github.com/gameguild-gg/gameguild/commit/0f61cbdda2659fe565dd44f3c0e903a24b0d90d8) Thanks [@tolstenko](https://github.com/tolstenko)! - Fix emcc: run Emscripten's Node.js build tools (compiler.mjs, preprocessor.mjs, acorn-optimizer.mjs) with their real sources in the browser worker instead of the symbols-only stub, so `em.run('emcc', ...)` produces the requested output artifact ([#451](https://github.com/gameguild-gg/gameguild/issues/451)).

  The old runJavaScript stub for compiler.mjs never emitted the generated JS glue or the `//FORWARDED_DATA:` payload, so Python's emscripten.py aborted with "Did not receive forwarded data in pre output - process failed?" and no output file was written. The new node-tool loader builds the tools' ESM graph from the CDN VFS (blob URLs, node-builtin shims with VFS-backed fs, import-cycle merging for src/modules.mjs ↔ src/parseTools.mjs, vm shim with completion-value semantics and lexical-declaration harvesting). The sysroot now also ships node_modules/acorn and html/ (shell.html/shell.css) required by those tools; LazyFS gains an HTTP fallback for manifest-unlisted files so older CDN manifests keep working.

- Updated dependencies [[`0f61cbd`](https://github.com/gameguild-gg/gameguild/commit/0f61cbdda2659fe565dd44f3c0e903a24b0d90d8)]:
  - emception@4.5.1
  - @gameguild/emception-xterm@4.5.1

## 4.5.0

### Minor Changes

- [`3e585a6`](https://github.com/gameguild-gg/gameguild/commit/3e585a61e23ef9b8441240e241d6638255cbeadf) Thanks [@mathrmartins](https://github.com/mathrmartins)! - Publish a reproducible, lockfile-driven Emception Toolchain with verified build receipts, source provenance, Linux-only release automation, and a Browser manifest URL pinned to the matching Toolchain version.

### Patch Changes

- [`e174aea`](https://github.com/gameguild-gg/gameguild/commit/e174aea68408263cf9cff4ff555547de6eb59c97) Thanks [@mathrmartins](https://github.com/mathrmartins)! - Isolate and clean browser-native scratch outputs so sequential assessment runs cannot reuse stale object or WASM bytes.
- Updated dependencies [[`3e585a6`](https://github.com/gameguild-gg/gameguild/commit/3e585a61e23ef9b8441240e241d6638255cbeadf)]:
  - emception@4.5.0
  - @gameguild/emception-xterm@4.5.0

## 4.0.0

### Major Changes

- # v4.0.0 — Major infrastructure and frontend rework

  Rewrote the Emception build pipeline, package layout, and frontend
  integrations. This is a synthetic breaking-change declaration — the
  3.x → 4.x major bump covers all changes accumulated since v3.11.0
  that were never tagged as breaking at the time.

  ## Breaking changes
  - Migrated the Emception package stack from npm to pnpm
  - Reorganized `tools/emception/packages/*` into independent publishable units
  - Reworked the worker boot chain and `createEmception()` surface
  - Frontend demos moved under `tools/emception/apps/*` with new names

  Consumers upgrading from 3.x should treat the public API surface as
  changed and re-pin to `emception@4.x` and the `@gameguild/emception-*` scope.

### Patch Changes

- Updated dependencies []:
  - emception@4.0.0
  - @gameguild/emception-xterm@4.0.0
