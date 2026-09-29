import type { Metadata } from 'next';
import Link from 'next/link';
import { notFound } from 'next/navigation';

import { BlogPostCard } from '@/components/blogs/blog-post-card';
import { BLOG_INDEX_PATH, buildBlogAuthorPath, isReservedBlogSegment } from '@/lib/blogs/seo';
import { getAuthorPosts } from '@/lib/blogs/queries';

interface AuthorPageProps {
  readonly params: Promise<{ username: string }>;
  readonly searchParams: Promise<{ before?: string; beforeId?: string }>;
}

function cursorFromParams(searchParams: { before?: string; beforeId?: string }): { beforePublishedAt: string; beforeId: string } | undefined {
  return searchParams.before && searchParams.beforeId
    ? { beforePublishedAt: searchParams.before, beforeId: searchParams.beforeId }
    : undefined;
}

export async function generateMetadata({ params }: AuthorPageProps): Promise<Metadata> {
  const { username } = await params;
  return {
    title: `${decodeURIComponent(username)} — posts`,
    description: `All published posts by ${decodeURIComponent(username)} on GameGuild.`,
    robots: { index: true, follow: true },
  };
}

export default async function AuthorBlogPage({ params, searchParams }: AuthorPageProps) {
  const [{ username }, query] = await Promise.all([params, searchParams]);

  if (isReservedBlogSegment(username)) {
    notFound();
  }

  const handle = decodeURIComponent(username);
  const page = await getAuthorPosts(handle, cursorFromParams(query));

  if (!page) {
    notFound();
  }

  const items = page.items;
  const last = items[items.length - 1];
  const nextCursor =
    page.hasMore && last ? `?before=${encodeURIComponent(last.publishedAt ?? '')}&beforeId=${encodeURIComponent(last.id ?? '')}` : null;

  return (
    <main className="min-h-screen bg-[#070a12] text-white">
      <section className="border-b border-white/10">
        <div className="container mx-auto flex flex-col gap-4 px-4 py-16">
          <p className="text-sm text-slate-400">
            <Link href={BLOG_INDEX_PATH} className="hover:text-slate-200">
              Blog
            </Link>
            <span aria-hidden="true"> / </span>
            <span className="text-slate-300">{handle}</span>
          </p>
          <h1 className="text-4xl font-semibold tracking-tight md:text-5xl">{handle}</h1>
          <p className="text-sm text-slate-400">
            <a href={`${buildBlogAuthorPath(handle)}/rss.xml`} className="hover:text-slate-200">
              RSS feed
            </a>
          </p>
        </div>
      </section>

      <section className="container mx-auto px-4 py-12">
        {items.length === 0 ? (
          <p className="text-slate-400">No posts published yet.</p>
        ) : (
          <>
            <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
              {items.map((post) => (
                <BlogPostCard key={post.id} post={post} />
              ))}
            </div>
            {nextCursor ? (
              <div className="mt-10 flex justify-center">
                <Link
                  href={`${buildBlogAuthorPath(handle)}${nextCursor}`}
                  className="rounded-full border border-white/15 bg-white/5 px-6 py-2 text-sm text-white transition-colors hover:bg-white/10"
                >
                  Older posts
                </Link>
              </div>
            ) : null}
          </>
        )}
      </section>
    </main>
  );
}
