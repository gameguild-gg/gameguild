# Building from source

Most consumers install `@gameguild/emception-browser`; its default manifest is
the matching, versioned `@gameguild/emception-toolchain` release. Build from
source when changing Emscripten/LLVM/Python, generated glue patches, libraries,
or artifact packaging.

## Prerequisites

- Node.js (current LTS) and pnpm
- CMake 3.20 or newer
- host Python 3
- Ninja, curl, and a working Emscripten build environment

## Quick build

```bash
cd tools/emception
pnpm install
pnpm run build:all
```

The initial toolchain build is large. Package-only work can use:

```bash
pnpm run test:packages
pnpm run typecheck:packages
pnpm run build:packages
```

## Canonical release pipeline

`build:all` first builds the package layers and toolchain inputs, then runs the
serial release pipeline:

| Phase | Scripts | Output / invariant |
| --- | --- | --- |
| Packages | `build:packages:core`, `build:packages:xterm`, `build:packages:browser`, `typecheck` | runnable TypeScript packages before heavy work |
| SDK/tools | `build:emsdk`, `build:emscripten:warmup`, `build:toolchain:parallel` | compiler, linker, Binaryen, CPython, CMake, curl |
| Graphics | `build:graphics` | SDL3, ImGui, raylib, Allegro libraries and runtime pairs |
| Working sysroot | `build:sysroot` | mutable `sysroot/` assembled from build outputs |
| Freeze | `build:stage:sysroot` | clean `build/stage/sysroot` plus fingerprint receipt |
| Patch | `patch:glue` | versioned patches applied only to staged generated glue |
| Manifest | `build:manifest` | schema-v2 manifest and raw canonical `build/cdn` tree |
| Package | `build:brotli`, `build:bundles`, `build:assert-no-dupes` | compressed, hash-checked release payload |
| Publish staging | `deploy:cdn`, `stage:toolchain:cdn` | local CDN and authoritative toolchain npm payload |
| Compatibility | `stage:core:cdn` | temporary compatibility copy in `emception/cdn` |

`build:pipeline` starts from an existing working sysroot. `build:cdn` starts
from an existing staged sysroot. Neither manifest generation nor glue patching
accepts the mutable `sysroot/` as release input.

## Generated glue policy

Emscripten glue is generated code and may change when the SDK changes. All
required adaptations are centralized in `scripts/lib/glue-patches.mjs` and
executed by `patch:glue`.

The patcher verifies known source markers and matching `.mjs`/`.wasm` pairs. A
missing pair or an unknown generated shape fails the build instead of producing
a release that the IDE would need to repair at runtime.

## Manifest v2 and ABI profiles

`build:manifest` records:

- package artifact version and complete build fingerprint;
- Browser runtime ABI and glue patch-set version;
- pinned tool versions;
- hashes, sizes, executability, and bundle placement for every file;
- each WASM profile's matching glue path, WASM path, imports, exports, and
  pair-derived profile hash.

The Browser validates this metadata before boot. This is the executable
boundary between a generated toolchain release and package code.

## Tool build flags

Each tool is standalone; Emception does not use Emscripten `MAIN_MODULE` /
`SIDE_MODULE`. Tools use modularized ES modules, explicit filesystem/runtime
exports, Asyncify for browser I/O, and isolated linear memory.

## Release ownership

`@gameguild/emception-toolchain` is the canonical artifact package. The
`emception/cdn` copy is a compatibility bridge and must not become a second
independently generated release. Both copies are staged from the same
`build/cdn` output.
