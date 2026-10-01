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
  const authorLinks = [
    ...(post.primaryAuthorHandle
      ? [{ handle: post.primaryAuthorHandle, label: post.primaryAuthorDisplayName ?? post.primaryAuthorHandle }]
      : []),
    ...(post.coAuthorHandles ?? []).map((authorHandle) => ({ handle: authorHandle, label: authorHandle })),
  ].filter((a): a is { handle: string; label: string } => Boolean(a.handle && a.label));

  return (
    <article className="group relative flex h-full flex-col gap-3 rounded-2xl border border-border bg-card p-6 transition-colors hover:border-muted-foreground/40">
      <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
        {authorLinks.length > 0 ? (
          <span className="flex flex-wrap items-center gap-2">
            {authorLinks.map((author, index) => (
              <span key={author.handle} className="flex items-center gap-2">
                {index > 0 ? <span aria-hidden="true">,</span> : null}
                {/* z-10 keeps the author link clickable over the card's stretched-link overlay */}
                <Link href={`/social/profiles/${author.handle}`} className="z-10 font-medium text-foreground/80 underline-offset-4 hover:underline">
                  {author.label}
                </Link>
              </span>
            ))}
          </span>
        ) : null}
        {published ? <span aria-hidden="true">·</span> : null}
        {published ? <time dateTime={post.publishedAt ?? undefined}>{published}</time> : null}
        {post.readTimeMinutes ? (
          <>
            <span aria-hidden="true">·</span>
            <span>{post.readTimeMinutes} min read</span>
          </>
        ) : null}
      </div>

      <h2 className="text-xl font-semibold leading-snug tracking-tight text-foreground">
        <Link href={href} className="focus-visible:outline-none">
          <span className="absolute inset-0" aria-hidden="true" />
          {post.title}
        </Link>
      </h2>

      {post.excerpt ? <p className="line-clamp-3 text-sm leading-6 text-muted-foreground">{post.excerpt}</p> : null}

      {post.tags && post.tags.length > 0 ? (
        <ul className="mt-auto flex flex-wrap gap-2 pt-2">
          {post.tags.map((tag) => (
            <li key={tag} className="rounded-full border border-border bg-muted px-2.5 py-0.5 text-xs text-muted-foreground">
              {tag}
            </li>
          ))}
        </ul>
      ) : null}
    </article>
  );
}
