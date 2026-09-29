import { describe, expect, it, vi } from 'vitest';

import type { BlogPostSummaryPage } from '@/lib/blogs/types';

const getAuthorPosts = vi.hoisted(() => vi.fn());

vi.mock('@/lib/blogs/queries', () => ({
  getAuthorPosts: (...args: unknown[]) => getAuthorPosts(...args),
}));

const { GET } = await import('./route');

function summaryPage(posts: Array<Record<string, unknown>>): BlogPostSummaryPage {
  return { items: posts as BlogPostSummaryPage['items'], hasMore: false };
}

function parseXmlItems(xml: string): string[] {
  const matches = xml.match(/<item>/g);
  return matches ?? [];
}

describe('blogs rss.xml route handler', () => {
  it('returns parseable application/rss+xml XML with one item', async () => {
    getAuthorPosts.mockResolvedValue(
      summaryPage([
        {
          id: 'post-1',
          title: 'Hello <World> & "Friends"',
          slug: 'hello-world',
          primaryAuthorHandle: 'alice',
          excerpt: 'First "post" excerpt',
          publishedAt: '2026-01-15T10:00:00Z',
        },
      ]),
    );

    const response = await GET(new Request('https://gameguild.gg/blogs/alice/rss.xml'), {
      params: Promise.resolve({ username: 'alice' }),
    });

    expect(response.headers.get('Content-Type')).toBe('application/rss+xml; charset=utf-8');
    const xml = await response.text();
    expect(() => new DOMParser().parseFromString(xml, 'text/xml')).not.toThrow();
    expect(xml).toContain('<rss version="2.0"');
    expect(xml).toContain('<title>Hello &lt;World&gt; &amp; &quot;Friends&quot;</title>');
    expect(xml).toContain('<link>https://gameguild.gg/blogs/alice/hello-world</link>');
    expect(xml).toContain('<guid isPermaLink="true">https://gameguild.gg/blogs/alice/hello-world</guid>');
    expect(xml).toContain('<description>First &quot;post&quot; excerpt</description>');
    expect(xml).toContain('<pubDate>Thu, 15 Jan 2026 10:00:00 GMT</pubDate>');
    expect(parseXmlItems(xml)).toHaveLength(1);
  });

  it('emits an empty channel when the author has no posts', async () => {
    getAuthorPosts.mockResolvedValue(summaryPage([]));

    const response = await GET(new Request('https://gameguild.gg/blogs/alice/rss.xml'), {
      params: Promise.resolve({ username: 'alice' }),
    });

    const xml = await response.text();
    expect(xml).toContain('<channel>');
    expect(parseXmlItems(xml)).toHaveLength(0);
  });
});
