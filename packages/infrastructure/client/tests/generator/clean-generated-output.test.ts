import { existsSync, mkdtempSync, mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';

import { cleanGeneratedOutput } from '../../scripts/utils/clean-generated-output.js';

describe('cleanGeneratedOutput', () => {
  const directories: string[] = [];

  afterEach(() => {
    for (const directory of directories.splice(0)) {
      rmSync(directory, { force: true, recursive: true });
    }
  });

  it('removes stale generated modules before recreating the output directory', () => {
    const root = mkdtempSync(join(tmpdir(), 'game-guild-client-'));
    directories.push(root);
    const output = join(root, 'generated');
    const modules = join(output, 'modules');
    mkdirSync(modules, { recursive: true });
    writeFileSync(join(output, 'types.gen.ts'), 'stale');
    writeFileSync(join(modules, 'realestate.gen.ts'), 'stale');

    cleanGeneratedOutput(output);

    expect(existsSync(output)).toBe(true);
    expect(existsSync(join(output, 'types.gen.ts'))).toBe(false);
    expect(existsSync(join(modules, 'realestate.gen.ts'))).toBe(false);
  });
});
