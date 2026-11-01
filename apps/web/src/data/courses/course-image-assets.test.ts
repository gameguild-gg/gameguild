import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

import { describe, expect, it } from 'vitest';

/**
 * Issue #117 regression: course markdown may reference self-hosted assets with
 * root-relative URLs (e.g. the Bloom's Taxonomy figure on the ai4games
 * syllabus). A missing file renders as a broken image on the course page, and
 * nothing else fails at build time — so assert here that every root-relative
 * image reference in the course corpus resolves to a real file under public/.
 */

const COURSES_ROOT = path.dirname(fileURLToPath(import.meta.url));
const PUBLIC_ROOT = path.resolve(COURSES_ROOT, '../../../public');

function listMarkdownFiles(dir: string): string[] {
  const entries: string[] = [];
  for (const name of readdirSync(dir)) {
    const full = path.join(dir, name);
    if (statSync(full).isDirectory()) {
      entries.push(...listMarkdownFiles(full));
    } else if (name.endsWith('.md')) {
      entries.push(full);
    }
  }
  return entries;
}

/** Root-relative asset links inside markdown images: ![alt](/assets/...) */
const ROOT_RELATIVE_IMAGE_SRC = /!\[[^\]]*\]\((\/[^)\s]+)\)/g;

describe('course markdown image assets', () => {
  it('references at least one self-hosted image (Bloom figure present)', () => {
    const markdown = readFileSync(path.join(COURSES_ROOT, 'ai4games', 'syllabus.md'), 'utf8');

    expect(markdown).toMatch(/!\[Bloom's Taxonomy\]\(\/assets\/images\/courses\/blooms-taxonomy\.jpg\)/);
  });

  it('resolves every root-relative image reference to a real file under public/', () => {
    const files = listMarkdownFiles(COURSES_ROOT);
    expect(files.length).toBeGreaterThan(0);

    const missing: Array<{ file: string; src: string }> = [];
    for (const file of files) {
      const markdown = readFileSync(file, 'utf8');
      for (const match of markdown.matchAll(ROOT_RELATIVE_IMAGE_SRC)) {
        const src = match[1];
        const assetPath = path.join(PUBLIC_ROOT, src.replace(/^\//, ''));
        if (!existsSync(assetPath)) {
          missing.push({ file: path.relative(COURSES_ROOT, file), src });
        }
      }
    }

    expect(missing).toEqual([]);
  });
});
