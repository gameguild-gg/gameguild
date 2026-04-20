export type RustRuntimeKind = 'emscripten' | 'wasmer-browser';

/**
 * Feature-flag selector for Rust runtime migration.
 *
 * Scope guard:
 * - Only applies to the `rustc` tool path.
 * - All non-Rust tools must remain on Emscripten.
 *
 * Flag source (browser):
 * - globalThis.__EMCEPTION_RUST_RUNTIME = 'emscripten' | 'wasmer-browser'
 */
export function selectRustRuntimeForTool(toolBasename: string): RustRuntimeKind {
    if (toolBasename !== 'rustc') return 'emscripten';

    const configured = readRustRuntimeFlag();
    return configured ?? 'emscripten';
}

function readRustRuntimeFlag(): RustRuntimeKind | null {
    const globalValue = (globalThis as { __EMCEPTION_RUST_RUNTIME?: unknown }).__EMCEPTION_RUST_RUNTIME;
    if (globalValue === 'emscripten' || globalValue === 'wasmer-browser') {
        return globalValue;
    }
    return null;
}
