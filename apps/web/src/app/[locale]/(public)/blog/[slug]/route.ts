import { legacyNotFound, legacyRedirect, resolveLegacyBlogSlug } from '@/lib/blogs/legacy-redirect';

export const dynamic = 'force-dynamic';

/**
 * Legacy Ghost slug redirect (issues #91/#96): `/blog/<ghost-slug>` resolves the published
 * post carrying that slug and 308-redirects to the canonical internal route
 * `/blogs/<handle>/<slug>`. Static sibling routes (`/blog/new`) are served by their pages.
 */
export async function GET(_request: Request, { params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  if (!slug) {
    return legacyNotFound();
  }

  const resolution = await resolveLegacyBlogSlug(decodeURIComponent(slug));
  if (!resolution?.handle || !resolution?.slug) {
    return legacyNotFound();
  }

  return legacyRedirect(
    `/blogs/${encodeURIComponent(resolution.handle)}/${encodeURIComponent(resolution.slug)}`,
  );
}
