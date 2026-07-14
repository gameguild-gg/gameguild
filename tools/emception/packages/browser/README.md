# @gameguild/emception-browser

Browser runtime adapter for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Spawns Web Workers, persists workspaces in IndexedDB, ships OffscreenCanvas + SDL/ImGui helpers, and bundles the cross-origin-isolation preflight.

This is the package most browser apps want. The bare-metal `npm i emception` meta-package forwards to it.

## Live Demo

Try it at [gameguild-gg.github.io/gameguild/](https://gameguild-gg.github.io/gameguild/) — features a live IDE with working templates for C++, SDL3, Raylib, CMake, and Python.

## Install

```bash
npm install @gameguild/emception-browser
# Optional, for terminal:
npm install @gameguild/emception-xterm @xterm/xterm
```

The CDN payload is bundled inside the `emception` package under `emception/cdn/*`. By default, `createEmception()` points at the latest published `emception/cdn/manifest.json` on jsDelivr. For self-hosting, copy `emception/cdn/*` into your app's public `/cdn/` directory and pass `manifestUrl: '/cdn/manifest.json'`.

## Quick start

```ts
import { createEmception } from '@gameguild/emception-browser';

const em = await createEmception({
  manifestUrl: 'https://cdn.jsdelivr.net/npm/emception/cdn/manifest.json',
  tty: 'none',
});

const result = await em.compileAndRun('int main(){ printf("hi\\n"); return 0; }');
console.log(result.stdout); // 'hi\n'
```

With xterm:

```ts
import { Terminal } from '@xterm/xterm';
import { fromXterm, toXterm } from '@gameguild/emception-xterm';

const xterm = new Terminal();
xterm.open(document.getElementById('term')!);

const em = await createEmception({
  manifestUrl: '/cdn/manifest.json',
  stdin: fromXterm(xterm),
  stdout: toXterm(xterm),
  stderr: toXterm(xterm),
});
```

## Cross-origin isolation

The toolchain Workers need `SharedArrayBuffer`, which requires `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp`. Self-host a root-level `/coi-serviceworker.js` (for example by copying `node_modules/@gameguild/emception-browser/dist/coi-serviceworker.js` into your app's public root), or import the preflight helper:

```ts
import { ensureCrossOriginIsolated } from '@gameguild/emception-browser';
await ensureCrossOriginIsolated();
```

Throws `CrossOriginIsolationError` if SAB isn't available.

## Surface

Top-level exports include `boot`, `bootInWorker`, `createEmception`, `ToolRunner`, `MiniShell`, `LineBuffer`, `TTYBridge`, `createBrowserBridge`, `createVFSManager`, `decompressBrotli`, `isBrotliSupported`, `detectAsyncStrategy`, plus `IDBFS` / `LazyFS` / `mountVFSFS` for advanced consumers composing their own `VFSManager`.

Types: `BootResult`, `CreateEmceptionOptions`, `EmceptionAPI`, `RunOptions`, `ToolResult`, `IOProvider`, `VFSManager`, `FileEntry`, `FSManifest`, `IDBFSOptions`, `MountVFSFSOptions`.
