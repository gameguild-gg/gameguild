# @emception/node

Node 20+ runtime adapter for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Implements `RuntimeAdapter` from `@emception/core` against `node:worker_threads`, `node:fs/promises`, and the WHATWG stream bridges that ship with modern Node.

## Status

- Adapter primitives, fs workspace store, manifest loader, and stream bridges are **implemented and tested**.
- `createEmception()` itself still throws — the worker-client orchestration extraction (Phase 7.2) is the next milestone.

## Install

```bash
npm install @emception/node @emception/sysroot
```

`@emception/sysroot` is a peer dep — pin the version of LLVM you want to ship.

## What you can use today

### `createNodeRuntimeAdapter(opts?)`

Returns a `RuntimeAdapter` that any future orchestrator can drive:

```ts
import { createNodeRuntimeAdapter } from '@emception/node';

const adapter = createNodeRuntimeAdapter({
  workerEntry: new URL('./my-worker.mjs', import.meta.url),
  fsRoot: '/var/lib/emception',
});
const manifest = await adapter.loadManifest({ source: 'default' });
```

### `FsWorkspaceManager` / `createFsWorkspaceManager(opts)`

Disk-backed `WorkspaceManager` implementing the full `@emception/core` contract. Atomic temp+rename writes, `.emception/{meta,build}.json` sidecars, traversal-guarded paths, full `SeedPolicy` (`once`/`overwrite`/`merge`) semantics.

```ts
import { createFsWorkspaceManager } from '@emception/node';

const mgr = await createFsWorkspaceManager({ root: '/var/lib/emception' });
const ws = await mgr.open({ name: 'submission-42', seed: { 'main.cpp': source }, seedPolicy: 'overwrite' });
await ws.writeFile('extra.h', '#pragma once\n');
```

### `loadManifest(opts?)`

Reads a `FSManifest` either from a local path (`{ path }`), a URL (`{ url }` — uses global `fetch`), or the bundled `@emception/sysroot/manifest.json` by default (resolved via `createRequire`).

### Stream bridges

- `readableToWeb(stream)` — Node `Readable` → `ReadableStream<Uint8Array>` (uses static `Readable.toWeb`).
- `writableToWeb(stream)` — Node `Writable` → `WritableStream<Uint8Array>`.
- `processStdio()` — `{ stdin, stdout, stderr }` WHATWG-shaped wrappers around `process.std*`.

### `wrapNodeWorker(worker)`

Wraps a `node:worker_threads.Worker` in an `EventTarget` facade with WeakMap-backed listener tracking, so `removeEventListener` actually unsubscribes (which the raw Node Worker doesn't support cleanly).

## Tests

```bash
npm test --workspace=@emception/node
```

9 tests (zero deps): adapter identity, transferable detection, `spawnWorker` round-trip + listener removal, fs store round-trip + traversal guard, manifest delegation.

## Roadmap

Phase 7.2 will replace the throwing `createEmception` stub with full worker orchestration once the browser package's `worker-client` is extracted into `@emception/core`. Until then, drive the adapter primitives directly from your own orchestrator.
