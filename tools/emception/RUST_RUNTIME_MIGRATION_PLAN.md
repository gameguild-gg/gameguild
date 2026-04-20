# Rust-Only Wasmer/Browserpod Runtime Migration Plan

## Goal

Migrate **Rust execution only** to a Wasmer/browser runtime path while keeping all other workspaces and toolchains (C/C++, Python, cmake, ninja, LLVM/Binaryen tools) on the current Emscripten path.

## Non-Negotiable Constraints

- Rust migration must be **strictly scoped** to `rustc` (and Rust run-path only if needed).
- No behavior regressions in non-Rust workspaces.
- Runtime switch must be behind a **feature flag** with immediate fallback to existing Emscripten Rust path.

## Phased Plan

### Phase 0 — Baseline & Guardrails

1. Lock current pass criteria using Playwright test:
   - `rust-terminal workspace compiles and runs Rust program`
2. Record current Rust runtime entry points in orchestrator:
   - `TOOL_REGISTRY.rustc`
   - `ToolRunner.run(...)`
   - `spawnProcess(...)`
3. Add runtime-selection logs for Rust only.

### Phase 1 — Runtime Abstraction (No Behavior Change)

1. Introduce runtime kind model in orchestrator:
   - `emscripten` (default)
   - `wasmer-browser` (new, Rust-only)
2. Add Rust-only runtime selector:
   - Feature flag driven
   - Defaults to Emscripten
3. Keep non-Rust tools hardcoded to Emscripten.

### Phase 2 — Wasmer/Browser Runtime Adapter (Rust)

1. Implement Rust adapter contract:
   - Module load
   - argv/env
   - stdout/stderr callbacks
   - exit/proc_exit mapping
2. Bridge file operations against current VFS layer.
3. Keep fallback path to Emscripten on adapter errors.

### Phase 3 — Integration & Validation

1. Validate Rust compile/run flow in `rust-terminal` workspace.
2. Run Rust E2E and confirm interactive stdin behavior.
3. Regression-check representative C/C++ workflow(s) to ensure no impact.

### Phase 4 — Rollout

1. Keep feature flag default conservative.
2. Add diagnostics to compare runtime behavior and latency.
3. Promote to default only after repeated green E2E + manual verification.

## Feature Flag Proposal

- `globalThis.__EMCEPTION_RUST_RUNTIME` with values:
  - `"emscripten"` (default)
  - `"wasmer-browser"` (opt-in)

## Acceptance Criteria

1. Rust E2E (`rust-terminal workspace compiles and runs Rust program`) passes using Wasmer/browser runtime.
2. Same test passes using fallback Emscripten runtime.
3. Non-Rust workflows remain unchanged and pass existing checks.

## Implementation Status (Current)

- Migration plan document added.
- README section linked and updated with runtime flag behavior.
- Rust-only runtime selection scaffolding implemented (`emscripten` vs `wasmer-browser`).
- Rust execution path now uses a Wasmer adapter when `__EMCEPTION_RUST_RUNTIME = "wasmer-browser"`.
- Adapter keeps a guarded fallback to the internal WASI runtime on adapter/runtime errors.
- Non-Rust toolchains remain on Emscripten.
