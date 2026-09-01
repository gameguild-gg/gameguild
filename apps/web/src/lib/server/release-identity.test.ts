import { describe, expect, it } from 'vitest';
import { readReleaseIdentity } from './release-identity';

describe('readReleaseIdentity', () => {
  it('reads immutable build and runtime deployment metadata', () => {
    expect(
      readReleaseIdentity({
        VERSION: '2.4.0',
        RELEASE_SHA: 'release-sha',
        SOURCE_TREE: 'tree-sha',
        IMAGE_DIGEST: 'sha256:image',
        BUILT_AT: '2026-09-01T12:00:00Z',
        DEPLOYED_AT: '2026-09-01T12:05:00Z',
      }),
    ).toEqual({
      version: '2.4.0',
      releaseSha: 'release-sha',
      sourceTree: 'tree-sha',
      imageDigest: 'sha256:image',
      builtAt: '2026-09-01T12:00:00Z',
      deployedAt: '2026-09-01T12:05:00Z',
    });
  });

  it('uses explicit unknown values when deployment metadata is absent', () => {
    expect(readReleaseIdentity({})).toEqual({
      version: 'Unknown',
      releaseSha: 'Unknown',
      sourceTree: 'Unknown',
      imageDigest: 'Unknown',
      builtAt: 'Unknown',
      deployedAt: 'Unknown',
    });
  });

  it('uses the image version variable when no runtime override is present', () => {
    expect(readReleaseIdentity({ GAMEGUILD_VERSION: '4.3.0' }).version).toBe('4.3.0');
  });
});
