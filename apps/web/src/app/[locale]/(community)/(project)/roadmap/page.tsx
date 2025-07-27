import React from 'react';
import { Metadata } from 'next';
import { ProjectRoadmap } from '@/components/contributors/project-roadmap';
import Link from 'next/link';

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: {
      template: '%s | Roadmap',
      default: 'Roadmap',
    },
    description: 'Game Guild development roadmap - track our progress and upcoming features',
    openGraph: {
      title: 'Game Guild Roadmap',
      description: 'Explore our development journey and future plans for the gaming community platform',
      type: 'website',
    },
  };
}

export default function RoadmapPage(): React.JSX.Element {
  return (
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-7xl mx-auto">
        {/* Page Header */}
        <div className="text-center mb-12">
          <h1 className="text-5xl font-bold text-foreground mb-4">Development Roadmap</h1>
          <p className="text-muted-foreground text-xl max-w-4xl mx-auto leading-relaxed">
            Follow our journey from initial concept to the premier gaming community platform. Each milestone represents months of hard work, community feedback,
            and innovative features.
          </p>
        </div>

        {/* Roadmap Component */}
        <ProjectRoadmap />

        {/* Additional Info */}
        <div className="mt-16 text-center">
          <div className="bg-card rounded-lg border border-border p-8">
            <h2 className="text-2xl font-bold text-foreground mb-4">Want to Contribute?</h2>
            <p className="text-muted-foreground mb-6 max-w-2xl mx-auto">
              Help us build the future of gaming communities. Every contribution, whether code, feedback, or ideas, helps shape our roadmap.
            </p>
            <div className="flex flex-col sm:flex-row gap-4 justify-center">
              <Link
                href="/contributors"
                className="inline-flex items-center gap-2 px-6 py-3 bg-primary text-primary-foreground rounded-lg font-medium hover:bg-primary/90 transition-colors"
              >
                <span>👥</span>
                View Contributors
              </Link>
              <Link
                href="https://github.com/gameguild-gg/website"
                target="_blank"
                rel="noopener noreferrer"
                className="inline-flex items-center gap-2 px-6 py-3 bg-background border border-border text-foreground rounded-lg font-medium hover:bg-accent hover:text-accent-foreground transition-colors"
              >
                <span>🌟</span>
                Star on GitHub
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
