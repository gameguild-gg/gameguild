import React from 'react';

// TODO: Restore full roadmap page once @/components/contributors/project-roadmap is ported.
export default async function Page({ }: PageProps<'/[locale]/about/roadmap'>): Promise<React.JSX.Element> {
    return (
        <div className="min-h-screen bg-background p-6">
            <div className="max-w-7xl mx-auto text-center">
                <h1 className="text-5xl font-bold text-foreground mb-4">Development Roadmap</h1>
                <p className="text-muted-foreground text-xl max-w-4xl mx-auto leading-relaxed">
                    This page is under reconstruction.
                </p>
            </div>
        </div>
    );
}
