# Emception packages

Seven public packages are built from this workspace:

| Package | Tier | Responsibility |
| --- | --- | --- |
| [`@gameguild/emception-toolchain`](toolchain/) | artifacts | canonical manifest, compressed sysroot, generated glue/WASM pairs |
| [`emception`](core/) | core | runtime-neutral contracts, VFS/build/test engines, presets |
| [`@gameguild/emception-browser`](browser/) | runtime | Worker boot, browser persistence, streams, canvas execution |
| [`@gameguild/emception-xterm`](xterm/) | I/O | xterm byte-stream adapter |
| [`@gameguild/emception-react`](react/) | UI | React bindings and hooks |
| [`@gameguild/emception-webcomponent`](webcomponent/) | UI | framework-free `<emception-run>` |
| [`@gameguild/emception-ide`](ide/) | UI | vanilla IDE React component and custom element |

The workspace root is private and is not a meta-package.

## Dependency direction

```text
toolchain artifacts ──loaded by──> browser
core contracts ──────────────────> browser ──> IDE / React / custom element
core contracts ──────────────────> xterm  ───> optional terminal integration
```

Generated artifact ownership does not flow upward into UI packages. The
Browser validates and executes artifact profiles; the IDE only consumes the
Browser's public API.

## Verification

```bash
cd tools/emception
pnpm run test:packages
pnpm run typecheck:packages
pnpm run build:packages
```
