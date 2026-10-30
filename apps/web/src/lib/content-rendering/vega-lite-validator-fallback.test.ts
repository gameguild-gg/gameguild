import { describe, expect, it, vi } from 'vitest';

import { VegaLiteValidator } from '../../../../../packages/features/lexical-surface/src/features/vega-lite/editor/vega-lite-validator';

vi.mock('vega-lite', () => {
  throw new Error('Compiler unavailable for fallback control');
});

describe('Vega-Lite validation without the optional compiler', () => {
  it('preserves basic validation and warnings when the library cannot load', async () => {
    const spec = JSON.stringify({
      data: { values: [{ category: 'A', value: 2 }] },
      mark: 'bar',
      encoding: {
        x: { field: 'category', type: 'nominal' },
        y: { field: 'value', type: 'quantitative' },
      },
    });

    const result = await VegaLiteValidator.validateSpec(spec);

    expect(result.isValid).toBe(true);
    expect(result.warnings?.length).toBeGreaterThan(0);
  });
});
