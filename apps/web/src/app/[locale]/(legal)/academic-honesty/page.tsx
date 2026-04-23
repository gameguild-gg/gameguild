import React from 'react';

// TODO: Restore real markdown content once @/components/markdown-renderer is ported and honesty.md is migrated.
export default async function Page({ }: PageProps<'/[locale]/academic-honesty'>): Promise<React.JSX.Element> {
    return (
        <main className="flex-1 container mx-auto px-4 py-8">
            <div className="max-w-4xl mx-auto">
                <div className="bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100 rounded-lg p-6 shadow-sm">
                    <h1 className="text-3xl font-bold mb-4">Academic Honesty</h1>
                    <p className="text-muted-foreground">This page is under reconstruction.</p>
                </div>
            </div>
        </main>
    );
}
