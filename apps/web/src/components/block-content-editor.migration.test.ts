import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const webRoot = process.cwd();

describe('block content editor migration surface', () => {
  it.each([
    'src/components/block-content-editor/engines/editor-provider.tsx',
    'src/components/block-content-editor/engines/static-viewer-sections.tsx',
    'src/app/[locale]/(block-content-editor)/block-content-editor/page.tsx',
    'src/app/[locale]/(block-content-editor)/block-content-editor/studio/page.tsx',
    'src/app/api/static-viewer/folder/[folderName]/route.ts',
    'src/app/api/static-viewer/file/[...path]/route.ts',
    'src/data/test-blocks/projeto-17792247804366bs8q7l9t/index.json',
    'public/assets/clang.wasm',
    'public/assets/lld.wasm',
    'public/assets/memfs.wasm',
  ])('keeps %s available', (relativePath) => {
    expect(existsSync(join(webRoot, relativePath))).toBe(true);
  });
});
