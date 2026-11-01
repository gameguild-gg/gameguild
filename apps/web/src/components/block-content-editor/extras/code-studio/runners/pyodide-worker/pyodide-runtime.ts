/**
 * Worker-side Pyodide runtime loading (#111).
 *
 * The legacy main-thread runner fetched the gzipped Pyodide bootstrap and
 * injected it into the DOM with a <script> tag. Inside a Worker there is no
 * DOM, so the script is fetched, gunzipped with `pako`, wrapped in a Blob and
 * dynamically imported — the same pattern the Emception worker uses for its
 * brotli module. Nothing leaks into the page: the module lives and dies with
 * the Worker, and terminating the Worker frees the WASM heap entirely.
 */

import { ungzip } from 'pako';

/** The subset of the Pyodide API the worker bridge relies on. */
export interface PyodideRuntime {
  runPythonAsync(code: string): Promise<unknown>;
  setStdout(options: { batched: (text: string) => void }): void;
  setStderr(options: { batched: (text: string) => void }): void;
  setStdin(options: { stdin: () => string | null }): void;
  setInterruptBuffer(buffer: Int32Array): void;
  loadPackagesFromImports?(code: string): Promise<void>;
  FS: {
    writeFile(path: string, data: string | Uint8Array): void;
    mkdir(path: string): void;
  };
}

type LoadPyodideFn = (options?: { indexURL?: string; fullStdLib?: boolean }) => Promise<PyodideRuntime>;

/**
 * Wraps a classic/UMD script so it can be dynamically imported as an ES
 * module. `var loadPyodide`/`self.loadPyodide` stay in scope, so the appended
 * export recovers the loader either way.
 */
function toEsmModuleSource(source: string): string {
  return `${source}\n;export default typeof loadPyodide !== 'undefined' ? loadPyodide : self.loadPyodide;\n`;
}

async function loadPyodideScriptModule(scriptUrl: string): Promise<LoadPyodideFn> {
  const response = await fetch(scriptUrl, { cache: 'force-cache' });
  if (!response.ok) {
    throw new Error(`Failed to fetch ${scriptUrl}: HTTP ${response.status}`);
  }
  const compressed = new Uint8Array(await response.arrayBuffer());
  const scriptText = new TextDecoder().decode(ungzip(compressed));

  const blob = new Blob([toEsmModuleSource(scriptText)], { type: 'text/javascript' });
  const blobUrl = URL.createObjectURL(blob);
  try {
    // `webpackIgnore`/`turbopackIgnore` keep the bundler from trying to
    // resolve the runtime-generated blob URL at build time (same trick as the
    // Emception worker uses for its brotli module).
    const module = (await import(/* webpackIgnore: true */ /* turbopackIgnore: true */ blobUrl)) as { default?: LoadPyodideFn };
    const loadPyodide = module.default ?? (self as unknown as { loadPyodide?: LoadPyodideFn }).loadPyodide;
    if (typeof loadPyodide !== 'function') {
      throw new Error('Pyodide script did not expose a loadPyodide function');
    }
    return loadPyodide;
  } finally {
    URL.revokeObjectURL(blobUrl);
  }
}

/** Loads and initialises Pyodide inside the Worker. */
export async function loadPyodideRuntime(scriptUrl: string, indexUrl: string): Promise<PyodideRuntime> {
  const loadPyodide = await loadPyodideScriptModule(scriptUrl);
  return loadPyodide({ indexURL: indexUrl, fullStdLib: false });
}
