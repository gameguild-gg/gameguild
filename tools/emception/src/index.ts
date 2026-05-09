/**
 * `emception` — meta package.
 *
 * This package is now a thin batteries-included alias around
 * `@emception/browser` (the browser runtime adapter) plus `@emception/xterm`
 * (the xterm.js I/O bridge), so existing consumers can keep using
 * `npm i emception` without reaching for the scoped packages directly.
 *
 * Migration guide (no code changes required for current consumers):
 *
 *     // Same surface as before:
 *     import { createEmception, bootInWorker } from 'emception';
 *
 *     // For TTY work, prefer the dedicated package:
 *     import { TTYBridge } from '@emception/xterm';
 *     // (still re-exported via `emception` for back-compat)
 */

export * from '@gameguild/emception-browser';
export * as xterm from '@gameguild/emception-xterm';

