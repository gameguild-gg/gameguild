/**
 * `emception/worker` — worker entry shim.
 *
 * Re-imports the Web Worker entry from `@emception/browser/worker` for
 * its side effects (registers `self.onmessage` handler and waits for the
 * boot message from the main thread).
 *
 * Previously this file held the full worker bootstrap logic;
 * it now lives in `@emception/browser/src/worker-entry.ts`.
 */
import '@gameguild/emception-browser/worker';
