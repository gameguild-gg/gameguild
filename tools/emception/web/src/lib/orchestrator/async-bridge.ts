/**
 * Async strategy detection: Asyncify (primary) vs JSPI (progressive enhancement).
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
