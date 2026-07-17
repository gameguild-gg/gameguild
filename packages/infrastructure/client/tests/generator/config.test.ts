import { describe, expect, it } from 'vitest';

import { resolveGeneratorConfig } from '../../scripts/config.js';

describe('generator configuration', () => {
  it('uses a captured OpenAPI artifact and stable source label', () => {
    const config = resolveGeneratorConfig(
      ['--openapi', 'artifacts/test-results/openapi/openapi.json', '--force'],
      {}
    );

    expect(config.openApiSource).toBe('artifacts/test-results/openapi/openapi.json');
    expect(config.generatedSourceLabel).toBe('captured-openapi');
    expect(config.force).toBe(true);
  });

  it('rejects --openapi without a path', () => {
    expect(() => resolveGeneratorConfig(['--openapi'], {})).toThrow(
      '--openapi requires a JSON artifact path'
    );
  });
});
