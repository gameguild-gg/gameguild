/**
 * Host-side Pyodide Worker factory (#111).
 *
 * Isolated in its own module so tests can mock the construction while the
 * real factory keeps the webpack `new Worker(new URL(...))` pattern that
 * emits the dedicated `pyodide-runtime` chunk. Mirrors the approach used by
 * the Emception toolchain worker (see `tools/emception/.../boot.ts`).
 */

import { PYODIDE_WORKER_NAME } from './protocol';

/** True when the current environment can construct Web Workers at all. */
export function isWorkerAvailable(): boolean {
  return typeof window !== 'undefined' && typeof Worker !== 'undefined';
}

export function createPyodideWorker(): Worker | null {
  if (!isWorkerAvailable()) return null;
  try {
    return new Worker(new URL('./pyodide.worker', import.meta.url), { type: 'module', name: PYODIDE_WORKER_NAME });
  } catch {
    // CSP restrictions or a cross-origin-isolation mismatch can make worker
    // construction throw; callers degrade gracefully to the main thread.
    return null;
  }
}
