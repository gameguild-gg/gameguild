import { afterEach, describe, expect, it, vi } from 'vitest';
import { GET } from './route';

describe('GET /api/health', () => {
  afterEach(() => vi.unstubAllEnvs());

  it('returns the exact release identity in the body and response header', async () => {
    vi.stubEnv('VERSION', '2.4.0');
    vi.stubEnv('RELEASE_SHA', 'release-sha');
    vi.stubEnv('SOURCE_TREE', 'tree-sha');
    vi.stubEnv('IMAGE_DIGEST', 'sha256:image');
    vi.stubEnv('BUILT_AT', '2026-09-01T12:00:00Z');
    vi.stubEnv('DEPLOYED_AT', '2026-09-01T12:05:00Z');

    const response = await GET(new Request('http://localhost/api/health'));

    expect(response.status).toBe(200);
    expect(response.headers.get('X-GameGuild-Release-Sha')).toBe('release-sha');
    await expect(response.json()).resolves.toMatchObject({
      service: 'web',
      version: '2.4.0',
      releaseSha: 'release-sha',
      sourceTree: 'tree-sha',
      imageDigest: 'sha256:image',
      builtAt: '2026-09-01T12:00:00Z',
      deployedAt: '2026-09-01T12:05:00Z',
    });
  });
});
