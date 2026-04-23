import React from 'react';

// TODO: Restore full contributors page once @/components/contributors/* and @/lib/integrations/github are ported.
export default async function Page({ }: PageProps<'/[locale]/about/contributors'>): Promise<React.JSX.Element> {
    return (
        <div className="min-h-screen flex flex-col">
            <main className="flex-1 container mx-auto px-4 py-8">
                <h1 className="text-4xl font-bold mb-6">Contributors</h1>
                <p className="text-muted-foreground">This page is under reconstruction.</p>
            </main>
        </div>
    );
}
