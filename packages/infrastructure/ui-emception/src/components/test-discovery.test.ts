import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';

describe('emception-ui Jest test discovery', () => {
  it('uses the shared Jest 30-compatible matcher and deterministic test command', () => {
    const sharedConfig = readFileSync(resolve(process.cwd(), '../../tooling/jest/src/index.js'), 'utf8');
    const packageConfig = readFileSync(resolve(process.cwd(), 'jest.config.mjs'), 'utf8');
    const packageJson = JSON.parse(readFileSync(resolve(process.cwd(), 'package.json'), 'utf8')) as {
      scripts: Record<string, string>;
    };

    expect(sharedConfig).toContain("testMatch: ['**/*.{test,spec}.{js,jsx,ts,tsx}']");
    expect(packageConfig).not.toMatch(/^\s*testMatch:/m);
    expect(packageJson.scripts.test).toBe('jest --runInBand');
  });
});
