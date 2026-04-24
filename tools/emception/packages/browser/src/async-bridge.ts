/**
 * Async strategy: Asyncify.
 *
 * Emscripten's Asyncify instruments WASM binaries at compile time to support
 * stack unwinding/rewinding. Async JS imports (FS hooks, subprocess dispatch)
 * are declared via -sASYNCIFY_IMPORTS and handled transparently by Emscripten.
 * This works in ALL browsers (Chrome, Safari, Firefox, Edge).
 *
 * JSPI (JavaScript Promise Integration) was previously used but has been
 * removed: it only works in Chrome/Edge 137+ (~24% global browser share).
 */

export function detectAsyncStrategy(): 'asyncify' {
  return 'asyncify';
}
