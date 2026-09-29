import { describe, expect, it } from 'vitest';

import { buildBlogAbsoluteCanonicalUrl, buildBlogCanonicalUrl, isReservedBlogSegment } from './seo';

describe('buildBlogCanonicalUrl', () => {
  it('builds unprefixed canonical paths (no locale prefix, no trailing slash)', () => {
    expect(buildBlogCanonicalUrl('alice', 'hello-world')).toBe('/blogs/alice/hello-world');
  });

  it('builds absolute URLs from a base without double slashes', () => {
    expect(buildBlogAbsoluteCanonicalUrl('https://gameguild.gg/', 'alice', 'hello')).toBe(
      'https://gameguild.gg/blogs/alice/hello',
    );
  });
});

describe('isReservedBlogSegment', () => {
  it('rejects the RSS route segment', () => {
    expect(isReservedBlogSegment('rss.xml')).toBe(true);
  });

  it('accepts ordinary handles', () => {
    expect(isReservedBlogSegment('alice')).toBe(false);
    expect(isReservedBlogSegment('alice-smith')).toBe(false);
  });
});
