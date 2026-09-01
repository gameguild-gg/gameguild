import { describe, expect, it } from 'vitest';

import { stableMetadata } from '../../scripts/diff.js';

describe('generated client diff metadata', () => {
  it('ignores spec hashes that produce identical generated output', () => {
    expect(stableMetadata({
      hash: 'environment-dependent-spec-hash',
      apiVersion: '4.3.0',
      source: 'gameguild-openapi',
      generatedAt: '2026-08-29T00:00:00.000Z',
      generatedBy: 'developer',
    })).toEqual({
      apiVersion: '4.3.0',
      source: 'gameguild-openapi',
    });
  });
});
