/**
 * Single source of truth for blog URLs (metadata, RSS, sitemap, JSON-LD).
 *
 * Canonical rule (plan todo 8, evidence `apps/web/src/proxy.ts:46-64`):
 * default locale `en-US` is externally unprefixed, so canonical URLs NEVER
 * carry a locale prefix. Metadata builders land in todo 8.
 */
export const BLOG_INDEX_PATH = '/blogs';

export function buildBlogCanonicalUrl(handle: string, slug: string): string {
  return `${BLOG_INDEX_PATH}/${handle}/${slug}`;
}

export function buildBlogAbsoluteCanonicalUrl(baseUrl: string, handle: string, slug: string): string {
  return `${baseUrl.replace(/\/$/, '')}${buildBlogCanonicalUrl(handle, slug)}`;
}

/**
 * Next static routes shadow `[username]`; handles colliding with them must
 * never reach the dynamic route. `rss.xml` is reserved by the per-author RSS
 * route at `blogs/[username]/rss.xml`.
 */
const RESERVED_BLOG_SEGMENTS = new Set(['rss.xml']);

export function isReservedBlogSegment(username: string): boolean {
  return RESERVED_BLOG_SEGMENTS.has(username);
}

export const BLOG_API_BASE_URL = (
  process.env.API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '');
