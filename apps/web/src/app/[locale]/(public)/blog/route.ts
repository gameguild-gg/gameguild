import { legacyRedirect } from '@/lib/blogs/legacy-redirect';

export const dynamic = 'force-dynamic';

/**
 * Legacy Ghost blog index redirect (issues #91/#96): `/blog` permanently redirects to the
 * internal blog index. The response is noindexed so the legacy path is never indexed.
 */
export function GET() {
  return legacyRedirect('/blogs');
}
