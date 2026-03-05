# Browser C/C++ Toolchain — Emscripten in WebAssembly

A complete C/C++ development environment that runs entirely in the browser. Write, compile, and execute C/C++ code directly in a web interface — no local toolchain installation required.

## Project Goals

- **Browser-based compilation**: Full Emscripten toolchain (clang, lld, wasm-opt, emcc) running as WebAssembly
- **Micro-kernel architecture**: Each tool runs as an isolated WASM process with its own memory; the TypeScript kernel provides VFS, IPC, and process management
- **Layered virtual filesystem**: Persistent user files (IndexedDB), CDN-backed system files (lazy fetch), and in-memory scratch space — managed by the kernel, shared across processes via syscalls
- **Interactive terminal**: xterm.js-based shell with stdin/stdout/stderr piped through the kernel

## Key Technologies

| Layer | Technology |
|-------|-----------|
| Compiler backend | LLVM + Clang (compiled to WASM) |
| Optimiser | Binaryen (compiled to WASM) |
| Emscripten driver | CPython (compiled to WASM) running `emcc.py` |
| Build system | Emscripten SDK (latest — em++ / emcc) |
| Kernel | TypeScript (process manager, VFS, IPC, scheduler) |
| Web frontend | Next.js 15 + React |
| Terminal | xterm.js |
| Testing | Playwright (E2E) |

---

## Quick Start

### Prerequisites

- Node.js (Latest LTS or current)
- CMake (3.20+)
- Python 3 (host Python for Emscripten SDK — separate from the WASM CPython)
- Ninja (optional, faster native builds)
- curl (for downloading source tarballs)

### Build & Run

```bash
cd tools/emception
npm install
npm run build:all   # full toolchain build (~30 min first time)
npm run web:dev     # start dev server (default http://localhost:3000)
```

---

## Build Pipeline

`npm run build:all` runs sequential steps via `run-s`:

| # | Script | What it does |
|---|--------|-------------|
| 1 | `build:orchestrator` | TypeScript type-check (`tsc --noEmit`) |
| 2 | `build:emsdk` | Downloads & configures the Emscripten SDK |
| 3 | `build:binaryen` | Builds each Binaryen tool as a standalone WASM process (wasm-opt, wasm-as, …) |
| 4 | `build:cpython` | Cross-compiles CPython as a standalone WASM process |
| 5 | `build:llvm` | Builds each LLVM tool as a standalone WASM process (clang, lld, llvm-nm, …) |
| 6 | `build:sysroot` | Populates `/usr/include`, `/usr/lib` with headers, libs, and Emscripten runtime files |
| 7 | `build:manifest` | Generates a file manifest for the CDN + optional Brotli compression |
| 8 | `deploy:cdn` | Copies compressed assets to `web/public/cdn/` for serving |

Individual steps can be run independently (e.g. `npm run build:llvm`).

All build scripts are **TypeScript** (in `scripts/`), executed via **tsx**, for cross-platform compatibility.

### Build Flags (Tool Processes)

Each tool is compiled as a **standalone** Emscripten module — no MAIN_MODULE/SIDE_MODULE, no dlopen. Standard Emscripten build:

```
em++  -sALLOW_MEMORY_GROWTH=1  -sMAXIMUM_MEMORY=2147483648
      -sFORCE_FILESYSTEM=1     -sMODULARIZE=1
      -sEXPORT_ES6=1           -sEXIT_RUNTIME=1
      -sINVOKE_RUN=0           -sEXPORTED_FUNCTIONS=_main
      -sEXPORTED_RUNTIME_METHODS=FS,callMain
```

**Asyncify is intentionally excluded** — it is incompatible with Emscripten's default reference-types feature. Tools don't need async unwinding for simple `callMain()` invocations.

Additional per-tool flags:

| Tool | Extra Flags |
|------|-------------|
| Binaryen | `-sSTACK_SIZE=4194304` (4 MB — deep AST recursion) |
| LLVM | `-sSTACK_SIZE=8388608` (8 MB — deep parsing recursion), `-sUSE_ZLIB=1` |
| CPython | `-sSTACK_SIZE=2097152` (2 MB — import chain), `-sUSE_ZLIB=1`, `-sUSE_BZIP2=1`, `-sUSE_SQLITE3=1` |

Each tool statically links what it needs (libc, libc++, LLVM libs, etc.) and gets its own isolated WASM linear memory.

---

## Architecture Overview

### Micro-Kernel Design

The architecture follows a **micro-kernel** pattern inspired by operating system design. The "kernel" is a TypeScript layer that manages processes, the virtual filesystem, and inter-process communication. Each tool (clang, lld, wasm-opt, python, etc.) runs as an **isolated WASM process** with its own linear memory — there is no shared memory between tools.

This eliminates the entire class of bugs caused by the previous MAIN_MODULE/SIDE_MODULE dynamic linking model (BSS corruption, environ loss, symbol conflicts, memory pressure from cohabiting 2GB address space).

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Browser Tab                                                                │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Kernel  (TypeScript — orchestrator/)                                 │  │
│  │                                                                       │  │
│  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐      │  │
│  │  │  Process   │  │    VFS     │  │    IPC     │  │    TTY     │      │  │
│  │  │  Manager   │  │  (layered) │  │ (message   │  │  (xterm)   │      │  │
│  │  │            │  │            │  │  passing)  │  │            │      │  │
│  │  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘      │  │
│  │        └───────────────┴───────────────┴───────────────┘              │  │
│  │                             │                                         │  │
│  │            Syscall Interface (postMessage / SharedArrayBuffer)         │  │
│  │                             │                                         │  │
│  └─────────────────────────────┼─────────────────────────────────────────┘  │
│                                │                                            │
│  ┌─────────────────────────────┴─────────────────────────────────────────┐  │
│  │  Isolated WASM Processes  (each has its own linear memory)            │  │
│  │                                                                       │  │
│  │  ┌───────────┐ ┌───────────┐ ┌───────────┐ ┌───────────┐            │  │
│  │  │  clang    │ │   lld     │ │ wasm-opt  │ │  python   │  …         │  │
│  │  │  .wasm    │ │  .wasm    │ │  .wasm    │ │  .wasm    │            │  │
│  │  │           │ │           │ │           │ │           │            │  │
│  │  │ own libc  │ │ own libc  │ │ own libc  │ │ own libc  │            │  │
│  │  │ own LLVM  │ │ own LLVM  │ │ own byn   │ │ own pylib │            │  │
│  │  │ own heap  │ │ own heap  │ │ own heap  │ │ own heap  │            │  │
│  │  │ own FS    │ │ own FS    │ │ own FS    │ │ own FS    │            │  │
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────┘            │  │
│  │                                                                       │  │
│  │  Each process sees the same filesystem view via kernel-mediated       │  │
│  │  syscalls — reads/writes go through the kernel VFS, not shared mem.   │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │  Virtual Filesystem  (kernel-managed, layered, unified view)          │  │
│  │                                                                       │  │
│  │  /usr/lib/       → LazyFS  (WASM binaries, libs — CDN-backed)        │  │
│  │  /usr/bin/       → LazyFS  (emcc, em++, clang wrappers)              │  │
│  │  /usr/include/   → LazyFS  (C/C++ system headers)                    │  │
│  │  /usr/lib/python3.14/ → LazyFS  (stdlib zip + init files)            │  │
│  │  /home/user/     → IDBFS   (persistent user files — IndexedDB)       │  │
│  │  /tmp/           → IDBFS   (volatile — in-memory only, no IDB)       │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Why Micro-Kernel Instead of Dynamic Linking?

The previous architecture used Emscripten's `MAIN_MODULE` + `SIDE_MODULE` dynamic linking — one shared WASM runtime with tools loaded via `dlopen`/`dlsym` into shared linear memory. This caused systemic issues:

| Problem | Cause | Micro-kernel fix |
|---------|-------|-----------------|
| **BSS corruption** | SIDE_MODULE loading zeros BSS segments overlapping MAIN_MODULE globals (e.g. musl's `__environ`) | Each process has its own BSS — no overlap possible |
| **Environment variable loss** | Loading side modules after `callMain()` corrupts the environ table in shared memory | Each process has its own environ, set by kernel before `main()` |
| **Symbol conflicts** | All modules share one symbol table; name collisions cause silent corruption | Each process has its own symbol table — fully isolated |
| **Memory pressure** | LLVM (~30MB) + Binaryen (~10MB) + CPython coexist in a single 2GB space | Each process gets its own 2GB address space |
| **No standard builds** | MAIN_MODULE/SIDE_MODULE require non-standard Emscripten flags and manual patches | Standard `emcc` builds — no patches needed |
| **Fragile initialization** | Tools must be loaded in specific order; workarounds needed (setenv after dlopen, ENV injection patches) | Each tool initializes independently with its own clean state |

### Kernel Components

The TypeScript kernel (`orchestrator/`) provides OS-like services to WASM processes:

| Component | File(s) | Responsibility |
|-----------|---------|---------------|
| **Process Manager** | `tool-runner.ts` | Spawns WASM processes, manages lifecycle, captures exit codes |
| **VFS** | `vfs/` | Layered filesystem (LazyFS, IDBFS, OverlayFS) — single source of truth |
| **IPC** | `async-bridge.ts` | Message passing between kernel and WASM processes (syscall dispatch) |
| **Shell** | `shell.ts` | Command parser, pipeline support, process spawning |
| **TTY** | `tty/xterm-bridge.ts`, `tty/line-buffer.ts` | xterm.js integration, stdin/stdout/stderr routing, line buffering |
| **Network** | `net/fetch-bridge.ts`, `net/cors-proxy.ts`, `net/git-tarball.ts` | fetch-based network access, CORS proxy, Git tarball download |
| **Loader** | `loader/wasm-module.ts`, `loader/brotli.ts` | WASM binary fetching, Brotli decompression, caching |
| **Emscripten Bridge** | `emscripten/browser-bridge.ts`, `emscripten/subprocess-shim.ts` | Emscripten module patching, subprocess IPC shim for CPython |

### Tool Processes

Each tool is a standalone WASM module. The kernel knows how to spawn them via a **TOOL_REGISTRY**:

| Tool | WASM Binary | Statically Links |
|------|-------------|-----------------|
| `clang`, `clang++` | clang.wasm | libc, libc++, LLVM |
| `lld`, `wasm-ld` | lld.wasm | libc, libc++, LLVM |
| `llvm-nm` | llvm-nm.wasm | libc, libc++, LLVM |
| `llvm-ar` | llvm-ar.wasm | libc, libc++, LLVM |
| `llvm-objcopy` | llvm-objcopy.wasm | libc, libc++, LLVM |
| `llc` | llc.wasm | libc, libc++, LLVM |
| `wasm-opt` | wasm-opt.wasm | libc, libc++, Binaryen |
| `wasm-as` | wasm-as.wasm | libc, libc++, Binaryen |
| `wasm-ctor-eval` | wasm-ctor-eval.wasm | libc, libc++, Binaryen |
| `wasm-emscripten-finalize` | wasm-emscripten-finalize.wasm | libc, libc++, Binaryen |
| `wasm-metadce` | wasm-metadce.wasm | libc, libc++, Binaryen |
| `emcc`, `em++` | python.wasm | libc, libc++, libpython |

### Virtual Filesystem Layers

The VFS is owned by the kernel and exposed to processes via syscalls. It is composed of four pluggable layers (see `orchestrator/vfs/`):

| Layer | File | Purpose |
|-------|------|---------|
| **LazyFS** | `lazy.ts` | Fetches files from the CDN on first access; backed by a manifest of available paths |
| **IDBFS** | `idb.ts` | IndexedDB-backed filesystem with optional volatile (in-memory only) mode |
| **OverlayFS** | `overlay.ts` | Composes layers: writes go to IDBFS write-layer, reads fall through LazyFS |

Files are served via Brotli-compressed assets on the CDN and decompressed on the client using the loader (`orchestrator/loader/brotli.ts`).

### Process Lifecycle

1. **User types command** (e.g. `clang -o hello hello.c`) → shell parses it
2. **Kernel looks up** the tool in the TOOL_REGISTRY → resolves to `clang.wasm`
3. **Kernel fetches** the WASM binary from CDN (Brotli-compressed, cached after first load)
4. **Kernel spawns** a new WASM instance with its own memory, ENV, argc/argv
5. **Process filesystem** is connected to the kernel VFS via syscall bridge — process sees the same `/usr/include`, `/home/user`, `/tmp` as every other process
6. **Process runs** `main(argc, argv)` → reads/writes files via kernel-mediated syscalls
7. **Process exits** → kernel captures exit code, stdout, stderr; memory is reclaimed
8. **Shell continues** with the next command in the pipeline

### Subprocess Dispatch (emcc → clang/lld/wasm-opt)

When `emcc` (CPython running `emcc.py`) needs to invoke sub-tools (clang, lld, wasm-opt), it cannot use POSIX `subprocess.Popen` — there are no native processes in the browser. Instead, a **subprocess shim** (`orchestrator/emscripten/subprocess_shim.py`) replaces Python's `subprocess` module at runtime:

1. **emcc calls `subprocess.run(['clang', '-o', 'hello.o', 'hello.c'])`**
2. **Shim intercepts** → serializes the command as JSON to `/tmp/.subprocess_request`
3. **Shim calls `os.system('__dispatch_subprocess')`** → triggers JSPI suspension
4. **Kernel reads** the JSON request from the VFS
5. **Kernel spawns** the requested tool (clang.wasm) as a new isolated process
6. **Tool runs**, writes output files to VFS, exits
7. **Kernel writes** stdout/stderr to `/tmp/.subprocess_stdout` and `/tmp/.subprocess_stderr`
8. **JSPI resumes** CPython → shim reads results from VFS → returns to emcc

This IPC mechanism allows the single-threaded browser environment to run multi-process compilation pipelines synchronously from Python's perspective.

### Comparison to OS Design

| OS Concept | Emception Equivalent |
|-----------|---------------------|
| Kernel | TypeScript orchestrator |
| Process | Isolated WASM instance |
| Syscalls | `postMessage` / `SharedArrayBuffer` bridge |
| `/proc`, `/dev` | Kernel-managed VFS layers |
| `fork`/`exec` | Kernel spawns new WASM instance from binary |
| Pipes | Kernel routes stdout of one process to stdin of the next |
| Filesystem | LazyFS + IDBFS + OverlayFS stack |
| Shared libraries | Not needed — each process statically links its dependencies |
| Virtual memory | Each WASM instance has its own linear memory (up to 2GB) |

---

## Web Frontend

The web interface (`web/`) is a **Next.js** application providing:

- **Monaco-based code editor** for C/C++ source files
- **xterm.js terminal** connected to the kernel shell
- **File browser** backed by the VFS
- **E2E tests** via Playwright (`web/e2e/compile.spec.ts`)

```bash
npm run web:dev    # development server
npm run web:build  # production build
npm run web:start  # serve production build
```

---

## AI Instructions for tools/emception

When working in this directory, follow these rules. **Do not take shortcuts.**

## No Shortcuts or Placeholders

- **No stubs.** Do not add stub functions, empty implementations, or `TODO` placeholders that defer real work. Implement the full behavior.
- **No bypasses.** Do not bypass, skip, or work around failing functionality. Fix the root cause.
- **No fake/dummy implementations.** Do not add dummy values, mock returns, or no-op implementations to satisfy interfaces. Implement the real logic.
- **No placeholder data.** Do not use placeholder strings, fake IDs, or synthetic data to get something "working." Use real data or proper configuration.
- **Do not skip jobs or steps.** Do not skip or bypass any build steps or jobs. Fix the underlying issue.
- **Do not ignore errors.** Do not ignore build errors or warnings. Address them promptly.
- **No errors should go unoticed or unfixed.** If something is broken, fix it. Do not let errors linger.
- **Do not modify code on build folders or git ignored files.** All changes must be in the source files and in the automated build scripts. Do not rely on manual patching or one-off fixes.

## Tests

- **Do not remove tests** to make a test suite pass. Fix the implementation so the tests pass.
- **Do not disable or skip tests** (e.g., `it.skip`, `xit`, `@Disabled`) to avoid failures. Fix the code under test.
- **Do not relax assertions** or weaken test expectations to get green. Strengthen the implementation instead.
- **E2E timeouts**: Give around 5m for the timeout for the E2E tests.

## Quality

- **Fix the root cause.** When something fails, diagnose why and fix it there. Do not paper over symptoms.
- **Preserve intended behavior.** Changes must not reduce functionality, hide errors, or mask bugs.
- **Run the full workflow.** Prefer running the real build/test scripts over abbreviated or "quick" paths when validating changes.
- **Use the latest version possible** do not hardcode versions or dependencies. Create ways to get the latest compatible versions if possible. Ex.: for the python, it should be the same as the latest emsdk uses.
- **Do not use temporary files or manual steps** to get something working. Implement the proper automated solution.
- **All patches should be applied at build time.** Do not rely on manual patching or one-off fixes. Implement build-time patches if necessary. Or post-build patches that run automatically after compilation.

## If Unsure 
- **If a proper fix seems complex, implement it anyway. Prefer correct over quick.**
- **If blocked, explain the blocker and propose a concrete path forward rather than inserting a workaround.**

---

## Project Structure

```
tools/emception/
├── scripts/                  # Build scripts (TypeScript, run via tsx)
│   ├── setup-emsdk.ts        #   Download & configure Emscripten SDK
│   ├── build-llvm.ts         #   Compile LLVM tools as standalone WASM processes
│   ├── build-binaryen.ts     #   Compile Binaryen tools as standalone WASM processes
│   ├── build-cpython.ts      #   Cross-compile CPython as standalone WASM process
│   ├── populate-sysroot.ts   #   Assemble /usr/include + /usr/lib
│   ├── generate-manifest.ts  #   File manifest + Brotli compression
│   ├── deploy-cdn.ts         #   Copy assets to web/public/cdn/
│   ├── compress-cdn.ts       #   Brotli compress CDN files
│   ├── deploy-cpython.ts     #   Deploy CPython stdlib
│   ├── clean.ts              #   Remove build artifacts
│   ├── strip-subprocess.py   #   Strip subprocess module from CPython stdlib
│   └── lib/                  #   Shared utilities for build scripts
│       └── emsdk.ts          #     EMSDK setup helper (PATH, env vars)
│
├── orchestrator/             # TypeScript kernel
│   ├── tool-runner.ts        #   Process manager — TOOL_REGISTRY, spawn, lifecycle
│   ├── shell.ts              #   Shell command parser and pipeline dispatcher
│   ├── async-bridge.ts       #   Syscall bridge (kernel ↔ WASM process IPC)
│   ├── index.ts              #   Public API
│   ├── vfs/                  #   Virtual filesystem (kernel-managed)
│   │   ├── lazy.ts           #     LazyFS — CDN-backed on-demand fetch
│   │   ├── idb.ts            #     IDBFS — IndexedDB persistence (+ volatile mode)
│   │   ├── overlay.ts        #     OverlayFS — layer composition
│   │   ├── interface.ts      #     Common VFS interface
│   │   └── index.ts          #     VFS manager
│   ├── loader/               #   WASM binary loading
│   │   ├── wasm-module.ts    #     Module factory loader
│   │   └── brotli.ts         #     Brotli decompression
│   ├── tty/                  #   Terminal I/O (xterm.js integration)
│   │   ├── xterm-bridge.ts   #     xterm.js ↔ kernel bridge
│   │   └── line-buffer.ts    #     Line buffering for stdin
│   ├── net/                  #   Network layer
│   │   ├── cors-proxy.ts     #     CORS proxy for cross-origin fetches
│   │   ├── fetch-bridge.ts   #     Fetch API abstraction
│   │   └── git-tarball.ts    #     Git repository tarball downloader
│   └── emscripten/           #   Emscripten-specific helpers
│       ├── subprocess-shim.ts#     Subprocess shim re-exporter
│       ├── subprocess_shim.py#     Python subprocess replacement for browser IPC
│       ├── browser-bridge.ts #     Browser-specific WASM module patching
│       ├── raw-imports.d.ts  #     TypeScript declarations for .py imports
│       └── index.ts          #     Emscripten helpers public API
│
├── userland/                 #   Source code for WASM-compiled tool processes
│   ├── llvm/                 #     LLVM/Clang/LLD source + build artifacts
│   ├── binaryen/             #     Binaryen source + build artifacts
│   ├── cpython/              #     CPython source + cross-compile artifacts
│   └── busybox/              #     BusyBox (shell utilities)
│
├── sysroot/                  #   Emscripten sysroot (headers, libs, runtime)
├── build/                    #   Built WASM artifacts & CDN files (generated)
├── tools/                    #   Vendored tools
│   └── emsdk/                #     Emscripten SDK (downloaded by build:emsdk)
│
├── web/                      #   Next.js frontend application
│   ├── src/                  #     App source (pages, components, lib)
│   ├── e2e/                  #     Playwright E2E tests
│   │   └── compile.spec.ts   #       Compilation pipeline test
│   ├── public/               #     Static assets + CDN files
│   ├── playwright.config.ts  #     Playwright configuration
│   └── package.json          #     Frontend dependencies
│
├── package.json              #   Build scripts & dependencies
├── tsconfig.json             #   TypeScript configuration
└── README.md                 #   This file
```
