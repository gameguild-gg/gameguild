import { promises as fs } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { afterEach, describe, expect, it, vi } from 'vitest';
// Guard logic lives in the script itself (node runs it directly, so it must be
// plain ESM); vitest can import .mjs via a relative path regardless of its
// `src/**` include pattern (include only governs test discovery).
import { manifestsMatch, syncEmceptionCdn, JSDELIVR_MANIFEST_URL } from '../../../scripts/sync-emception-cdn.mjs';

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

describe('syncEmceptionCdn — tier 1 (local WASM build, real fs)', () => {
  it('first run copies the whole dir and reports tier 1 synced', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":1}');
    await fs.writeFile(path.join(src, 'bundle.tar.br'), 'bytes');

    const logs: string[] = [];
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: (m) => logs.push(m) });

    expect(result).toEqual({ tier: 1, action: 'synced', source: src });
    expect(await fs.readFile(path.join(tgt, 'manifest.json'), 'utf8')).toBe('{"version":1}');
    expect(await fs.readFile(path.join(tgt, 'bundle.tar.br'), 'utf8')).toBe('bytes');
    expect(logs.join('\n')).toContain('copied');
  });

  it('second run with matching manifest is a no-op (tier 1 skip)', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":1}');
    await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    const logs: string[] = [];
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: (m) => logs.push(m) });

    expect(result).toEqual({ tier: 1, action: 'skip', source: src });
    expect(logs.join('\n')).toContain('up to date');
  });

  it('diverged target manifest triggers a re-copy (tier 1 synced)', async () => {
    const root = await makeTree();
    const src = path.join(root, 'cdn');
    const tgt = path.join(root, 'public', 'emception');
    await fs.mkdir(src);
    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":2}');
    await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    await fs.writeFile(path.join(src, 'manifest.json'), '{"version":3}');
    const result = await syncEmceptionCdn({ srcDir: src, tgtDir: tgt, log: () => {} });

    expect(result).toEqual({ tier: 1, action: 'synced', source: src });
    expect(await fs.readFile(path.join(tgt, 'manifest.json'), 'utf8')).toBe('{"version":3}');
  });
});

describe('syncEmceptionCdn — tier 2 (npm tarball, mocked fetch + extract)', () => {
  it('falls through to npm fetch when local src is missing', async () => {
    const root = await makeTree();
    const srcMissing = path.join(root, 'no-such-src');
    const tgt = path.join(root, 'public', 'emception');
    const npmCache = path.join(root, 'npm-cache');
    // Pre-create the cached cdn so fetchNpmTarball short-circuits as a cache hit.
    const cachedCdn = path.join(npmCache, 'package', 'cdn');
    await fs.mkdir(cachedCdn, { recursive: true });
    await fs.writeFile(path.join(cachedCdn, 'manifest.json'), '{"version":1,"npm":true}');

    // fetch should never be called for tier 2 in the cache-hit path; if it is,
    // fail the test by throwing.
    const fetchImpl = vi.fn(async () => { throw new Error('fetch should not be called (cache hit)'); });

    const logs: string[] = [];
    const result = await syncEmceptionCdn({
      srcDir: srcMissing,
      tgtDir: tgt,
      npmCacheDir: npmCache,
      log: (m) => logs.push(m),
      fetchImpl,
    });

    expect(result.tier).toBe(2);
    expect(result.action).toBe('synced');
    expect(await fs.readFile(path.join(tgt, 'manifest.json'), 'utf8')).toBe('{"version":1,"npm":true}');
    expect(logs.join('\n')).toContain('cache hit');
  });

  it('tier 2 failure (registry 500) falls through to tier 3 when jsDelivr reachable', async () => {
    const root = await makeTree();
    const srcMissing = path.join(root, 'no-such-src');
    const tgt = path.join(root, 'public', 'emception');
    const npmCache = path.join(root, 'npm-cache');

    const fetchImpl = vi.fn(async (url: string) => {
      if (url === 'https://registry.npmjs.org/emception/latest') return { ok: false, status: 500 };
      if (url === JSDELIVR_MANIFEST_URL) return { ok: true };
      throw new Error(`unexpected fetch ${url}`);
    });

    const logs: string[] = [];
    const result = await syncEmceptionCdn({
      srcDir: srcMissing,
      tgtDir: tgt,
      npmCacheDir: npmCache,
      log: (m) => logs.push(m),
      fetchImpl,
    });

    expect(result.tier).toBe(3);
    expect(result.action).toBe('jsdelivr');
    expect(result.url).toBe(JSDELIVR_MANIFEST_URL);
    expect(logs.join('\n')).toContain('jsDelivr');
  });
});

describe('syncEmceptionCdn — tier 3 (jsDelivr fallback) and exhaustion', () => {
  it('throws when local + npm + jsDelivr all unavailable', async () => {
    const root = await makeTree();
    const srcMissing = path.join(root, 'no-such-src');
    const tgt = path.join(root, 'public', 'emception');
    const npmCache = path.join(root, 'npm-cache');

    const fetchImpl = vi.fn(async (url: string) => {
      if (url === 'https://registry.npmjs.org/emception/latest') return { ok: false, status: 500 };
      if (url === JSDELIVR_MANIFEST_URL) return { ok: false, status: 503 };
      throw new Error(`unexpected fetch ${url}`);
    });

    await expect(
      syncEmceptionCdn({
        srcDir: srcMissing,
        tgtDir: tgt,
        npmCacheDir: npmCache,
        log: () => {},
        fetchImpl,
      }),
    ).rejects.toThrow(/No emception CDN source available/);
  });
});
