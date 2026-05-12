# Building from source

> **You probably don't need this.** Most consumers install `@gameguild/emception-browser` from npm and use the CDN payload bundled in `emception/cdn/*`. Build from source only when bumping LLVM/Emscripten/Python or hacking on CDN packaging.

## Prerequisites

- Node.js (current LTS)
- CMake ≥ 3.20
- Python 3 (host Python for Emscripten SDK; separate from the WASM CPython)
- Ninja (optional, faster native builds)
- curl

## Quick build

```bash
cd tools/emception
npm install
npm run build:all   # ~30 min on first run
```

Then run a demo:

```bash
cd apps/ide-react && npm install && npm run dev   # Vite/React (http://localhost:5173)
cd apps/ide-next  && npm install && npm run dev   # Next.js   (http://localhost:3000)
```

The demos sync CDN assets from the built CDN payload via a `predev`/`prebuild` hook.

## Pipeline (`build:all`)

`run-s` runs these in order:

| #   | Script               | Output                                                                     |
| --- | -------------------- | -------------------------------------------------------------------------- |
| 1   | `typecheck`          | `tsc --noEmit`                                                             |
| 2   | `build:emsdk`        | Downloads/configures the Emscripten SDK                                    |
| 3   | `build:binaryen`     | `wasm-opt`, `wasm-as`, `wasm-ctor-eval`, `wasm-metadce`                    |
| 4   | `build:cpython`      | Cross-compiled standalone CPython WASM                                     |
| 5   | `build:llvm`         | `clang`, `lld`, `llvm-nm`, `llvm-ar`, `llvm-objcopy`, `llc`                |
| 6   | `build:libcurl-lite` | Minimal `libcurl.a` for tools that need HTTP                               |
| 7   | `build:ninja`        | `ninja.wasm`                                                               |
| 8   | `build:cmake`        | `cmake.wasm`                                                               |
| 9   | `build:sdl3`         | `libSDL3.a` + `sdl3-runtime.mjs`                                           |
| 10  | `build:raylib`       | `libraylib.a` + companions + `raylib-runtime.mjs` (USE_GLFW=3, WebGL2)     |
| 11  | `build:imgui`        | `libimgui.a`                                                               |
| 12  | `build:sysroot`      | Populates `/usr/{include,lib,bin}` + emscripten runtime                    |
| 13  | `build:brotli`       | Native Brotli CLI (build-time) + WASM decoder (runtime)                    |
| 14  | `patch:glue`         | Patches Emscripten `.mjs` glue for VFS + async bridge                      |
| 15  | `build:manifest`     | `manifest.json` + raw CDN staging                                          |
| 16  | `build:bundles`      | Brotli-compressed `.tar.br` bundles (≈ 29 files, ≈ 182 MB)                 |
| 17  | `build:lib`          | Publishable library (`tsup` + `tsc -p tsconfig.lib.json`)                  |
| 18  | `deploy:cdn`         | Copies CDN to `apps/ide-react/public/cdn/` and `apps/ide-next/public/cdn/` |

Convenience aggregates:

- `build:cdn` = `build:manifest build:bundles deploy:cdn`
- `build:pipeline` = `build:sysroot build:brotli patch:glue build:cdn build:lib`

Individual steps run on their own (`npm run build:llvm`, etc.). All scripts are TypeScript in `scripts/` executed via `tsx`.

## Versioning

LLVM and Python versions **are not hardcoded**. They are detected from the active Emscripten SDK after `build:emsdk` and propagated to downstream steps. Updating emsdk automatically propagates everywhere — no manual config edits.

## Tool build flags

Each tool is built standalone (no `MAIN_MODULE` / `SIDE_MODULE`):

```
em++  -sALLOW_MEMORY_GROWTH=1  -sMAXIMUM_MEMORY=2147483648
      -sFORCE_FILESYSTEM=1     -sMODULARIZE=1
      -sEXPORT_ES6=1           -sEXIT_RUNTIME=1
      -sINVOKE_RUN=0           -sEXPORTED_FUNCTIONS=_main
      -sEXPORTED_RUNTIME_METHODS=FS,callMain
      -sASYNCIFY               -sASYNCIFY_STACK_SIZE=…
      -sASYNCIFY_IMPORTS=[…]   -mno-reference-types
```

Per-tool stack sizes:

| Tool     | Extra flags                                                                       |
| -------- | --------------------------------------------------------------------------------- |
| Binaryen | `-sSTACK_SIZE=4194304` (4 MB)                                                     |
| LLVM     | `-sSTACK_SIZE=8388608` (8 MB), `-sUSE_ZLIB=1`                                     |
| CPython  | `-sSTACK_SIZE=2097152` (2 MB), `-sUSE_ZLIB=1`, `-sUSE_BZIP2=1`, `-sUSE_SQLITE3=1` |

## Bundle classification

`generate-bundles.ts` assigns every sysroot file to exactly one `.tar.br` via a 5-pass classifier:

| Pass | Strategy         | Output                                                                               |
| ---- | ---------------- | ------------------------------------------------------------------------------------ |
| 1    | Tool pairs       | 6 bundles: `clang`, `lld`, `python`, `wasm-opt`, `ninja`, `cmake`                    |
| 2a   | Cache classifier | 8 sub-bundles under `cache-lib/`                                                     |
| 2b   | SDL3 pre-assign  | `sdl3.tar.br`                                                                        |
| 2c   | Prefix groups    | 11 bundles (`raylib`, `imgui`, `clang-headers`, `usr-include`, `emscripten-core`, …) |
| 3    | Catch-all        | `usr-lib-misc.tar.br`                                                                |

`clang-headers` (`/usr/lib/clang/<ver>/include`) ships separately so the resource-dir headers can load independently of `clang.wasm`. `populate-sysroot.ts` auto-detects the active LLVM version under `tools/emsdk/upstream/lib/clang/<ver>/include`.
