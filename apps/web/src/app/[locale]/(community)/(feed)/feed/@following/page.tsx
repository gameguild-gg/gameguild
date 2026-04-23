import React from 'react';

// TODO: Wire to "Following" feed query (posts from users the viewer follows).
export default async function FollowingSlot(): Promise<React.JSX.Element> {
  return (
    <section aria-label="Following feed" className="space-y-4">
      <h2 className="text-xl font-semibold">Following</h2>
      <p className="text-muted-foreground text-sm">No posts yet from people you follow.</p>
    </section>
  );
}
