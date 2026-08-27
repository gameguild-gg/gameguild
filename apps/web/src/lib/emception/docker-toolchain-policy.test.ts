import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { describe, expect, it } from 'vitest';

describe('web Docker Toolchain source policy', () => {
  it('uses the exact Toolchain package version rather than a legacy latest package', async () => {
    const dockerfile = await readFile(path.resolve(process.cwd(), 'Dockerfile'), 'utf8');

    expect(dockerfile).toContain('@gameguild/emception-toolchain@${TOOLCHAIN_VERSION}');
    expect(dockerfile).not.toContain('emception@latest');
    expect(dockerfile).not.toContain('tools/emception/public/cdn');
  });
});
