import type { PostCardData } from '@/lib/posts/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { MessageCircle, Pin } from 'lucide-react';
import Image from 'next/image';
import React from 'react';

function initials(name: string | null, authorId: string): string {
  const source = name?.trim() || authorId.replace(/[-_]+/g, ' ');
  if (!source) return 'GG';
  const parts = source.split(/\s+/).filter(Boolean);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

function timeAgo(iso: string): string {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '';
  const minutes = Math.round((Date.now() - then) / 60_000);
  if (minutes < 60) return `${Math.max(1, minutes)}m`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  if (days < 7) return `${days}d`;
  return new Date(then).toLocaleDateString();
}

/** Instagram-style post card: author row, square media, caption, engagement counts. */
export function PostCard({ post }: { post: PostCardData }): React.JSX.Element {
  const authorLabel = post.authorName?.trim() || post.authorId.slice(0, 8);

  return (
    <article data-testid="post-card" className="overflow-hidden rounded-2xl border bg-card shadow-sm">
      <header className="flex items-center gap-3 px-4 py-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-400 to-violet-500 text-xs font-bold text-white">
          {initials(post.authorName, post.authorId)}
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold">{authorLabel}</p>
          <p className="text-xs text-muted-foreground">{timeAgo(post.createdAt)}</p>
        </div>
        {post.likesCount > 0 ? <Badge variant="secondary">♥ {post.likesCount}</Badge> : null}
      </header>

      {post.mediaUrl ? (
        <div className="relative aspect-square w-full bg-muted">
          <Image
            src={post.mediaUrl}
            alt=""
            fill
            className="object-cover"
            sizes="(min-width: 1280px) 28rem, 100vw"
          />
        </div>
      ) : null}

      <div className="flex items-start gap-2 px-4 py-3">
        {post.mediaType === 'Image' ? null : <Pin className="mt-0.5 size-3.5 shrink-0 text-muted-foreground" aria-hidden="true" />}
        <p className="whitespace-pre-wrap break-words text-sm leading-snug">{post.content}</p>
      </div>

      {post.commentsCount > 0 ? (
        <p className="flex items-center gap-1.5 px-4 pb-3 text-xs text-muted-foreground">
          <MessageCircle className="size-3.5" aria-hidden="true" />
          {post.commentsCount} comments
        </p>
      ) : null}
    </article>
  );
}
