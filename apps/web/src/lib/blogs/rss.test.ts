import { describe, expect, it, vi } from 'vitest';
import { authorRssResponse, buildAuthorRssXml } from './rss';

vi.mock('@/lib/blogs/queries', () => ({
  getAuthorPosts: vi.fn(async () => ({
    items: [
      {
        slug: 'hello-world',
        title: 'Hello & <World>',
        excerpt: 'First post',
        publishedAt: '2026-09-01T00:00:00Z',
      },
    ],
    hasMore: false,
  })),
}));

describe('buildAuthorRssXml', () => {
  it('builds a valid RSS 2.0 document with escaped titles and absolute links', async () => {
    const xml = await buildAuthorRssXml('alice');

    expect(xml).toContain('<?xml version="1.0" encoding="UTF-8"?>');
    expect(xml).toContain('<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">');
    expect(xml).toContain('<title>alice — GameGuild Blog</title>');
    expect(xml).toContain('<title>Hello &amp; &lt;World&gt;</title>');
    expect(xml).toMatch(/<guid isPermaLink="true">https?:\/\/[^<]+\/blogs\/alice\/hello-world<\/guid>/);
    expect(xml).toContain('<pubDate>Tue, 01 Sep 2026 00:00:00 GMT</pubDate>');
    expect(xml).toContain('rel="self" type="application/rss+xml"');
  });

  it('serves with the RSS content type and cache headers', () => {
    const response = authorRssResponse('<rss />');

    expect(response.headers.get('Content-Type')).toBe('application/rss+xml; charset=utf-8');
    expect(response.headers.get('Cache-Control')).toBe('public, s-maxage=3600, stale-while-revalidate=86400');
  });
});
