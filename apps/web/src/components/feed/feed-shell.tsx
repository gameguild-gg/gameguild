import React from 'react';

import { FeedHero } from '@/components/feed/feed-hero';
import { FeedSection } from '@/components/feed/feed-section';
import { FeaturedProjects } from '@/components/feed/featured-projects';
import { LatestUpdates } from '@/components/feed/latest-updates';
import { UpcomingPlaytests } from '@/components/feed/upcoming-playtests';

/**
 * Member-facing feed shell for `/`. Composes the static feed rail (hero,
 * latest updates, playtests, featured projects) with the personalized
 * sections (following / discover / trending) from the community feed API.
 */
export function FeedShell(): React.JSX.Element {
  return (
    <main className="bg-slate-950 text-white">
      <FeedHero />

      <section className="mx-auto grid w-full max-w-7xl gap-8 px-4 py-14 sm:px-6 lg:grid-cols-[1fr_0.8fr] lg:px-8">
        <LatestUpdates />

        <aside className="space-y-5">
          <UpcomingPlaytests />
          <FeaturedProjects />
        </aside>
      </section>

      <section className="mx-auto w-full max-w-7xl px-4 pb-14 sm:px-6 lg:px-8">
        <div className="grid gap-6 md:grid-cols-3">
          <FeedSection kind="following" />
          <FeedSection kind="discover" />
          <FeedSection kind="trending" />
        </div>
      </section>
    </main>
  );
}
