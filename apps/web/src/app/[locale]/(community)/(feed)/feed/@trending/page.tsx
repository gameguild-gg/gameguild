import React from 'react';

// TODO: Wire to "Trending" feed query (popular posts in a recent time window).
export default async function TrendingSlot(): Promise<React.JSX.Element> {
    return (
        <section aria-label="Trending feed" className="space-y-4">
            <h2 className="text-xl font-semibold">Trending</h2>
            <p className="text-muted-foreground text-sm">Trending posts will appear here.</p>
        </section>
    );
}
