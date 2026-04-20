# Phase 2 Implementation: Rust Runtime Adapter Wiring

**Status:** ✅ Complete  
**Date:** 2026-04-20  
**Scope:** Wasmer/browser runtime routing for rustc (Rust-only)

## Summary

Phase 2 wires the Rust runtime adapter into the execution path. When `globalThis.__EMCEPTION_RUST_RUNTIME` is set to `"wasmer-browser"`, the rustc tool now routes through the WASI runtime instead of the Emscripten path.

## What Changed

### Modified: `tool-runner.ts` (lines ~560-575)

**Before:**

```typescript
if (rustRuntime === 'wasmer-browser') {
  // Phase-1 scaffold: adapter not wired yet, keep Rust functional via Emscripten.
  console.log(`${LOG_PREFIX}   rustc wasmer-browser requested; using Emscripten fallback until adapter is enabled`);
}
// Spawn an isolated WASM process (Emscripten)
const result = await this.spawnProcess(descriptor, argv, options, toolBasename);
```

**After:**

```typescript
if (rustRuntime === 'wasmer-browser') {
  // Phase 2: Route rustc to WASI runtime instead of Emscripten.
  console.log(`${LOG_PREFIX}   Routing rustc → WASI runtime (enableFS=true, filesystem access enabled)`);
  const rustcWasmPath = descriptor.modulePath; // /usr/lib/rust/rustc.wasm
  const wasiArgv = ['wasi-run', rustcWasmPath, ...argv.slice(1)];
  const result = await this.runWasi(wasiArgv, { ...options, enableFS: true });
  console.log(`${LOG_PREFIX} ===== RUN COMPLETE: ${tool} — exitCode=${result.exitCode}, total=${elapsed(tTotal)} =====`);
  return result;
}
// Spawn an isolated WASM process (Emscripten fallback)
const result = await this.spawnProcess(descriptor, argv, options, toolBasename);
```

## Execution Path

### When `__EMCEPTION_RUST_RUNTIME = "wasmer-browser"` (Feature Flag Enabled)

```
run("rustc", argv, options)
  ↓
selectRustRuntimeForTool("rustc") → "wasmer-browser"
  ↓
runWasi(['wasi-run', '/usr/lib/rust/rustc.wasm', ...args], {enableFS: true})
  ↓
WASI Runtime
  - Pre-loads VFS files (sysroot rlibs, source files, headers)
  - Pre-opens directories: /, /tmp, /home, /home/user, /usr
  - Sets env vars: SYSROOT=/usr/lib/rust, HOME=/home/user, etc.
  - Dispatches rustc-main.wasm WebAssembly instance
  - Handles path_open, fd_read, fd_write, fd_seek for filesystem ops
  - Harvests written files back to VFS after execution
```

### When `__EMCEPTION_RUST_RUNTIME = "emscripten"` or Not Set (Default)

```
run("rustc", argv, options)
  ↓
selectRustRuntimeForTool("rustc") → "emscripten"
  ↓
spawnProcess(descriptor, argv, options)
  ↓
Emscripten Runtime (existing path — no changes)
```

## Rust-Only Scope Guard

Non-Rust tools (C++, Python, Ninja, etc.) are **not affected**:

```typescript
// In selectRustRuntimeForTool()
export function selectRustRuntimeForTool(toolBasename: string): RustRuntimeKind {
  if (toolBasename !== 'rustc') return 'emscripten'; // Hard guard — non-Rust tools always Emscripten

  const configured = readRustRuntimeFlag();
  return configured ?? 'emscripten'; // Fallback to Emscripten if flag not set
}
```

## WASI Runtime Capabilities (Phase 1 Already Implemented)

The `runWasi()` method provides full WASI support for rustc:

| Feature                     | Implementation                                                           |
| --------------------------- | ------------------------------------------------------------------------ |
| **Filesystem**              | VFS-backed file I/O with pre-loading and artifact harvesting             |
| **Directories**             | Pre-opened: /, /tmp, /home, /home/user, /usr                             |
| **WASI Imports**            | path_open, fd_read, fd_write, fd_seek, fd_readdir, fd_filestat_get, etc. |
| **Env Vars**                | SYSROOT, RUSTC_ICE, HOME, TMPDIR, and full rustup environment            |
| **Stdin/Stdout/Stderr**     | Routed through WebAssembly imports                                       |
| **Exit Handling**           | proc_exit traps caught and converted to exitCode                         |
| **C++ Exceptions**          | **cxa_throw, **cxa*begin_catch, invoke*\* trampolines supported          |
| **Indirect Function Table** | Large table (131072 entries) for complex LLVM call sites                 |

## Validation Checklist (Phase 3)

- [ ] **Rust E2E Test:** `rust-terminal workspace compiles and runs Rust program`
  - Command: `npm run e2e:rust`
  - Expected: Compiles to wasm32-wasip1, runs interactive stdin example
  - Must pass with `__EMCEPTION_RUST_RUNTIME = "wasmer-browser"` set in test
- [ ] **Emscripten Fallback:** Same test with default flag (or not set)
  - Command: `npm run e2e:rust` (with feature flag unset)
  - Expected: Should still work via Emscripten path
- [ ] **Non-Rust Regression:** C++ and Python workspaces still work
  - C++: `cpp-terminal` workspace compiles and runs
  - Python: `python` workspace executes scripts
  - Expected: No behavior changes (still Emscripten)

## Known Limitations (Phase 2)

- Wasmer/Browserpod integration not yet active (feature flag is manual)
- No automatic fallback if WASI path fails (must catch errors in Phase 3)
- File preloading list is static (large rustc rlibs may slow startup)

## Next Steps (Phase 3 & 4)

### Phase 3 — Integration & Validation

1. Run full Rust E2E test with feature flag enabled
2. Capture performance metrics (startup, compile time)
3. Run non-Rust regression checks (C++, Python, Ninja)
4. Debug any WASI/filesystem issues that surface

### Phase 4 — Rollout

1. Add performance diagnostics (WASI vs Emscripten comparison)
2. Promote feature flag to default (once green)
3. Add fallback error handling for WASI failures
4. Document final architecture in README

## File References

- Plan: [RUST_RUNTIME_MIGRATION_PLAN.md](./RUST_RUNTIME_MIGRATION_PLAN.md)
- README: [README.md](./README.md#rust-runtime-migration-wasmerbrowserpod-rust-only)
- Scaffolding:
  - [src/lib/orchestrator/runtimes/rust-runtime.ts](./src/lib/orchestrator/runtimes/rust-runtime.ts)
  - [src/lib/orchestrator/runtimes/runtime-adapter.ts](./src/lib/orchestrator/runtimes/runtime-adapter.ts)
  - [src/lib/orchestrator/runtimes/wasmer-rust-adapter.ts](./src/lib/orchestrator/runtimes/wasmer-rust-adapter.ts)
- Core: [src/lib/orchestrator/tool-runner.ts](./src/lib/orchestrator/tool-runner.ts) (modified)

## Acceptance Criteria (Phase 2 Complete)

✅ Rust-only runtime selection scaffolding in place  
✅ Feature flag routing implemented  
✅ WASI adapter path wired into execution flow  
✅ Emscripten fallback preserved (default behavior)  
✅ Non-Rust tools unaffected  
✅ No TypeScript compilation errors

Ready for Phase 3 validation.
