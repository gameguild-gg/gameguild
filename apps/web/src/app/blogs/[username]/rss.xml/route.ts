import { isReservedBlogSegment } from '@/lib/blogs/seo';
import { authorRssResponse, buildAuthorRssXml } from '@/lib/blogs/rss';

export const revalidate = 3600;

export async function GET(_request: Request, { params }: { params: Promise<{ username: string }> }) {
  const { username } = await params;

  if (!username || isReservedBlogSegment(username)) {
    return new Response('Not Found', { status: 404 });
  }

  const xml = await buildAuthorRssXml(decodeURIComponent(username));
  return authorRssResponse(xml);
}
