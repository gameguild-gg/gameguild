# @emception/browser

Browser runtime adapter for [emception](https://github.com/gameguild-gg/gameguild/tree/main/tools/emception). Spawns Web Workers, persists workspaces in IndexedDB, ships OffscreenCanvas + SDL/ImGui helpers, and bundles the cross-origin-isolation preflight.

This is the package most browser apps want. The bare-metal `npm i emception` meta-package forwards to it.

## Install

```bash
npm install @emception/browser @emception/sysroot
# Optional, for terminal:
npm install @emception/xterm @xterm/xterm
```

`@emception/sysroot` is the WASM toolchain payload. In production you can either bundle it (npm install) or load it from a CDN — see the manifest URL options below.

## Quick start

```ts
import { createEmception } from '@emception/browser';

const em = await createEmception({
  manifestUrl: 'https://cdn.jsdelivr.net/npm/@emception/sysroot/manifest.json',
  tty: 'none',
});

const result = await em.compileAndRun('int main(){ printf("hi\\n"); return 0; }');
console.log(result.stdout); // 'hi\n'
```

With xterm:

```ts
import { Terminal } from '@xterm/xterm';
import { fromXterm, toXterm } from '@emception/xterm';

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

The sysroot Workers need `SharedArrayBuffer`, which requires `Cross-Origin-Opener-Policy: same-origin` and `Cross-Origin-Embedder-Policy: require-corp`. Self-host the COI service worker that ships with `@emception/sysroot`, or import the preflight helper:

```ts
import { ensureCrossOriginIsolated } from '@emception/browser';
await ensureCrossOriginIsolated();
```

Throws `CrossOriginIsolationError` if SAB isn't available.

## Surface

Top-level exports include `boot`, `bootInWorker`, `createEmception`, `ToolRunner`, `MiniShell`, `LineBuffer`, `TTYBridge`, `createBrowserBridge`, `createVFSManager`, `decompressBrotli`, `isBrotliSupported`, `detectAsyncStrategy`, plus `IDBFS` / `LazyFS` / `mountVFSFS` for advanced consumers composing their own `VFSManager`.

Types: `BootResult`, `CreateEmceptionOptions`, `EmceptionAPI`, `RunOptions`, `ToolResult`, `IOProvider`, `VFSManager`, `FileEntry`, `FSManifest`, `IDBFSOptions`, `MountVFSFSOptions`.

## Roadmap

The current `createEmception` lives here.
