import React from 'react';

export default async function Page({ }: PageProps<'/[locale]/feed'>): Promise<React.JSX.Element> {
  return (
    <header className="space-y-2">
      <h1 className="text-3xl font-bold">Community feed</h1>
      <p className="max-w-2xl text-muted-foreground">
        Live social feed sections from followed members, recommendations, and trending community activity.
      </p>
    </header>
  );
}
