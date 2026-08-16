import { FeedTabs, type FeedTab } from '@/components/feed/feed-tabs';
import { FeaturedProjects } from '@/components/feed/featured-projects';
import { InfinitePostFeed } from '@/components/feed/infinite-post-feed';
import { UpcomingPlaytests } from '@/components/feed/upcoming-playtests';
import { loadPosts, POSTS_PAGE_SIZE, type PostsStream } from '@/lib/posts/queries';
import { LockKeyhole } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

const TAB_STREAM: Record<FeedTab, PostsStream> = {
  foryou: 'feed',
  following: 'feed',
  discover: 'public',
  trending: 'trending',
};

/**
 * Instagram-style social feed for `/`: sticky tabs over an infinite-scrolling
 * stream of member posts (SSR first page, IntersectionObserver pagination),
 * with a suggestions rail on wide screens.
 */
export async function FeedShell({ tab = 'foryou' }: { tab?: FeedTab }): Promise<React.JSX.Element> {
  const items = await loadPosts(TAB_STREAM[tab], 0);
  const nextSkip = items.length === POSTS_PAGE_SIZE ? POSTS_PAGE_SIZE : null;
  const personal = tab === 'foryou' || tab === 'following';

  return (
    <div className="mx-auto flex w-full max-w-5xl items-start gap-10 px-4 sm:px-6">
      <div className="mx-auto w-full max-w-md min-w-0 flex-1 py-6">
        <FeedTabs active={tab} />
        {personal && items.length === 0 ? (
          <SignedOutEmpty />
        ) : (
          <InfinitePostFeed stream={TAB_STREAM[tab]} initialItems={items} initialNextSkip={nextSkip} />
        )}
      </div>

      <aside className="hidden w-80 shrink-0 space-y-5 py-8 xl:block">
        <UpcomingPlaytests />
        <FeaturedProjects />
      </aside>
    </div>
  );
}

function SignedOutEmpty(): React.JSX.Element {
  return (
    <div className="flex flex-col items-center gap-2 rounded-2xl border border-dashed p-10 text-center">
      <LockKeyhole className="size-6 text-muted-foreground" aria-hidden="true" />
      <p className="text-sm font-medium">Your feed is building</p>
      <p className="text-sm text-muted-foreground">
        Follow members and creators so their posts land here.
      </p>
      <Link href="/console/community/members/users" className="mt-2 text-sm font-semibold text-primary hover:underline">
        Find members to follow
      </Link>
    </div>
  );
}
