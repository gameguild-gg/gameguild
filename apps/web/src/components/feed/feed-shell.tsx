import { FeedCard } from '@/components/feed/feed-card';
import { FeedTabs, type FeedTab } from '@/components/feed/feed-tabs';
import { FeedUpdateCard } from '@/components/feed/feed-update-card';
import { FeaturedProjects } from '@/components/feed/featured-projects';
import { UpcomingPlaytests } from '@/components/feed/upcoming-playtests';
import { getCommunityFeed, type CommunityFeedItem, type CommunityFeedKind } from '@/lib/community/queries/members';
import { getPersonalizedFeed, type PersonalFeedItem } from '@/lib/feed/personalized-feed';
import { LockKeyhole } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

type Stream =
  | { kind: 'personal'; items: PersonalFeedItem[] }
  | { kind: 'community'; items: CommunityFeedItem[] }
  | { kind: 'signin'; items: [] };

async function loadStream(tab: FeedTab): Promise<Stream> {
  if (tab === 'foryou') {
    const feed = await getPersonalizedFeed(12);
    return { kind: 'personal', items: feed.items };
  }

  const feed = await getCommunityFeed(tab as CommunityFeedKind, { take: 12 });
  if (feed.requiresSignIn) return { kind: 'signin', items: [] };
  return { kind: 'community', items: feed.items };
}

/**
 * Instagram-style social feed for `/`: sticky tab bar (For You via the
 * personalized feed API, Following/Discover/Trending via the social feed)
 * over a single-column card stream, with a suggestions rail on wide screens.
 */
export async function FeedShell({ tab = 'foryou' }: { tab?: FeedTab }): Promise<React.JSX.Element> {
  const stream = await loadStream(tab);

  return (
    <div className="mx-auto flex w-full max-w-5xl items-start gap-10 px-4 sm:px-6">
      <div className="mx-auto w-full max-w-md min-w-0 flex-1 py-6">
        <FeedTabs active={tab} />
        {stream.kind === 'signin' ? (
          <SignInPrompt />
        ) : stream.items.length === 0 ? (
          <EmptyState tab={tab} />
        ) : stream.kind === 'personal' ? (
          <div className="space-y-4">
            {stream.items.map((item) => (
              <FeedUpdateCard key={item.id} item={item} />
            ))}
          </div>
        ) : (
          <div className="space-y-4">
            {stream.items.map((item) => (
              <FeedCard key={item.id} item={item} />
            ))}
          </div>
        )}
      </div>

      <aside className="hidden w-80 shrink-0 space-y-5 py-8 xl:block">
        <UpcomingPlaytests />
        <FeaturedProjects />
      </aside>
    </div>
  );
}

function SignInPrompt(): React.JSX.Element {
  return (
    <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed p-10 text-center">
      <LockKeyhole className="size-6 text-muted-foreground" aria-hidden="true" />
      <p className="text-sm font-medium">Sign in to load this feed</p>
      <p className="text-sm text-muted-foreground">Follow members and creators to build a personalized feed.</p>
      <Link href="/sign-in" className="mt-2 text-sm font-semibold text-primary hover:underline">
        Go to sign in
      </Link>
    </div>
  );
}

function EmptyState({ tab }: { tab: FeedTab }): React.JSX.Element {
  const copy: Record<FeedTab, string> = {
    foryou: 'Your personalized updates will appear here as activity happens.',
    following: 'Follow members and creators to fill this feed.',
    discover: 'Recommended community updates will appear here as activity is generated.',
    trending: 'Popular community updates will appear here when they gain traction.',
  };
  return <p className="rounded-2xl border border-dashed p-10 text-center text-sm text-muted-foreground">{copy[tab]}</p>;
}
