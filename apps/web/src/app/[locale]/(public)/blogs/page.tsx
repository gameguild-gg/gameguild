import type { Metadata } from 'next';
import Link from 'next/link';

import { BlogPostCard } from '@/components/blogs/blog-post-card';
import { BLOG_INDEX_PATH } from '@/lib/blogs/seo';
import { getBlogIndex } from '@/lib/blogs/queries';

export const metadata: Metadata = {
  title: 'Blog',
  description: 'Posts from the GameGuild community — tutorials, devlogs, and deep dives from working game developers.',
  robots: { index: true, follow: true },
};

interface BlogIndexPageProps {
  readonly searchParams: Promise<{ before?: string; beforeId?: string }>;
}

function cursorFromParams(searchParams: { before?: string; beforeId?: string }): { beforePublishedAt: string; beforeId: string } | undefined {
  return searchParams.before && searchParams.beforeId
    ? { beforePublishedAt: searchParams.before, beforeId: searchParams.beforeId }
    : undefined;
}

export default async function BlogIndexPage({ searchParams }: BlogIndexPageProps) {
  const params = await searchParams;
  const page = await getBlogIndex(cursorFromParams(params));
  const items = page?.items ?? [];
  const last = items[items.length - 1];
  const nextCursor = page?.hasMore && last ? `?before=${encodeURIComponent(last.publishedAt ?? '')}&beforeId=${encodeURIComponent(last.id ?? '')}` : null;

  return (
    <main className="min-h-screen bg-[#070a12] text-white">
      <section className="border-b border-white/10">
        <div className="container mx-auto flex flex-col gap-4 px-4 py-16">
          <h1 className="text-4xl font-semibold tracking-tight md:text-5xl">Blog</h1>
          <p className="max-w-2xl text-lg text-slate-300">
            Tutorials, devlogs, and deep dives from the GameGuild community.
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
                  href={`${BLOG_INDEX_PATH}${nextCursor}`}
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
