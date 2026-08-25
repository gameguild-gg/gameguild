# Building from source

Most consumers install `@gameguild/emception-browser`; its default manifest is
the matching, versioned `@gameguild/emception-toolchain` release. Build from
source when changing Emscripten/LLVM/Python, generated glue patches, libraries,
or artifact packaging.

## Supported build environment

The release gate runs only on Linux (`ubuntu-latest`). Local macOS arm64 builds
run the same locked maintenance pipeline and are supported for validation, but
only the Linux gate is a release guarantee. Local Windows builds are useful
during development, but they are best-effort.

Linux prerequisites are Node.js, pnpm, host Python 3, CMake, Ninja, curl,
Brotli, and a C/C++ build environment. The locked EMSDK is downloaded by the
Toolchain itself.

On macOS arm64, install Xcode Command Line Tools, Node.js, pnpm, and the host
build tools before running the pipeline:

```bash
xcode-select --install
brew install cmake ninja brotli
```

Xcode supplies Clang and the system curl. The locked EMSDK supplies its own
Node.js and Python runtimes after the host bootstrap completes.

## Quick build

```bash
pnpm install --frozen-lockfile --ignore-scripts
pnpm --dir tools/emception toolchain doctor
pnpm --dir tools/emception toolchain build all
pnpm --dir tools/emception toolchain release
```

The initial toolchain build is large. Package-only work can use:

```bash
pnpm --dir tools/emception run test:packages
pnpm --dir tools/emception run typecheck:packages
pnpm --dir tools/emception run build:packages
```

### Graphics capability notes

SDL3 is supplied by the version-pinned Emscripten port, which upstream still
classifies as experimental. The build reports that status once and suppresses
the port driver's repeated experimental diagnostic.

The Allegro WebAssembly recipe builds the SDL/OpenGL ES core plus the image,
font, audio, acodec, color, primitives, and memfile addons. Optional native
dependencies that are not shipped in the browser sysroot are disabled
explicitly: FreeImage, PNG, JPEG, WebP, FreeType/TTF, OpenAL/OpenSL, FLAC,
DUMB, OpenMPT, Vorbis, Opus, and MP3. CMake's `NO` capability summary for
these libraries is expected and is not a host prerequisite failure.

## Version ownership

`toolchain/toolchain.config.json` contains policy: Browser ABI, allowed release
channels, CMake constraints, EMSDK component grouping, and overlay ownership.

`toolchain/toolchain.lock.json` contains resolved versions only: immutable
archive URL or commit, SHA-256, EMSDK revision, and workspace content hash.
Builds never search for a latest release and do not accept per-tool version
environment overrides.

Useful maintenance commands:

```bash
pnpm --dir tools/emception toolchain outdated all
pnpm --dir tools/emception toolchain update cmake latest --dry-run
pnpm --dir tools/emception toolchain update cmake latest --verify
pnpm --dir tools/emception toolchain update emsdk latest --verify
```

An EMSDK update resolves LLVM, Binaryen, Python, and SDL together. `--verify`
writes a candidate lock under the disposable cache, builds the affected recipe
graph, applies patches, and replaces the tracked lock atomically only after the
verification succeeds.

## Directory ownership

```text
toolchain/
├── toolchain.config.json       # tracked policy
├── toolchain.lock.json         # tracked immutable sources
└── overlays/                   # tracked patches and project-owned code

.cache/toolchain/               # disposable downloads, sources and builds
artifacts/toolchain/            # canonical tools, sysroot, receipts and release
packages/toolchain/cdn/         # npm staging only
packages/core/cdn/              # deprecated compatibility copy
public/cdn/                     # demos and GitHub Pages only
```

Use scoped cleanup instead of deleting directories by hand:

```bash
pnpm --dir tools/emception toolchain clean artifacts
pnpm --dir tools/emception toolchain clean cache
pnpm --dir tools/emception toolchain clean all
```

These commands do not remove `node_modules`, tracked overlays, or external
checkouts outside the Toolchain cache.

## Canonical build and release pipeline

| Phase | Scripts | Output / invariant |
| --- | --- | --- |
| Resolve | lockfile and source manager | checksum-verified sources in the disposable cache |
| Build | named recipes | tools and mutable sysroot under `artifacts/toolchain` |
| Receipt | receipt engine | lock, recipe, overlay, dependency, command, version, and output hashes |
| Freeze | staging recipe | immutable sysroot snapshot with content fingerprint |
| Patch | glue recipe | versioned patches applied only to staged generated glue |
| Manifest | manifest recipe | schema-v2 metadata populated from actual receipts |
| Bundle | bundle recipe | deterministic Brotli archives and hashes |
| Stage | release recipe | npm Toolchain, Core compatibility, and public CDN copies |

`toolchain build` reuses an output only when its receipt identity matches and
every output hash is intact. `toolchain release` validates those dependencies,
then always regenerates the staged snapshot, glue, manifest, bundles, and
publish directories.

## Generated glue policy

Emscripten glue is generated code and may change when the SDK changes. All
required adaptations are centralized in `scripts/lib/glue-patches.mjs` and
executed by `patch:glue`.

The patcher verifies known source markers and matching `.mjs`/`.wasm` pairs. A
missing pair or an unknown generated shape fails the build instead of producing
a release that the IDE would need to repair at runtime.

## Manifest v2 and ABI profiles

The manifest records:

- package artifact version and complete build fingerprint;
- Browser runtime ABI and glue patch-set version;
- versions detected by the recipes, lock hash, receipt hash, and source
  provenance;
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
independently generated release. Both npm copies are staged byte-for-byte from
the same canonical artifact release.

## Versioning and publication

The seven public packages form one Changesets fixed group. An implementation
PR carries an explicit changeset; after it is merged, the Linux workflow opens
a version PR. That PR is rebuilt using the final version. Once merged, the
workflow packs every package, publishes `@gameguild/emception-toolchain`, waits
for the registry, publishes consumers in dependency order, and creates
`emception-vX.Y.Z`.

The release gate requires package versions, `manifest.artifactVersion`, and the
version embedded in the Browser default manifest URL to be identical.
