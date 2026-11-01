/**
 * Legacy Ghost blog host replacement (issues #91/#96): `/blog` deep links never had an
 * author segment. The index redirects permanently to the internal blog surface; individual
 * slugs resolve through the published-only legacy resolution endpoint and 308 to the
 * canonical internal route. Redirect responses carry X-Robots-Tag: noindex so the
 * intermediate hop is never indexed — crawlers consolidate on the internal URL.
 */

import { assertSafeServiceUrl } from '@/lib/security/safe-remote-url';

export const dynamic = 'force-dynamic';

const NOINDEX_HEADERS: Record<string, string> = { 'X-Robots-Tag': 'noindex' };

const apiBaseUrl = (
  process.env.API_URL ||
  process.env.NEXT_PUBLIC_API_URL ||
  'http://localhost:8080'
).replace(/\/$/, '');

const LEGACY_RESOLVE_TIMEOUT_MS = Number(process.env.PUBLIC_BLOG_API_TIMEOUT_MS ?? 10_000);

interface LegacyResolution {
  handle?: string | null;
  slug?: string | null;
}

export async function resolveLegacyBlogSlug(slug: string): Promise<LegacyResolution | null> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), LEGACY_RESOLVE_TIMEOUT_MS);
  try {
    const response = await fetch(
      assertSafeServiceUrl(
        `${apiBaseUrl}/api/social/blog/public/resolve-legacy/${encodeURIComponent(slug)}`,
        apiBaseUrl,
      ),
      { signal: controller.signal, cache: 'no-store', redirect: 'error' },
    );
    if (!response.ok) return null;
    return (await response.json().catch(() => null)) as LegacyResolution | null;
  } catch {
    return null;
  } finally {
    clearTimeout(timeout);
  }
}

export function legacyRedirect(target: string): Response {
  return new Response(null, {
    status: 308,
    headers: { Location: target, ...NOINDEX_HEADERS },
  });
}

export function legacyNotFound(): Response {
  return new Response('Not Found', { status: 404, headers: NOINDEX_HEADERS });
}
