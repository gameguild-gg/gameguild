/**
 * Pyodide Web Worker entry point (#111).
 *
 * Bundled by Next/webpack into its own chunk via the
 * `new Worker(new URL(...), { type: 'module', name: 'pyodide-runtime' })`
 * pattern in `create-pyodide-worker.ts`. All Python/WASM execution happens
 * here, off the main thread — the page never freezes and terminating this
 * worker fully releases the Pyodide WASM heap.
 */

import type { PyodideWorkerRequest } from './protocol';
import { loadPyodideRuntime } from './pyodide-runtime';
import { PyodideWorkerCore } from './worker-core';

const core = new PyodideWorkerCore((message) => self.postMessage(message), (scriptUrl, indexUrl) => loadPyodideRuntime(scriptUrl, indexUrl));

self.onmessage = (event: MessageEvent<PyodideWorkerRequest>) => {
  const message = event.data;
  if (!message || typeof message !== 'object' || typeof (message as { type?: unknown }).type !== 'string') return;
  core.handleMessage(message);
};

// Defensive: never let an stray rejection take the worker down silently.
self.addEventListener('unhandledrejection', (event) => {
  const reason = event.reason instanceof Error ? event.reason.message : String(event.reason);
  console.error('[pyodide-worker] unhandled rejection:', reason);
});
