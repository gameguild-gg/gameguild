/**
 * Async strategy detection: JSPI (primary) vs Asyncify (fallback).
 *
 * JSPI (JavaScript Promise Integration) enables transparent async I/O:
 * WASM stack is suspended while JS awaits file fetches from CDN/IDB.
 * The glue code patches wrap FS syscalls with WebAssembly.Suspending
 * and callMain with WebAssembly.promising.
 */

export function detectAsyncStrategy(): 'jspi' | 'asyncify' {
  try {
    if (typeof (WebAssembly as unknown as { Suspending?: unknown }).Suspending === 'function') {
      return 'jspi';
    }
  } catch {
    // ignore
  }
  return 'asyncify';
}

export function getEmscriptenFlags(): string[] {
  return [
    '-sASYNCIFY',
    '-sASYNCIFY_STACK_SIZE=65536',
    '-sASYNCIFY_IMPORTS=["emscripten_sleep","__syscall_read","__syscall_poll","fetch_async"]',
  ];
}
