import type { PostCardData } from '@/lib/posts/queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Heart, MessageCircle, Pin, Share2 } from 'lucide-react';
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

const TOKEN_PATTERN = /(#[\p{L}\p{N}_]+|@[\p{L}\p{N}_]+)/gu;

/** Renders caption text with hashtags and mentions highlighted. */
function Caption({ text }: { text: string }): React.JSX.Element {
  const nodes: React.ReactNode[] = [];
  let lastIndex = 0;
  let key = 0;

  for (const match of text.matchAll(TOKEN_PATTERN)) {
    const index = match.index ?? 0;
    if (index > lastIndex) nodes.push(text.slice(lastIndex, index));
    nodes.push(
      <span key={`token-${key++}`} className="font-semibold text-primary">
        {match[0]}
      </span>,
    );
    lastIndex = index + match[0].length;
  }
  if (lastIndex < text.length) nodes.push(text.slice(lastIndex));

  return <p className="whitespace-pre-wrap break-words text-sm leading-snug">{nodes}</p>;
}

function EngagementStat({
  icon,
  count,
  label,
}: {
  icon: React.ReactNode;
  count: number;
  label: string;
}): React.JSX.Element {
  return (
    <span className="inline-flex items-center gap-1.5 text-sm text-muted-foreground transition-colors hover:text-foreground" title={label}>
      {icon}
      {count > 0 ? <span className="tabular-nums">{count}</span> : null}
      <span className="sr-only">{label}</span>
    </span>
  );
}

/** Instagram-style post card: gradient avatar ring, media, highlighted caption, engagement row. */
export function PostCard({ post }: { post: PostCardData }): React.JSX.Element {
  const authorLabel = post.authorName?.trim() || post.authorId.slice(0, 8);

  return (
    <article data-testid="post-card" className="overflow-hidden rounded-2xl border bg-card shadow-sm transition-shadow hover:shadow-md">
      <header className="flex items-center gap-3 px-4 py-3">
        <span className="rounded-full bg-gradient-to-br from-amber-400 via-rose-500 to-violet-600 p-[2px]">
          <span className="flex size-9 items-center justify-center rounded-full border-2 border-card bg-muted text-xs font-bold text-foreground">
            {initials(post.authorName, post.authorId)}
          </span>
        </span>
        <div className="min-w-0 flex-1">
          <p className="flex items-center gap-1.5 truncate text-sm font-semibold">
            {authorLabel}
            {post.isPinned ? <Pin className="size-3 shrink-0 text-primary" aria-label="Pinned" /> : null}
          </p>
          <p className="text-xs text-muted-foreground">
            {timeAgo(post.createdAt)}
            {post.isEdited ? ' · edited' : ''}
          </p>
        </div>
        {post.isPinned ? <Badge variant="secondary">Pinned</Badge> : null}
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

      <div className="px-4 pt-3">
        <Caption text={post.content} />
      </div>

      <footer className="flex items-center gap-5 px-4 py-3">
        <EngagementStat icon={<Heart className="size-4" aria-hidden="true" />} count={post.likesCount} label={`${post.likesCount} likes`} />
        <EngagementStat icon={<MessageCircle className="size-4" aria-hidden="true" />} count={post.commentsCount} label={`${post.commentsCount} comments`} />
        <EngagementStat icon={<Share2 className="size-4" aria-hidden="true" />} count={post.sharesCount} label={`${post.sharesCount} shares`} />
      </footer>
    </article>
  );
}
