import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const webRoot = process.cwd();

describe('block content editor migration surface', () => {
  it.each([
    'src/components/block-content-editor/engines/editor-provider.tsx',
    'src/app/[locale]/(block-content-editor)/block-content-editor/page.tsx',
    'src/app/[locale]/(block-content-editor)/block-content-editor/studio/page.tsx',
  ])('keeps %s available', (relativePath) => {
    expect(existsSync(join(webRoot, relativePath))).toBe(true);
  });

  it.each([
    'public/assets/clang.wasm',
    'public/assets/lld.wasm',
    'public/assets/memfs.wasm',
    'public/assets/sysroot.tar',
  ])('does not version generated compiler binary %s', (relativePath) => {
    expect(existsSync(join(webRoot, relativePath))).toBe(false);
  });
});