// Runtime feature guards.
//
// Some `RunOptions` / `StdinInput` shapes only make sense in a browser
// environment: an `HTMLCanvasElement` / `OffscreenCanvas` for SDL output,
// or an xterm.js `Terminal` instance as a stdin source. Calling them from
// the Node adapter (or any future headless runtime) would silently mis-
// behave, so we fail fast with `RuntimeFeatureUnavailableError` (or its
// `CanvasUnavailableError` subclass) and a message that points users at
// `@emception/browser`.
//
// The detection is duck-typed because `@emception/core` MUST stay DOM-free:
// we recognise the shapes by their structural
// fingerprint rather than by referencing real DOM types.

import { CanvasUnavailableError, RuntimeFeatureUnavailableError } from '../errors.js';
import type { StdinInput } from '../types';

/**
 * Identifies the calling runtime in error messages so users know which
 * package they would need to swap to.
 */
export type RuntimeLabel = 'node' | 'headless' | string;

/**
 * Structural test for `HTMLCanvasElement` / `OffscreenCanvas` /
 * `HTMLCanvasElement.transferControlToOffscreen()` results.
 */
export function looksLikeCanvas(value: unknown): boolean {
  if (value === null || typeof value !== 'object') return false;
  const v = value as Record<string, unknown>;
  // OffscreenCanvas + HTMLCanvasElement both expose `width`/`height` as
  // numbers and a `getContext` method. HTMLCanvasElement also has
  // `transferControlToOffscreen`. We accept either.
  if (typeof v.width !== 'number' || typeof v.height !== 'number') return false;
  if (typeof v.getContext === 'function') return true;
  if (typeof v.transferControlToOffscreen === 'function') return true;
  return false;
}

/**
 * Structural test for an xterm.js `Terminal` instance used as a stdin
 * source. Real xterm `Terminal` objects expose `onData`, `write`, and
 * (typically) a numeric `cols` / `rows` pair.
 */
export function looksLikeXtermTerminal(value: unknown): boolean {
  if (value === null || typeof value !== 'object') return false;
  const v = value as Record<string, unknown>;
  return typeof v.onData === 'function' && typeof v.write === 'function';
}

/**
 * Throws {@link CanvasUnavailableError} if `canvas` is non-null and looks
 * like a `Canvas`-shaped object. Pass `runtime` (e.g. `'node'`) so the
 * error message can recommend the right package.
 */
export function assertCanvasUnsupported(canvas: unknown, runtime: RuntimeLabel = 'this runtime'): void {
  if (canvas === undefined || canvas === null) return;
  if (!looksLikeCanvas(canvas)) {
    throw new RuntimeFeatureUnavailableError(`RunOptions.canvas was provided to ${runtime} but the value is not a Canvas-shaped object`);
  }
  throw new CanvasUnavailableError(`Canvas output is unavailable in ${runtime}. Use @emception/browser (web worker + OffscreenCanvas) for SDL / GUI presets.`);
}

/**
 * Throws {@link RuntimeFeatureUnavailableError} if `stdin` is an xterm.js
 * `Terminal` instance. Other stdin shapes (string, Uint8Array, async
 * iterable, ReadableStream, function, `'none'`) are accepted as-is.
 */
export function assertXtermStdinUnsupported(stdin: StdinInput | undefined, runtime: RuntimeLabel = 'this runtime'): void {
  if (stdin === undefined || stdin === null) return;
  if (stdin === 'none') return;
  if (typeof stdin === 'string') return;
  if (stdin instanceof Uint8Array) return;
  if (typeof stdin === 'function') return;
  if (looksLikeXtermTerminal(stdin)) {
    throw new RuntimeFeatureUnavailableError(
      `xterm.js Terminal stdin is unavailable in ${runtime}. Use @emception/browser + @emception/xterm to wire a Terminal, or pass a string / Uint8Array / AsyncIterable / ReadableStream / callback instead.`,
    );
  }
}

/**
 * Convenience that runs both guards on a single `RunOptions`-shaped
 * object. Designed to be called once at the top of a non-browser
 * `createEmception().run()` implementation.
 */
export function assertNoBrowserOnlyFeatures(opts: { canvas?: unknown; stdin?: StdinInput } | undefined, runtime: RuntimeLabel = 'this runtime'): void {
  if (!opts) return;
  assertCanvasUnsupported(opts.canvas, runtime);
  assertXtermStdinUnsupported(opts.stdin, runtime);
}
