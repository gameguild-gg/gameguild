import { describe, expect, it } from 'vitest';

import {
  buildBlogAbsoluteCanonicalUrl,
  buildBlogCanonicalUrl,
  buildBlogJsonLd,
  buildBlogPostMetadata,
  isReservedBlogSegment,
  resolveBlogJsonLd,
  type BlogAuthorProfile,
} from './seo';
import type { BlogPostDetail } from './types';

const BASE_POST: BlogPostDetail = {
  id: 'post-1',
  slug: 'hello-world',
  primaryAuthorHandle: 'alice',
  primaryAuthorDisplayName: 'Alice Smith',
  coAuthorHandles: ['bob'],
  title: 'Hello World',
  excerpt: 'First post excerpt',
  publishedAt: '2026-01-15T10:00:00Z',
  updatedAt: '2026-01-16T12:00:00Z',
  format: 'Markdown',
};

const PROFILES: BlogAuthorProfile[] = [
  { handle: 'alice', displayName: 'Alice Smith' },
  { handle: 'bob', displayName: 'Bob Jones' },
];

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

describe('buildBlogPostMetadata', () => {
  it('falls back title/description to Title/Excerpt when SEO fields are empty', () => {
    const metadata = buildBlogPostMetadata(BASE_POST, PROFILES);

    expect(metadata.title).toBe('Hello World');
    expect(metadata.description).toBe('First post excerpt');
  });

  it('prefers MetaTitle and MetaDescription when present', () => {
    const metadata = buildBlogPostMetadata({ ...BASE_POST, metaTitle: 'SEO Title', metaDescription: 'SEO desc' }, PROFILES);

    expect(metadata.title).toBe('SEO Title');
    expect(metadata.description).toBe('SEO desc');
  });

  it('emits article openGraph with publishedTime, authors, and tags', () => {
    const metadata = buildBlogPostMetadata({ ...BASE_POST, tags: ['gamedev', 'csharp'] }, PROFILES);

    expect(metadata.openGraph?.type).toBe('article');
    expect((metadata.openGraph as { publishedTime?: string }).publishedTime).toBe('2026-01-15T10:00:00Z');
    expect(metadata.openGraph?.authors).toEqual(['Alice Smith', 'Bob Jones']);
    expect(metadata.openGraph?.tags).toEqual(['gamedev', 'csharp']);
  });

  it('defaults the twitter card to summary_large_image', () => {
    const metadata = buildBlogPostMetadata(BASE_POST, PROFILES);

    expect(metadata.twitter?.card).toBe('summary_large_image');
  });

  it('honors a stored twitter card value', () => {
    const metadata = buildBlogPostMetadata({ ...BASE_POST, twitterCard: 'summary' }, PROFILES);

    expect(metadata.twitter?.card).toBe('summary');
  });

  it('uses the absolute unprefixed canonical URL (no locale, no trailing slash)', () => {
    const metadata = buildBlogPostMetadata(BASE_POST, PROFILES);

    expect(metadata.alternates?.canonical).toBe('https://gameguild.gg/blogs/alice/hello-world');
    expect(metadata.alternates?.languages).toEqual({
      'en-US': 'https://gameguild.gg/blogs/alice/hello-world',
      'pt-BR': 'https://gameguild.gg/pt-BR/blogs/alice/hello-world',
    });
  });
});

describe('blog JSON-LD', () => {
  it('builds a BlogPosting with Person authors, keywords, publisher, and dateModified', () => {
    const jsonLd = buildBlogJsonLd({ ...BASE_POST, tags: ['a', 'b'] }, PROFILES);

    expect(jsonLd['@type']).toBe('BlogPosting');
    expect(jsonLd.headline).toBe('Hello World');
    expect(jsonLd.author).toEqual([
      { '@type': 'Person', name: 'Alice Smith' },
      { '@type': 'Person', name: 'Bob Jones' },
    ]);
    expect(jsonLd.keywords).toBe('a, b');
    expect(jsonLd.datePublished).toBe('2026-01-15T10:00:00Z');
    expect(jsonLd.dateModified).toBe('2026-01-16T12:00:00Z');
    expect((jsonLd.publisher as { '@type': string })['@type']).toBe('Organization');
  });

  it('returns exactly the stored override when StructuredDataOverride is present', () => {
    const override = '{"@type":"CustomThing"}';
    const payload = resolveBlogJsonLd({ ...BASE_POST, structuredDataOverride: override }, PROFILES);

    expect(payload).toBe(override);
  });

  it('generates BlogPosting JSON when no override exists', () => {
    const payload = resolveBlogJsonLd(BASE_POST, PROFILES);

    expect(JSON.parse(payload)).toMatchObject({ '@type': 'BlogPosting', headline: 'Hello World' });
  });
});
