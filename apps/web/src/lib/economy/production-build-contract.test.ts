import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

function readWorkspaceSource(path: string) {
  return readFileSync(resolve(process.cwd(), path), 'utf8');
}

describe('Economy production build contracts', () => {
  it('exports only async functions from the Economy server-action module', () => {
    const source = readWorkspaceSource('src/lib/economy/actions.ts');

    expect(source).not.toMatch(/export\s+function\s+\w+/);
  });

  it('loads the browser-only SumSub SDK without next/dynamic', () => {
    const source = readWorkspaceSource('src/components/economy/economy-kyc-workspace.tsx');

    expect(source).not.toContain("from 'next/dynamic'");
    expect(source).toContain("import('@sumsub/websdk-react')");
  });
});
