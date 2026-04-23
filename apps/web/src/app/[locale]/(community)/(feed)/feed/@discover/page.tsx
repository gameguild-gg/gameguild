import React from 'react';

// TODO: Wire to "Discover" feed query (recommendations / new content).
export default async function DiscoverSlot(): Promise<React.JSX.Element> {
    return (
        <section aria-label="Discover feed" className="space-y-4">
            <h2 className="text-xl font-semibold">Discover</h2>
            <p className="text-muted-foreground text-sm">Recommendations will appear here.</p>
        </section>
    );
}
