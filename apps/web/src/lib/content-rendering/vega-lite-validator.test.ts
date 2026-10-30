import { afterEach, describe, expect, it, vi } from 'vitest';
import { compile } from 'vega-lite';

import { VegaLiteValidator } from '../../../../../packages/features/lexical-surface/src/features/vega-lite/editor/vega-lite-validator';

vi.mock('vega-lite', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vega-lite')>();
  return { ...actual, compile: vi.fn(actual.compile) };
});

const validSpec = JSON.stringify({
  $schema: 'https://vega.github.io/schema/vega-lite/v6.json',
  data: { values: [{ category: 'A', value: 2 }] },
  mark: 'bar',
  encoding: {
    x: { field: 'category', type: 'nominal' },
    y: { field: 'value', type: 'quantitative' },
  },
});

afterEach(() => {
  vi.clearAllMocks();
});

describe('Vega-Lite validation boundaries', () => {
  it('accepts a valid chart through the real compiler', async () => {
    expect(await VegaLiteValidator.validateSpec(validSpec)).toEqual({ isValid: true, warnings: undefined });
    expect(compile).toHaveBeenCalledTimes(1);
  });

  it.each([null, undefined])('keeps a compiler failure invalid when the thrown value is %s', async (fault) => {
    vi.mocked(compile).mockImplementationOnce(() => {
      throw fault;
    });

    const result = await VegaLiteValidator.validateSpec(validSpec);

    expect(result.isValid).toBe(false);
    expect(result.error).toMatch(/^Compilation error:/);
  });

  it('preserves an ordinary compiler error as a failed validation', async () => {
    vi.mocked(compile).mockImplementationOnce(() => {
      throw new Error('Invalid chart');
    });

    expect(await VegaLiteValidator.validateSpec(validSpec)).toEqual({
      isValid: false,
      error: 'Compilation error: Invalid chart',
    });
  });

  it('rejects malformed JSON before chart compilation', async () => {
    const result = await VegaLiteValidator.validateSpec('{broken');

    expect(result.isValid).toBe(false);
    expect(result.error).toMatch(/^Invalid JSON:/);
    expect(compile).not.toHaveBeenCalled();
  });

  it('returns only string field names from untrusted encoding definitions', () => {
    const spec = JSON.stringify({
      data: { values: [{ category: 'A', value: 2 }] },
      encoding: {
        x: { field: 'category' },
        y: { field: 'value' },
        color: { field: 42 },
        tooltip: { field: { nested: 'invalid' } },
        size: { field: null },
        detail: { field: false },
      },
    });

    expect(VegaLiteValidator.getFieldSuggestions(spec)).toEqual(['category', 'value']);
  });

  it('preserves legitimate field order and removes duplicates', () => {
    const spec = JSON.stringify({
      data: { values: [{ category: 'A' }] },
      encoding: {
        x: { field: 'category' },
        y: { field: 'value' },
        color: { field: 'region' },
        tooltip: { field: 'value' },
      },
    });

    expect(VegaLiteValidator.getFieldSuggestions(spec)).toEqual(['category', 'value', 'region']);
  });

  it('keeps the built-in chart templates usable by the real compiler', async () => {
    const templates: Record<string, unknown> = VegaLiteValidator.getCommonPatterns();
    expect(Object.keys(templates).length).toBeGreaterThan(0);

    for (const template of Object.values(templates)) {
      if (!template || typeof template !== 'object' || Array.isArray(template)) {
        throw new Error('A chart template must be a JSON object');
      }
      const result = await VegaLiteValidator.validateSpec(JSON.stringify({
        ...template,
        data: { values: [{ category: 'A', value: 2, date: '2026-01-01', x: 1, y: 2 }] },
      }));
      expect(result.isValid).toBe(true);
    }
  });
});
