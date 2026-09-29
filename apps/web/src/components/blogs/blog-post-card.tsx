import Link from 'next/link';

import type { BlogPostSummary } from '@/lib/blogs/types';
import { buildBlogCanonicalPath } from '@/lib/blogs/seo';

function formatDate(iso: string | null | undefined): string | null {
  if (!iso) return null;
  return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
}

export function BlogPostCard({ post }: { post: BlogPostSummary }) {
  const handle = post.primaryAuthorHandle ?? '';
  const slug = post.slug ?? '';
  const href = buildBlogCanonicalPath(handle, slug);
  const published = formatDate(post.publishedAt);
  const authors = [
    ...(post.primaryAuthorDisplayName ?? post.primaryAuthorHandle ? [post.primaryAuthorDisplayName ?? post.primaryAuthorHandle] : []),
    ...(post.coAuthorHandles ?? []),
  ].filter((n): n is string => Boolean(n));

  return (
    <article className="group relative flex h-full flex-col gap-3 rounded-2xl border border-white/10 bg-white/[0.045] p-6 backdrop-blur transition-colors hover:border-white/20">
      <div className="flex flex-wrap items-center gap-2 text-sm text-slate-400">
        {authors.length > 0 ? <span className="text-slate-300">{authors.join(', ')}</span> : null}
        {published ? <span aria-hidden="true">·</span> : null}
        {published ? <time dateTime={post.publishedAt ?? undefined}>{published}</time> : null}
        {post.readTimeMinutes ? (
          <>
            <span aria-hidden="true">·</span>
            <span>{post.readTimeMinutes} min read</span>
          </>
        ) : null}
      </div>

      <h2 className="text-xl font-semibold leading-snug tracking-tight text-white">
        <Link href={href} className="focus-visible:outline-none">
          <span className="absolute inset-0" aria-hidden="true" />
          {post.title}
        </Link>
      </h2>

      {post.excerpt ? <p className="line-clamp-3 text-sm leading-6 text-slate-300">{post.excerpt}</p> : null}

      {post.tags && post.tags.length > 0 ? (
        <ul className="mt-auto flex flex-wrap gap-2 pt-2">
          {post.tags.map((tag) => (
            <li key={tag} className="rounded-full border border-white/10 bg-white/5 px-2.5 py-0.5 text-xs text-slate-300">
              {tag}
            </li>
          ))}
        </ul>
      ) : null}
    </article>
  );
}
