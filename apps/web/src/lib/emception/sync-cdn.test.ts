import { promises as fs } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { afterEach, describe, expect, it } from 'vitest';
// Guard logic lives in the script itself (node runs it directly, so it must be
// plain ESM); vitest can import .mjs via a relative path regardless of its
// `src/**` include pattern (include only governs test discovery).
import { manifestsMatch, syncEmceptionCdn } from '../../../scripts/sync-emception-cdn.mjs';

const tempDirs: string[] = [];

async function makeTree() {
  const root = await fs.mkdtemp(path.join(os.tmpdir(), 'emception-sync-'));
  tempDirs.push(root);
  return root;
}

afterEach(async () => {
  await Promise.all(tempDirs.splice(0).map((dir) => fs.rm(dir, { recursive: true, force: true })));
});

describe('manifestsMatch', () => {
  it('returns true when source and target manifests are byte-identical', async () => {
    const root = await makeTree();
    const src = path.join(root, 'src', 'manifest.json');
    const tgt = path.join(root, 'tgt', 'manifest.json');
    await fs.mkdir(path.dirname(src), { recursive: true });
    await fs.mkdir(path.dirname(tgt), { recursive: true });
    const content = JSON.stringify({ version: 1, files: ['a.wasm'] });
    await fs.writeFile(src, content);
    await fs.writeFile(tgt, content);
    await expect(manifestsMatch(src, tgt)).resolves.toBe(true);
  });

  it('returns false when manifests differ', async () => {
    const root = await makeTree();
    const src = path.join(root, 'src', 'manifest.json');
    const tgt = path.join(root, 'tgt', 'manifest.json');
    await fs.mkdir(path.dirname(src), { recursive: true });
    await fs.mkdir(path.dirname(tgt), { recursive: true });
    await fs.writeFile(src, '{"version":1}');
    await fs.writeFile(tgt, '{"version":2}');
    await expect(manifestsMatch(src, tgt)).resolves.toBe(false);
  });

  it('returns false when target manifest is missing', async () => {
    const root = await makeTree();
    const src = path.join(root, 'src', 'manifest.json');
    const tgt = path.join(root, 'tgt', 'manifest.json');
    await fs.mkdir(path.dirname(src), { recursive: true });
    await fs.writeFile(src, '{"version":1}');
    await expect(manifestsMatch(src, tgt)).resolves.toBe(false);
  });
});

describe('syncEmceptionCdn', () => {
  it('first run copies the whole dir and reports fresh', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":1}');
    await fs.writeFile(path.join(src, 'bundle.tar.br'), 'bytes');

    const logs: string[] = [];
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: (m) => logs.push(m) });

    expect(result).toEqual({ copied: true, reason: 'fresh' });
    expect(await fs.readFile(path.join(tgt, 'manifest.json'), 'utf8')).toBe('{"version":1}');
    expect(await fs.readFile(path.join(tgt, 'bundle.tar.br'), 'utf8')).toBe('bytes');
    expect(logs.join('\n')).toContain('copied');
  });

  it('second run with matching manifest is a no-op (up-to-date)', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":1}');
    await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    const logs: string[] = [];
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: (m) => logs.push(m) });

    expect(result).toEqual({ copied: false, reason: 'up-to-date' });
    expect(logs.join('\n')).toContain('up to date');
  });

  it('diverged target manifest triggers a re-copy', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":2}');
    await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":3}');
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    expect(result).toEqual({ copied: true, reason: 're-copied' });
    expect(await fs.readFile(path.join(tgt, 'manifest.json'), 'utf8')).toBe('{"version":3}');
  });
});
