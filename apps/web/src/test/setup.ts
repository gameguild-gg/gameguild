import * as matchers from '@testing-library/jest-dom/matchers';
import { cleanup } from '@testing-library/react';
import { afterEach, expect } from 'vitest';

expect.extend(matchers);
afterEach(cleanup);

process.env.AUTH_SECRET ??= 'vitest-auth-secret-must-be-at-least-32-characters';

if (typeof window !== 'undefined' && !window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  });
}

// JSDOM does not implement the SVG geometry APIs Mermaid uses to size labels.
// Keep the deterministic test geometry here so every Mermaid consumer exercises
// the real renderer instead of requiring individual tests to mock the component.
if (typeof SVGElement !== 'undefined') {
  Object.defineProperties(SVGElement.prototype, {
    getBBox: {
      configurable: true,
      value: () => ({ x: 0, y: 0, width: 80, height: 30 }),
    },
    getComputedTextLength: {
      configurable: true,
      value: () => 40,
    },
  });
}

// Vega-Lite probes for a 2D canvas before falling back to SVG. JSDOM logs an
// unimplemented-method error for that probe, while browsers return a context.
// Returning null preserves the renderer's SVG fallback without noisy false
// failures in otherwise successful coverage runs.
if (typeof HTMLCanvasElement !== 'undefined') {
  Object.defineProperty(HTMLCanvasElement.prototype, 'getContext', {
    configurable: true,
    value: () => null,
  });
}
