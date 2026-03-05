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

### Version Compatibility: LLVM & Python from Emsdk

**Critical**: The LLVM and Python versions must not be hardcoded. They must be **determined dynamically from the Emscripten SDK configuration** during the build process. This ensures full compatibility with the toolchain that will compile and run the C/C++ code.

- **LLVM version**: Detected from `emsdk` after `build:emsdk` step (step 2). The build scripts query the SDK for the active LLVM version and use that exact version when building all LLVM tools as WASM processes.
- **Python version**: Detected from `emsdk` after `build:emsdk` step. The build scripts read the SDK's Python version, download/cross-compile that exact CPython version as a WASM process, and configure the VFS with the matching Python stdlib (e.g. `/usr/lib/python3.14/` if emsdk uses 3.14).

This approach guarantees that:
- The browser-based toolchain is compatible with the emcc that drives the compilation
- No version mismatches occur between LLVM, Python, Binaryen, and Emscripten
- Updates to emsdk automatically propagate to the browser toolchain without manual configuration changes

Build scripts should read version information from `$EMSDK_PATH/.emsdk_` cache or the SDK's version.txt and expose these as environment variables to downstream steps.

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
│  │  └───────────┘ └───────────┘ └───────────┘ └───────────┘            │  │
│  │                    ↓ VFS via syscalls (shared)                       │  │
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
│  │  /usr/lib/python*/  → LazyFS  (stdlib zip + init files, version from emsdk) │  │
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

### Virtual Filesystem (VFS) Architecture

The VFS is a critical substrate: it is **injected into every WASM process** and **hijacks all filesystem calls**. The VFS is owned by the kernel and exposed to processes via a synchronous syscall bridge. It is composed of three layered backend implementations (see `orchestrator/vfs/`):

#### VFS Injection & Syscall Hijacking

- **Filesystem hijacking**: All POSIX filesystem calls (`open`, `read`, `write`, `stat`, etc.) are intercepted by a custom Emscripten FS implementation before being passed to the kernel.
- **Stdin/Stdout/Stderr hijacking**: The standard I/O streams are also hijacked. Instead of direct browser console output, `stdin`, `stdout`, and `stderr` receive **callback functions** that read/write data through the kernel, allowing the shell to capture and route process output.
- **Unified view**: All processes see the exact same filesystem tree via the kernel VFS — files written by one process are immediately visible to the next.

#### VFS Layer Stack

The VFS is layered to provide different storage semantics:

| Mount Point | Backend Layer | Behavior | Implementation |
|---|---|---|---|
| **`/tmp`** | **MemFS** | In-memory filesystem. Volatile (cleared on reload). Non-persistent. Used for temporary compilations, subprocess communication, intermediate files. | `mem.ts` — simple in-memory data structure (no IndexedDB) |
| **`/home/user`** | **IDBFS** | IndexedDB-backed persistent filesystem. User project files, source code, configuration files. Survives page reload. | `idb.ts` — reads/writes to browser IndexedDB |
| **`/usr`, `/lib`, `/etc`** | **LazyFS** | CDN-backed lazy filesystem. System files, headers, libraries, binaries. Files are **downloaded on-demand as Brotli-compressed bundles** and unpacked into IndexedDB. Bundles are cached — only downloaded if not already present. | `lazy.ts` — manifest-driven, bundle-based |

#### LazyFS: Bundle-Based Lazy Loading

LazyFS is optimized for large directory trees (e.g. `/usr/include` with thousands of headers, `/usr/lib` with WASM binaries and system libraries):

1. **Manifest**: At build time, the manifest (`build/manifest.json`) describes all available files, their paths, sizes, and which **bundle** they belong to.
   ```json
   {
     "files": {
       "/usr/include/stdio.h": { "bundle": "crt0", "offset": 1024, "size": 512 },
       "/usr/include/stdlib.h": { "bundle": "crt0", "offset": 1536, "size": 256 },
       "/usr/lib/libc.a": { "bundle": "libc", "offset": 0, "size": 1048576 }
     },
     "bundles": {
       "crt0": { "url": "/cdn/crt0.tar.br", "size": 2048 },
       "libc": { "url": "/cdn/libc.tar.br", "size": 2097152 }
     }
   }
   ```

2. **On first file access**: When a process tries to open `/usr/include/stdio.h`, LazyFS:
   - Checks the manifest → finds that it belongs to bundle `crt0`
   - Checks if bundle `crt0` is already downloaded and unpacked → if **yes**, return the file immediately
   - If **no**, **fetch the bundle from the CDN** as a Brotli-compressed tarball (`/cdn/crt0.tar.br`)
   - **Decompress** the Brotli archive on the client using `orchestrator/loader/brotli.ts`
   - **Unpack** the tar into IndexedDB under `/usr` (batch writes for performance)
   - **Return the file** to the process

3. **Bundle caching**: After a bundle is downloaded and unpacked, subsequent access to files in that bundle is instant (from IndexedDB). Bundles are versioned in the manifest — if the build changes, a new manifest URL ensures fresh downloads.

4. **Lazy semantics**: Only files that are actually accessed are downloaded. A project compiling a single header does not download all of `/usr/include`.

#### Design Principle: Pure Lazy Loading — NO Preloading or Warming

**The filesystem must be truly lazy — files are downloaded ONLY when accessed, never at startup or in advance.** This is critical:

- **No startup preload**: The browser session starts with zero files downloaded. Bundles only arrive when needed.
- **No filesystem warming**: Do not pre-fetch "likely-to-be-needed" files based on heuristics. The manifest drives access, not guessing.
- **No cache warming**: Do not populate IndexedDB on first load. Only download bundles that processes actually try to open.
- **No anticipatory downloads**: Avoid speculative fetching of related files (e.g., downloading all headers in a directory when one header is accessed).

This ensures:
- **Fast startup**: No blocking network I/O before the user can run code
- **Minimal bandwidth**: Only tools and files actually used are transferred
- **Responsive UI**: The terminal appears immediately; compilation can start while assets stream in
- **Scalability**: Adding more system files doesn't slow down initial load

Exceptions to this principle are only related to `/dev`, `/proc`, and other virtual filesystems that must be initialized at startup for process management — but these do not contain user-accessible files and are not part of the LazyFS design.

- `/tmp` should not be persisted across sessions, so it uses MemFS.
- `/home/user` should be persisted, so it uses IDBFS.
- `/usr`, `/lib` and other system directories should be lazily loaded from the CDN, so they use LazyFS.

#### OverlayFS (Optional Composition)

For scenarios requiring both read-through and write-isolation:

| Layer | Backend | Purpose |
|---|---|---|
| **Write layer** (top) | IDBFS | Captures all write operations |
| **Read layer** (bottom) | LazyFS or IDBFS | Falls through for cache misses |

This allows a process to write temporary files without affecting the shared system files.

### Process Lifecycle

1. **User types command** (e.g. `clang -o hello hello.c`) → shell parses it
2. **Kernel looks up** the tool in the TOOL_REGISTRY → resolves to `clang.wasm`
3. **Kernel fetches** the WASM binary from CDN (Brotli-compressed, cached after first load)
4. **Kernel spawns** a new WASM instance with its own memory, ENV, argc/argv
5. **Process filesystem** is connected to the kernel VFS via syscall bridge — process sees the same `/usr/include`, `/home/user`, `/tmp` as every other process
6. **Process runs** `main(argc, argv)` → reads/writes files via kernel-mediated syscalls
7. **Process exits** → kernel captures exit code, stdout, stderr; memory is reclaimed
8. **Shell continues** with the next command in the pipeline

#### Async Filesystem Operations via JSPI (JavaScript Promise Integration)

Emscripten ordinarily requires **synchronous** filesystem operations — POSIX APIs like `open()`, `read()`, `write()` must complete immediately within the same call stack. However, lazy-loading from a CDN and IndexedDB requires **asynchronous I/O** (network fetch, async IndexedDB queries). To bridge this gap, Emception uses **JavaScript Promise Integration (JSPI)**, a WebAssembly standard that allows WASM to suspend and resume execution asynchronously.

**JSPI Strategy**:

1. **Enable JSPI in Emscripten**: Compile tools with `-sJSPI=1` flag to enable promise-returning functions:
   ```
   em++ file.cpp -sJSPI=1 -sEXIT_RUNTIME=1 ...
   ```

2. **Async Syscall Bridge**: The FS layer exposes syscalls as **promise-returning** JavaScript functions. When a WASM process calls `open()` or `read()`:
   - The syscall handler checks if the file needs to be fetched from the CDN (LazyFS)
   - If a fetch is needed, the syscall **returns a Promise**
   - JSPI **suspends** the WASM execution
   - JavaScript **does** the async I/O (fetch, decompress, IndexedDB write)
   - JavaScript **resumes** the WASM execution with the result
   - WASM continues as if the syscall completed synchronously

3. **Transparent to user code**: The process code (C/C++ or Python) sees **blocking I/O** semantics. Under the hood, JSPI transparently converts the blocking syscall into an async operation:
   ```c
   // User code sees this as a synchronous open
   FILE *f = fopen("/usr/include/stdio.h", "r");
   // Internally:
   // - syscall: open("/usr/include/stdio.h") → Promise
   // - JSPI suspends WASM
   // - fetch from CDN, decompress, unpack to IDB
   // - JSPI resumes WASM
   // - open() returns normally
   ```

4. **Fallback strategies** (if JSPI unavailable):
   - **Asyncify**: Emscripten's older mechanism (slower, larger binary, runtime overhead) that replays execution to unwind and resume the stack.
   - **WebWorker + SharedArrayBuffer**: Offload blocking I/O to a worker thread with shared memory, allowing the main thread to block-wait without freezing the browser.

> **❌ NOT ACCEPTABLE**: Do NOT preload or "warm" the filesystem at startup. Preloading defeats the purpose of lazy loading and wastes bandwidth/storage. The system must download files on-demand when accessed.

#### I/O Stream Callbacks

In addition to filesystem hijacking, `stdin`, `stdout`, and `stderr` are hijacked and replaced with **callback functions**:

| Stream | Callback | Purpose |
|--------|----------|---------|
| **stdin** | `async (size: number) => Uint8Array` | Read up to `size` bytes from the input stream. Allows the shell to feed data to processes (interactive terminal input, piped data from previous processes). |
| **stdout** | `(data: Uint8Array) => Promise<void>` | Write data to the output stream. Called whenever a process outputs text. The kernel routes this to the terminal UI or pipes it to the next process in a pipeline. |
| **stderr** | `(data: Uint8Array) => Promise<void>` | Write diagnostic/error data. Like stdout but semantically separate, allowing the shell to color or route differently. |

**Example flow** (interactive terminal):
1. User types `echo hello` + Enter in the terminal UI
2. Shell parses command → spawns `echo.wasm`
3. `echo` calls `write(1, "hello\n", 6)` (write to stdout)
4. Syscall bridge calls the **stdout callback** with `Uint8Array([104, 101, 108, 108, 111, 10])`
5. Callback sends data to the kernel's TTY layer
6. TTY layer routes to xterm.js → user sees "hello" in the terminal

**Example flow** (piped commands):
1. User types `cat file.txt | wc -l`
2. Shell spawns `cat.wasm` and `wc.wasm` with pipes connected
3. `cat` writes to stdout → TTY routes to intermediate buffer
4. `wc` reads from stdin → TTY feeds the buffer
5. `wc` writes result to stdout → final output shown to user

This callback pattern decouples WASM processes from the browser environment and enables flexible routing of I/O through the kernel.

### Subprocess Dispatch (emcc → clang/lld/wasm-opt)

When `emcc` (CPython running `emcc.py`) needs to invoke sub-tools (clang, lld, wasm-opt), it cannot use POSIX `subprocess.Popen` — there are no native processes in the browser. Instead, a **subprocess shim** (`orchestrator/emscripten/subprocess_shim.py`) replaces Python's `subprocess` module at runtime:

1. **emcc calls `subprocess.run(['clang', '-o', 'hello.o', 'hello.c'])`**
2. **Shim intercepts** → serializes the command as JSON to `/tmp/.subprocess_request`
3. **Shim calls `os.system('__dispatch_subprocess')`** → triggers JSPI suspension
4. **Kernel reads** the JSON request from the VFS
5. **Kernel spawns** the requested tool (clang.wasm) as a new isolated process with:
   - **stdin callback**: Feeds data from the parent process's stdin buffer to the child process
   - **stdout callback**: Captures the child's stdout and routes to the parent's stdout buffer
   - **stderr callback**: Captures the child's stderr and routes to the parent's stderr buffer
6. **Tool runs**, reads/writes via callbacks, exits
7. **JSPI resumes** CPython → shim reads exit code from VFS → returns to emcc

This IPC mechanism allows the single-threaded browser environment to run multi-process compilation pipelines synchronously from Python's perspective, with all I/O flowing through callback-based streams rather than temporary files.

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
    - Should connect with the stdin/stdout/stderr callbacks to provide an interactive terminal experience
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

// todo update this