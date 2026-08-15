import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowRight, BookOpen, LockKeyhole, Radio } from 'lucide-react';
import Image from 'next/image';
import React from 'react';
import { Link } from '@/i18n/navigation';
import { type CommunityFeedKind, getCommunityFeed } from '@/lib/community/queries/members';

const FEED_LABELS: Record<CommunityFeedKind, string> = {
  following: 'Following',
  discover: 'Discover',
  trending: 'Trending',
};

const EMPTY_COPY: Record<CommunityFeedKind, string> = {
  following: 'Follow members and creators to build a personalized community feed.',
  discover: 'Recommended courses and community updates will appear here as activity is generated.',
  trending: 'Popular courses and community updates will appear here when they gain traction.',
};

export async function FeedSection({ kind }: { kind: CommunityFeedKind }): Promise<React.JSX.Element> {
  const feed = await getCommunityFeed(kind);

  return (
    <section aria-label={`${FEED_LABELS[kind]} feed`} className="space-y-4">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-xl font-semibold">{FEED_LABELS[kind]}</h2>
        <Badge variant="outline" className="gap-1">
          <Radio className="size-3" />
          Live API
        </Badge>
      </div>

      {feed.requiresSignIn ? (
        <Card className="border-dashed">
          <CardContent className="flex min-h-32 flex-col items-center justify-center gap-2 text-center text-sm text-muted-foreground">
            <LockKeyhole className="size-5" />
            Sign in to load this personalized feed.
          </CardContent>
        </Card>
      ) : feed.items.length === 0 ? (
        <Card className="border-dashed">
          <CardContent className="flex min-h-32 items-center justify-center text-center text-sm text-muted-foreground">{EMPTY_COPY[kind]}</CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {feed.items.map((item, index) => (
            <Card key={item.id} className="overflow-hidden bg-card/80">
              {item.imageUrl ? (
                <div className="relative h-36 border-b bg-muted">
                  <Image
                    src={item.imageUrl}
                    alt=""
                    fill
                    className="object-cover"
                    loading={index === 0 ? 'eager' : 'lazy'}
                    sizes="(min-width: 768px) 33vw, 100vw"
                  />
                  <div className="absolute inset-0 bg-gradient-to-t from-background/75 via-transparent to-transparent" />
                </div>
              ) : null}
              <CardHeader className="space-y-2 pb-3">
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="text-base">
                    {item.href ? (
                      <Link href={item.href} className="hover:underline">
                        {item.title}
                      </Link>
                    ) : (
                      item.title
                    )}
                  </CardTitle>
                  <Badge variant="secondary">{item.reason}</Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                {item.summary ? <p className="line-clamp-3 leading-6">{item.summary}</p> : <p>Content ID: {item.contentId}</p>}
                <div className="flex items-center justify-between gap-3 pt-1">
                  <span className="inline-flex items-center gap-1 text-xs">
                    <BookOpen className="size-3.5" />
                    {item.contentType}
                  </span>
                  {item.href ? (
                    <Link href={item.href} className="inline-flex items-center gap-1 text-xs font-semibold text-foreground hover:underline">
                      {item.actionLabel ?? 'Open'}
                      <ArrowRight className="size-3.5" />
                    </Link>
                  ) : (
                    <span className="text-xs">Relevance: {item.relevanceScore.toFixed(1)}</span>
                  )}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </section>
  );
}
