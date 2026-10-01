import { lazy, Suspense } from 'react';

import type { BlogPostDetail } from '@/lib/blogs/types';
import { resolveBlogJsonLd, type BlogAuthorProfile } from '@/lib/blogs/seo';
import { BlogViewBeacon } from '@/components/blogs/blog-view-beacon';

const MarkdownRenderer = lazy(async () => {
  const mod = await import('@game-guild/content-rendering');
  return { default: mod.MarkdownRenderer };
});

const LexicalSurface = lazy(async () => {
  const mod = await import('@game-guild/lexical-surface');
  return { default: mod.LexicalSurface };
});

function PostMarkdown({ content }: { content: string }) {
  return (
    <Suspense fallback={<div className="min-h-32 animate-pulse rounded-md bg-muted" />}>
      <MarkdownRenderer content={content} />
    </Suspense>
  );
}

function PostLexical({ content }: { content: string }) {
  return (
    <Suspense fallback={<div className="min-h-32 animate-pulse rounded-md bg-muted" />}>
      <LexicalContent content={content} />
    </Suspense>
  );
}

async function LexicalContent({ content }: { content: string }) {
  let initialState: unknown = null;
  try {
    initialState = JSON.parse(content);
  } catch {
    initialState = null;
  }

  const state =
    initialState && typeof initialState === 'object' && 'root' in initialState
      ? (initialState as import('react').ComponentProps<typeof LexicalSurface>['initialState'])
      : null;

  if (!state) {
    return <p className="text-sm text-muted-foreground">This post has no published content.</p>;
  }

  return (
    <LexicalSurface
      namespace="BlogPost"
      mountKey="blog-post-body"
      initialState={state}
      readOnly
      accessibleLabel="Blog post content"
      contentClassName="max-w-none"
      features={{ pageLayout: false }}
    />
  );
}

function formatPublishedDate(iso: string | null | undefined): string | null {
  if (!iso) return null;
  return new Date(iso).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' });
}

export function BlogPostView({ post, authorProfiles }: { post: BlogPostDetail; authorProfiles?: BlogAuthorProfile[] }) {
  const published = formatPublishedDate(post.publishedAt);
  const authors = [
    ...(post.primaryAuthorDisplayName ?? post.primaryAuthorHandle
      ? [post.primaryAuthorDisplayName ?? post.primaryAuthorHandle]
      : []),
    ...(post.coAuthorHandles ?? []),
  ].filter((n): n is string => Boolean(n));

  return (
    <article className="mx-auto max-w-3xl px-4 py-12">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: resolveBlogJsonLd(post, authorProfiles) }} />

      <header className="flex flex-col gap-4 border-b border-border pb-8">
        <h1 className="text-4xl font-semibold leading-tight tracking-tight text-foreground md:text-5xl">{post.title}</h1>
        <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
          {authors.length > 0 ? <span className="text-foreground/80">{authors.join(', ')}</span> : null}
          {published ? (
            <>
              <span aria-hidden="true">·</span>
              <time dateTime={post.publishedAt ?? undefined}>{published}</time>
            </>
          ) : null}
          {post.readTimeMinutes ? (
            <>
              <span aria-hidden="true">·</span>
              <span>{post.readTimeMinutes} min read</span>
            </>
          ) : null}
        </div>
      </header>

      <div className="prose max-w-none py-8 dark:prose-invert">
        {post.format === 'Lexical' && post.jsonBody ? (
          <PostLexical content={post.jsonBody} />
        ) : post.content ? (
          <PostMarkdown content={post.content} />
        ) : (
          <p className="text-sm text-muted-foreground">This post has no published content.</p>
        )}
      </div>

      {post.tags && post.tags.length > 0 ? (
        <footer>
          <ul className="flex flex-wrap gap-2">
            {post.tags.map((tag) => (
              <li key={tag} className="rounded-full border border-border bg-muted px-3 py-1 text-sm text-muted-foreground">
                {tag}
              </li>
            ))}
          </ul>
        </footer>
      ) : null}

      {post.id ? <BlogViewBeacon postId={post.id} /> : null}
    </article>
  );
}
