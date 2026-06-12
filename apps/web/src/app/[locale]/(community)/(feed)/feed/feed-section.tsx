import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { LockKeyhole, Radio } from 'lucide-react';
import React from 'react';
import { type CommunityFeedKind, getCommunityFeed } from '@/lib/community/queries/members';

const FEED_LABELS: Record<CommunityFeedKind, string> = {
  following: 'Following',
  discover: 'Discover',
  trending: 'Trending',
};

const EMPTY_COPY: Record<CommunityFeedKind, string> = {
  following: 'Follow members and creators to build a personalized community feed.',
  discover: 'Recommended community updates will appear here as activity is generated.',
  trending: 'Popular community updates will appear here when they gain traction.',
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
          <CardContent className="flex min-h-32 items-center justify-center text-center text-sm text-muted-foreground">
            {EMPTY_COPY[kind]}
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-3">
          {feed.items.map((item) => (
            <Card key={item.id} className="bg-card/80">
              <CardHeader className="space-y-2 pb-3">
                <div className="flex items-center justify-between gap-3">
                  <CardTitle className="text-base">{item.title}</CardTitle>
                  <Badge variant="secondary">{item.reason}</Badge>
                </div>
              </CardHeader>
              <CardContent className="space-y-2 text-sm text-muted-foreground">
                <p>Content ID: {item.contentId}</p>
                <p>Relevance: {item.relevanceScore.toFixed(1)}</p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </section>
  );
}
