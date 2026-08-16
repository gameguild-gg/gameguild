import { Link } from '@/i18n/navigation';
import type { CommunityFeedItem } from '@/lib/community/queries/members';
import { Badge } from '@game-guild/ui/components/badge';
import { ArrowRight } from 'lucide-react';
import Image from 'next/image';
import React from 'react';

function authorInitials(authorId: string): string {
  const clean = authorId.replace(/[-_]+/g, ' ').trim();
  if (!clean) return 'GG';
  const parts = clean.split(/\s+/);
  return parts.length === 1 ? parts[0].slice(0, 2).toUpperCase() : `${parts[0][0]}${parts[1][0]}`.toUpperCase();
}

function relativeDate(iso: string): string {
  const then = new Date(iso).getTime();
  if (Number.isNaN(then)) return '';
  const diff = Date.now() - then;
  const minutes = Math.round(diff / 60_000);
  if (minutes < 60) return `${Math.max(1, minutes)}m`;
  const hours = Math.round(minutes / 60);
  if (hours < 24) return `${hours}h`;
  const days = Math.round(hours / 24);
  if (days < 7) return `${days}d`;
  return new Date(then).toLocaleDateString();
}

/** Social-stream card for a community feed item — Instagram post styling. */
export function FeedCard({ item }: { item: CommunityFeedItem }): React.JSX.Element {
  return (
    <article data-testid="feed-card" className="overflow-hidden rounded-2xl border bg-card shadow-sm">
      <header className="flex items-center gap-3 px-4 py-3">
        <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-gradient-to-br from-sky-400 to-violet-500 text-xs font-bold text-white">
          {authorInitials(item.authorId)}
        </span>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-semibold">{item.authorId}</p>
          <p className="truncate text-xs text-muted-foreground">
            {item.contentType}
            {item.createdAt ? ` · ${relativeDate(item.createdAt)}` : ''}
          </p>
        </div>
        <Badge variant="secondary">{item.reason}</Badge>
      </header>

      {item.imageUrl ? (
        <div className="relative aspect-square w-full bg-muted">
          <Image
            src={item.imageUrl}
            alt={item.title}
            fill
            className="object-cover"
            sizes="(min-width: 1280px) 28rem, 100vw"
          />
        </div>
      ) : null}

      <div className="space-y-1.5 px-4 py-3">
        {item.href ? (
          <Link href={item.href} className="text-sm font-semibold leading-snug hover:underline">
            {item.title}
          </Link>
        ) : (
          <h2 className="text-sm font-semibold leading-snug">{item.title}</h2>
        )}
        {item.summary ? <p className="line-clamp-2 text-sm text-muted-foreground">{item.summary}</p> : null}
        {item.href ? (
          <Link
            href={item.href}
            className="inline-flex items-center gap-1 pt-1 text-sm font-semibold text-primary hover:underline"
          >
            {item.actionLabel ?? 'Open'}
            <ArrowRight className="size-3.5" aria-hidden="true" />
          </Link>
        ) : null}
      </div>
    </article>
  );
}
