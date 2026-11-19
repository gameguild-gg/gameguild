import { readdirSync, readFileSync } from 'node:fs';
import { extname, join } from 'node:path';
import { describe, expect, it } from 'vitest';

describe('coding surface architecture', () => {
  it('depends only on public boundaries', () => {
    const source = readProductionSource(import.meta.dirname);
    const specifiers = [...source.matchAll(/(?:\bfrom\s+|\bimport\s*\(\s*)['"]([^'"]+)['"]/g)]
      .map((match) => match[1]);
    for (const specifier of specifiers) {
      expect(specifier).not.toContain('@game-guild/emception-ui');
      expect(specifier).not.toContain('apps/web');
      expect(specifier).not.toMatch(/^next(?:\/|$)/);
      expect(specifier).not.toContain('/src/');
    }
  });
});

function readProductionSource(directory: string): string {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return readProductionSource(path);
    if (entry.name.endsWith('.test.ts') || entry.name.endsWith('.test.tsx') || !['.ts', '.tsx'].includes(extname(entry.name))) return [];
    return [readFileSync(path, 'utf8')];
  }).join('\n');
}
