import { describe, expect, it } from 'vitest';

import { resolveGeneratorConfig } from '../../scripts/config.js';

describe('generator configuration', () => {
  it('uses a captured OpenAPI artifact and stable source label', () => {
    const config = resolveGeneratorConfig(
      ['--openapi', 'artifacts/test-results/openapi/openapi.json', '--force'],
      {}
    );

    expect(config.openApiSource).toBe('artifacts/test-results/openapi/openapi.json');
    expect(config.generatedSourceLabel).toBe('gameguild-openapi');
    expect(config.force).toBe(true);
  });

  it('keeps generated output independent from the remote fetch URL', () => {
    const config = resolveGeneratorConfig(
      [],
      { OPENAPI_URL: 'http://127.0.0.1:15099/swagger/v1/swagger.json' },
    );

    expect(config.openApiSource).toBe(
      'http://127.0.0.1:15099/swagger/v1/swagger.json',
    );
    expect(config.generatedSourceLabel).toBe('gameguild-openapi');
  });

  it('rejects --openapi without a path', () => {
    expect(() => resolveGeneratorConfig(['--openapi'], {})).toThrow(
      '--openapi requires a JSON artifact path'
    );
  });
});
