import { FeedUpdateCard } from '@/components/feed/feed-update-card';
import { MessageSquare } from 'lucide-react';
import React from 'react';

import { getPersonalizedFeed } from '@/lib/feed/personalized-feed';

export async function LatestUpdates(): Promise<React.JSX.Element> {
  const feed = await getPersonalizedFeed(10);

  return (
    <div className="space-y-4">
      <div className="mb-2 flex items-center gap-3">
        <MessageSquare className="size-5 text-sky-200" aria-hidden="true" />
        <h2 className="text-2xl font-semibold">Latest updates</h2>
      </div>

      {feed.items.length === 0 ? (
        <p className="rounded-3xl border border-white/10 bg-slate-900/70 p-6 text-sm text-slate-400">
          Your personalized updates will appear here as courses, discussions, and community activity happen.
        </p>
      ) : (
        <div className="space-y-3">
          {feed.items.map((item) => (
            <FeedUpdateCard key={item.id} item={item} />
          ))}
        </div>
      )}
    </div>
  );
}
