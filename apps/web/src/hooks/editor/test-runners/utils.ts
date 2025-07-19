import type { TestCase } from './types';

/**
 * Format test output for display
 */
export function formatTestOutput(test: TestCase, result: any): string {
  if (result.passed) {
    return `✅ Test passed`;
  } else if (result.error) {
    return `❌ Error: ${result.error}`;
  } else {
    return `❌ Expected: ${JSON.stringify(result.expected)}, Actual: ${JSON.stringify(result.actual)}`;
  }
}

/**
 * Deep equality comparison for objects and arrays
 */
export function deepEqual(a: any, b: any): boolean {
  if (a === b) return true;

  if (Array.isArray(a) && Array.isArray(b)) {
    if (a.length !== b.length) return false;
    for (let i = 0; i < a.length; i++) {
      if (!deepEqual(a[i], b[i])) return false;
    }
    return true;
  }

  if (typeof a === 'object' && a !== null && typeof b === 'object' && b !== null) {
    const keysA = Object.keys(a);
    const keysB = Object.keys(b);

    if (keysA.length !== keysB.length) return false;

    for (const key of keysA) {
      if (!keysB.includes(key)) return false;
      if (!deepEqual(a[key], b[key])) return false;
    }

    return true;
  }

  return false;
}
