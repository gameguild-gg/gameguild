import { readdir, readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const repoRoot = resolve(process.cwd(), '../..');
const learningRoots = [
  'apps/web/src/app/[locale]/(dashboards)/workspace/learning',
  'apps/web/src/app/[locale]/(dashboards)/console/learning',
];

async function sourceFiles(relativePath: string): Promise<string[]> {
  const entries = await readdir(resolve(repoRoot, relativePath), {
    withFileTypes: true,
  });
  const files = await Promise.all(
    entries.map(async (entry) => {
      const entryPath = `${relativePath}/${entry.name}`;
      if (entry.isDirectory()) return sourceFiles(entryPath);
      return /\.[jt]sx?$/.test(entry.name) ? [entryPath] : [];
    }),
  );

  return files.flat();
}

describe('Learning locale navigation', () => {
  it('never builds Workspace or Console navigation with a manual locale prefix', async () => {
    const files = (
      await Promise.all(learningRoots.map((root) => sourceFiles(root)))
    ).flat();
    const violations: string[] = [];

    for (const file of files) {
      const source = await readFile(resolve(repoRoot, file), 'utf8');
      if (/`\/\$\{locale\}\/(?:workspace|console)\/learning/.test(source)) {
        violations.push(file);
      }
    }

    expect(violations).toEqual([]);
  });
});
