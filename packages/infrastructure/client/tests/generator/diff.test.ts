import { describe, expect, it } from 'vitest';

import { stableMetadata } from '../../scripts/diff.js';

describe('generated client diff metadata', () => {
  it('ignores per-run provenance while preserving integrity fields', () => {
    expect(stableMetadata({
      hash: 'spec-hash',
      apiVersion: '4.3.0',
      source: 'gameguild-openapi',
      generatedAt: '2026-08-29T00:00:00.000Z',
      generatedBy: 'developer',
    })).toEqual({
      hash: 'spec-hash',
      apiVersion: '4.3.0',
      source: 'gameguild-openapi',
    });
  });
});
