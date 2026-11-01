/**
 * Host-side Pyodide Worker factory (#111).
 *
 * Isolated in its own module so tests can mock the construction while the
 * real factory keeps the webpack `new Worker(new URL(...))` pattern that
 * emits the dedicated `pyodide-runtime` chunk. Mirrors the approach used by
 * the Emception toolchain worker (see `tools/emception/.../boot.ts`).
 */

/** True when the current environment can construct Web Workers at all. */
export function isWorkerAvailable(): boolean {
  return typeof window !== 'undefined' && typeof Worker !== 'undefined';
}

export function createPyodideWorker(): Worker | null {
  if (!isWorkerAvailable()) return null;
  try {
    // `name` MUST be a string literal: webpack's Worker parser only applies
    // it to the emitted chunk filename (static/chunks/pyodide-runtime.<hash>.js)
    // when it can evaluate the option statically — an identifier reference
    // leaves the chunk numeric and the COI header rule in next.config.ts
    // would not match. Keep in sync with PYODIDE_WORKER_NAME.
    return new Worker(new URL('./pyodide.worker', import.meta.url), { type: 'module', name: 'pyodide-runtime' });
  } catch {
    // CSP restrictions or a cross-origin-isolation mismatch can make worker
    // construction throw; callers degrade gracefully to the main thread.
    return null;
  }
}
