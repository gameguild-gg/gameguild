'use client';

import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Eye, X } from 'lucide-react';
import Link from 'next/link';
import { useState, useTransition } from 'react';

import { dismissFeedItemAction, markFeedItemViewedAction } from '@/lib/feed/feed-actions';
import type { PersonalFeedItem } from '@/lib/feed/personalized-feed';

export function FeedUpdateCard({ item }: { item: PersonalFeedItem }): React.JSX.Element {
  const [dismissed, setDismissed] = useState(false);
  const [isPending, startTransition] = useTransition();

  if (dismissed) return <></>;

  function handleDismiss() {
    setDismissed(true);
    startTransition(async () => {
      await dismissFeedItemAction(item.id);
    });
  }

  function handleClick() {
    if (item.isViewed) return;
    void markFeedItemViewedAction(item.id);
  }

  return (
    <Card data-testid={`feed-update-${item.id}`} className="bg-card/80">
      <CardHeader className="flex-row items-start justify-between space-y-0 pb-3">
        <CardTitle className="text-base">
          {item.href ? (
            <Link href={item.href} onClick={handleClick} className="hover:underline">
              {item.title}
            </Link>
          ) : (
            item.title
          )}
        </CardTitle>
        <div className="flex items-center gap-1">
          {item.isViewed ? null : (
            <span className="size-2 rounded-full bg-sky-400" aria-label="Unread" />
          )}
          <Button
            type="button"
            variant="ghost"
            size="icon-sm"
            aria-label="Dismiss update"
            disabled={isPending}
            onClick={handleDismiss}
          >
            <X className="size-4" />
          </Button>
        </div>
      </CardHeader>
      <CardContent className="flex items-center justify-between gap-3 text-sm text-muted-foreground">
        <span className="inline-flex items-center gap-2">
          <Badge variant="secondary">{item.kind}</Badge>
          {item.createdAt ? new Date(item.createdAt).toLocaleDateString() : null}
        </span>
        {item.isViewed ? (
          <span className="inline-flex items-center gap-1 text-xs">
            <Eye className="size-3.5" aria-hidden="true" />
            Viewed
          </span>
        ) : null}
      </CardContent>
    </Card>
  );
}
