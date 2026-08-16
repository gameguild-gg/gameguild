'use client';

import { Loader2 } from 'lucide-react';
import React from 'react';

import { PostCard } from '@/components/feed/post-card';
import { loadPostsAction } from '@/lib/posts/actions';
import type { PostCardData, PostsStream } from '@/lib/posts/queries';

/**
 * Infinite-scrolling post stream: renders the SSR page, then appends pages
 * when the sentinel enters the viewport until the stream is exhausted.
 */
export function InfinitePostFeed({
  stream,
  initialItems,
  initialNextSkip,
}: {
  stream: PostsStream;
  initialItems: PostCardData[];
  initialNextSkip: number | null;
}): React.JSX.Element {
  const [items, setItems] = React.useState(initialItems);
  const [nextSkip, setNextSkip] = React.useState<number | null>(initialNextSkip);
  const [loading, setLoading] = React.useState(false);
  const sentinelRef = React.useRef<HTMLDivElement | null>(null);
  const seenRef = React.useRef<Set<string>>(new Set(initialItems.map((item) => item.id)));
  const nextSkipRef = React.useRef<number | null>(initialNextSkip);
  const loadingRef = React.useRef(false);

  React.useEffect(() => {
    setItems(initialItems);
    setNextSkip(initialNextSkip);
    nextSkipRef.current = initialNextSkip;
    seenRef.current = new Set(initialItems.map((item) => item.id));
  }, [initialItems, initialNextSkip]);

  React.useEffect(() => {
    const node = sentinelRef.current;
    if (!node) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (!entries[0]?.isIntersecting) return;
        if (loadingRef.current || nextSkipRef.current === null) return;

        const skip = nextSkipRef.current;
        loadingRef.current = true;
        setLoading(true);
        void loadPostsAction(stream, skip)
          .then(({ items: page, nextSkip: more }) => {
            const fresh = page.filter((item) => !seenRef.current.has(item.id));
            fresh.forEach((item) => seenRef.current.add(item.id));
            if (fresh.length > 0) {
              setItems((current) => [...current, ...fresh]);
            }
            const hasNext = fresh.length > 0 && more !== null;
            nextSkipRef.current = hasNext ? more : null;
            setNextSkip(nextSkipRef.current);
          })
          .finally(() => {
            loadingRef.current = false;
            setLoading(false);
          });
      },
      { rootMargin: '600px 0px' },
    );
    observer.observe(node);
    return () => observer.disconnect();
  }, [stream]);

  return (
    <div className="space-y-4">
      {items.map((post) => (
        <PostCard key={post.id} post={post} />
      ))}

      {items.length === 0 && !loading ? (
        <p className="rounded-2xl border border-dashed p-10 text-center text-sm text-muted-foreground">
          No posts here yet — be the first to share something.
        </p>
      ) : null}

      <div ref={sentinelRef} aria-hidden="true" className="h-px" />

      {loading ? (
        <p className="flex items-center justify-center gap-2 py-4 text-sm text-muted-foreground">
          <Loader2 className="size-4 animate-spin" aria-hidden="true" />
          Loading more…
        </p>
      ) : nextSkip === null && items.length > 0 ? (
        <p className="py-4 text-center text-xs text-muted-foreground">You&apos;re all caught up.</p>
      ) : null}
    </div>
  );
}
