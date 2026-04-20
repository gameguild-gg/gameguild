/**
 * Stub for @wasmer/wasi — used at build time (Turbopack/webpack) to prevent
 * errors when the package's WASM binary is unavailable.
 *
 * WasmerRustAdapter catches the resulting runtime error and falls back to the
 * built-in WASI runtime automatically.
 */

export async function init(): Promise<void> {
    throw new Error('@wasmer/wasi: WASM binary not available');
}

export class WASI {
    constructor(_config: unknown) {
        throw new Error('@wasmer/wasi: WASM binary not available');
    }
}
