import React from 'react';

export default async function Page(): Promise<React.JSX.Element> {
  return (
    <header className="space-y-2">
      <h1 className="text-3xl font-bold">Community feed</h1>
      <p className="max-w-2xl text-muted-foreground">
        Live social feed sections from followed members, recommendations, trending activity, and public course discovery.
      </p>
    </header>
  );
}
