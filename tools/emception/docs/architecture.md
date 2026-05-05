# Architecture

Emception is a **micro-kernel** that runs an Emscripten C/C++ toolchain entirely in the browser. A TypeScript kernel manages processes, the virtual filesystem, and IPC. Each tool (clang, lld, wasm-opt, python, …) runs as an **isolated WASM process** with its own 2 GB linear memory.

## Why a micro-kernel (not dynamic linking)?

The previous design used Emscripten `MAIN_MODULE` + `SIDE_MODULE`. That caused systemic issues:

| Problem             | Cause                                            | Micro-kernel fix                      |
| ------------------- | ------------------------------------------------ | ------------------------------------- |
| BSS corruption      | SIDE_MODULE zeroes overlap MAIN_MODULE globals   | Each process has its own BSS          |
| Environment loss    | `dlopen` after `callMain` corrupts `__environ`   | Each process has its own environ      |
| Symbol conflicts    | Single shared symbol table across all tools      | Each process has its own symbol table |
| Memory pressure     | LLVM + Binaryen + CPython sharing one 2 GB space | Each process gets its own 2 GB space  |
| Non-standard builds | Required custom Emscripten patches               | Standard `emcc` builds, no patches    |

## High-level layout

```
┌──────────────────────────────────────────────────────────────────┐
│  Browser tab                                                      │
│  ┌────────────────────────────────────────────────────────────┐   │
│  │  Kernel (TypeScript — packages/core)                       │   │
│  │  ┌──────────┐ ┌──────┐ ┌──────┐ ┌──────┐                   │   │
│  │  │ Process  │ │ VFS  │ │ IPC  │ │ TTY  │                   │   │
│  │  │ Manager  │ │      │ │      │ │      │                   │   │
│  │  └──────────┘ └──────┘ └──────┘ └──────┘                   │   │
│  │           Syscall bridge (postMessage / SAB)               │   │
│  └─────────────────────────┬──────────────────────────────────┘   │
│                            │                                       │
│  ┌─────────────────────────┴──────────────────────────────────┐   │
│  │  Isolated WASM processes (own memory each)                 │   │
│  │  clang.wasm  lld.wasm  wasm-opt.wasm  python.wasm  …       │   │
│  └────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

## Kernel components

| Component         | Location                            | Responsibility                                |
| ----------------- | ----------------------------------- | --------------------------------------------- |
| Process Manager   | `packages/core/src/tool-runner.ts`  | Spawn WASM, lifecycle, exit codes             |
| VFS               | `packages/core/src/vfs/`            | Layered filesystem (LazyFS, IDBFS, OverlayFS) |
| IPC               | `packages/core/src/async-bridge.ts` | Syscall dispatch                              |
| Shell             | `packages/core/src/shell.ts`        | Command parsing, pipelines                    |
| TTY               | `packages/core/src/tty/`            | xterm bridge, line buffering                  |
| Loader            | `packages/core/src/loader/`         | WASM fetch, Brotli decompression              |
| Emscripten bridge | `packages/core/src/emscripten/`     | Glue patching, subprocess shim                |

## Tool registry

Each tool is a standalone Emscripten module. The kernel resolves command names via a `TOOL_REGISTRY`:

| Command(s)                                              | WASM binary                     | Statically links        |
| ------------------------------------------------------- | ------------------------------- | ----------------------- |
| `clang`, `clang++`                                      | `clang.wasm`                    | libc, libc++, LLVM      |
| `lld`, `wasm-ld`                                        | `lld.wasm`                      | libc, libc++, LLVM      |
| `llvm-nm`, `llvm-ar`, `llvm-objcopy`, `llc`             | `llvm-*.wasm`                   | libc, libc++, LLVM      |
| `wasm-opt`, `wasm-as`, `wasm-ctor-eval`, `wasm-metadce` | `wasm-*.wasm`                   | libc, libc++, Binaryen  |
| `wasm-emscripten-finalize`                              | `wasm-emscripten-finalize.wasm` | libc, libc++, Binaryen  |
| `emcc`, `em++`                                          | `python.wasm`                   | libc, libc++, libpython |

### Browser cc1 mode

`clang.wasm` cannot fork a `cc1` subprocess in the browser (no `posix_spawn`). The driver mode silently exits 0 in ~35 ms. `cc1_main` is statically linked, so the IDE invokes it directly:

```
clang -cc1 -triple wasm32-unknown-emscripten \
      -resource-dir /usr/lib/clang/<ver> \
      -internal-isystem /usr/lib/clang/<ver>/include \
      -internal-isystem /usr/include/c++/v1 \
      -internal-isystem /usr/include \
      -fcxx-exceptions -fexceptions ...
```

See `packages/ide/src/components/Ide.tsx` for the canonical browser cc1 argv.

## Process lifecycle

1. Shell parses `clang -o hello hello.c` and looks up `clang` in `TOOL_REGISTRY`.
2. Kernel fetches `clang.wasm` (Brotli, cached).
3. Kernel instantiates a fresh WASM with its own memory, ENV, argv.
4. Process syscalls are routed to the kernel VFS.
5. Process exits → kernel captures exit code + streams.

## Subprocess dispatch (`emcc` → `clang`/`lld`)

`emcc` is CPython running `emcc.py`. It cannot use `subprocess.Popen`. A Python shim (`packages/core/src/emscripten/subprocess_shim.py`) replaces `subprocess` at runtime:

1. `emcc` calls `subprocess.run(['clang', ...])`.
2. Shim writes the request as JSON to `/tmp/.subprocess_request`.
3. Shim calls `os.system('__dispatch_subprocess')` → Asyncify suspends.
4. Kernel reads the request, spawns `clang.wasm` with stdin/stdout/stderr callbacks bridged to the parent's buffers.
5. Tool exits, kernel writes exit code to VFS.
6. Asyncify resumes CPython → shim returns to `emcc`.

## Graphics runtimes (`*-runtime.mjs`)

The build emits two MODULARIZE Emscripten factories patched at runtime in `packages/ide/src/components/Ide.tsx`:

- `sdl3-runtime.mjs` — links `libSDL3.a`, exposes SDL3 GL bindings + RAF main loop.
- `raylib-runtime.mjs` (~197 KB) — built with `-sUSE_GLFW=3 -sMAX_WEBGL_VERSION=2 -sMIN_WEBGL_VERSION=2`; raylib itself uses `-DGRAPHICS=GRAPHICS_API_OPENGL_ES3` so VAOs are core WebGL2.

The IDE patches minified `callUserCallback` and `handleException` to swallow `WebAssembly.RuntimeError` (memory-out-of-bounds) and call `Module.pauseMainLoop()` so a guest crash does not kill the Chromium tab.

## OS analogy

| OS concept       | Emception equivalent                                  |
| ---------------- | ----------------------------------------------------- |
| Kernel           | TypeScript orchestrator                               |
| Process          | Isolated WASM instance                                |
| Syscalls         | `postMessage` / `SharedArrayBuffer` bridge            |
| `fork` / `exec`  | Kernel spawns new WASM instance                       |
| Pipes            | Kernel routes stdout → next stdin                     |
| Shared libraries | None — each process statically links its dependencies |
| Virtual memory   | Each WASM instance has its own 2 GB linear memory     |

See [`vfs.md`](./vfs.md) for the filesystem stack and Asyncify-based async I/O, and [`build.md`](./build.md) for how the WASM binaries are produced.
