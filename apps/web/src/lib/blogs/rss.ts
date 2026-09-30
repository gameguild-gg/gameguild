import { buildBlogAuthorAbsoluteUrl, buildBlogAbsoluteCanonicalUrl, BLOG_SITE_BASE_URL } from '@/lib/blogs/seo';
import { getAuthorPosts } from '@/lib/blogs/queries';

export const RSS_ITEM_LIMIT = 20;

function xmlEscape(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&apos;');
}

function rfc822(iso: string | null | undefined): string {
  if (!iso) return '';
  const date = new Date(iso);
  return Number.isNaN(date.getTime()) ? '' : date.toUTCString();
}

export async function buildAuthorRssXml(handle: string): Promise<string> {
  const page = await getAuthorPosts(handle);
  const items = (page?.items ?? []).slice(0, RSS_ITEM_LIMIT);
  const authorUrl = buildBlogAuthorAbsoluteUrl(BLOG_SITE_BASE_URL, handle);

  const itemXml = items
    .map((post) => {
      const link = buildBlogAbsoluteCanonicalUrl(BLOG_SITE_BASE_URL, handle, post.slug ?? '');
      const pubDate = rfc822(post.publishedAt);
      return [
        '    <item>',
        `      <title>${xmlEscape(post.title ?? '')}</title>`,
        `      <link>${xmlEscape(link)}</link>`,
        `      <guid isPermaLink="true">${xmlEscape(link)}</guid>`,
        `      <description>${xmlEscape(post.excerpt ?? '')}</description>`,
        pubDate ? `      <pubDate>${pubDate}</pubDate>` : null,
        '    </item>',
      ]
        .filter((line): line is string => line !== null)
        .join('\n');
    })
    .join('\n');

  return [
    '<?xml version="1.0" encoding="UTF-8"?>',
    '<rss version="2.0" xmlns:atom="http://www.w3.org/2005/Atom">',
    '  <channel>',
    `    <title>${xmlEscape(`${handle} — GameGuild Blog`)}</title>`,
    `    <link>${xmlEscape(authorUrl)}</link>`,
    `    <description>${xmlEscape(`Latest posts by ${handle} on GameGuild`)}</description>`,
    `    <atom:link href="${xmlEscape(`${authorUrl}/rss.xml`)}" rel="self" type="application/rss+xml" />`,
    itemXml,
    '  </channel>',
    '</rss>',
  ].join('\n');
}

export function authorRssResponse(xml: string): Response {
  return new Response(xml, {
    headers: {
      'Content-Type': 'application/rss+xml; charset=utf-8',
      'Cache-Control': 'public, s-maxage=3600, stale-while-revalidate=86400',
    },
  });
}
