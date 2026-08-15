import { describe, expect, it } from 'vitest';
import { readFileSync } from 'node:fs';

import { GeneratedApi, UsersModule } from '../../src/index.js';

describe('generated package exports', () => {
  it('exposes generated modules directly and through the compatibility namespace', () => {
    expect(GeneratedApi.UsersModule).toBe(UsersModule);
  });

  it('keeps the root entrypoint safe for React Server Components', () => {
    const source = readFileSync(new URL('../../src/index.ts', import.meta.url), 'utf8');

    expect(source).not.toContain("'./integrations/react/");
  });
});
