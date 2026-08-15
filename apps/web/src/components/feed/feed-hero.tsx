import React from 'react';

export function FeedHero(): React.JSX.Element {
  return (
    <section className="border-b border-white/10 bg-[linear-gradient(180deg,#0f172a,#020617)]">
      <div className="mx-auto w-full max-w-7xl px-4 py-16 sm:px-6 lg:px-8">
        <h1 className="text-5xl font-semibold tracking-tight">Community feed</h1>
        <p className="mt-5 max-w-2xl text-lg leading-8 text-slate-300">
          Follow public project activity, testing sessions, and member updates from the GameGuild community.
        </p>
      </div>
    </section>
  );
}
