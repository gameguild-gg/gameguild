# @gameguild/emception-browser

Browser runtime adapter for Emception. It owns Web Worker boot, manifest/ABI
validation, workspace persistence, terminal stream adaptation, and canvas
artifact execution.

## Install

```bash
npm install @gameguild/emception-browser
```

Install `@gameguild/emception-xterm` and `@xterm/xterm` only when mounting an
interactive terminal.

## Headless usage

```ts
import { createEmception } from '@gameguild/emception-browser';

const emception = await createEmception({
  tty: 'none',
  onStdout: (text) => console.log(text),
});

const result = await emception.compileAndRun(
  '#include <stdio.h>\nint main(){puts("hello");}',
);
emception.dispose();
```

When `manifestUrl` is omitted, the Browser loads the matching version of
`@gameguild/emception-toolchain` from its pinned jsDelivr URL. To self-host,
copy that package's `cdn/` directory and pass the resulting manifest URL.

## Canvas API

The public `canvas` surface keeps generated glue and raw WebAssembly concerns
out of UI packages:

```ts
import { ToolchainPreset } from 'emception';

const session = await emception.canvas.buildAndStart({
  toolchain: ToolchainPreset.SDL_CPP,
  sourcePath: '/home/user/main.cpp',
  wasmPath: '/home/user/main.wasm',
}, {
  canvas: document.querySelector('canvas')!,
});

if ('phase' in session) {
  const failure = session.phase === 'compile' ? session.compile : session.link;
  console.error(failure.stderr);
} else {
  session.stop();
}
```

`build`, `start`, `buildAndStart`, and `stop` select a manifest profile, load
its matched runtime factory, construct browser imports, and manage lifecycle.

## Cross-origin isolation

Toolchain Workers require `SharedArrayBuffer`. Serve
`Cross-Origin-Opener-Policy: same-origin` and
`Cross-Origin-Embedder-Policy: require-corp`, or use the supplied preflight and
service worker:

```ts
import { ensureCrossOriginIsolated } from '@gameguild/emception-browser';

await ensureCrossOriginIsolated();
```

## Stable surface

Use `createEmception()` and the returned `BrowserEmceptionAPI` for application
code. Low-level Worker/VFS exports remain available for advanced runtime
adapters, but presentation packages should not wrap them.
