import React from 'react';
import { Metadata } from 'next';
import { getContributors } from '@/lib/contributors';
import { ContributorsHeader } from '@/components/contributors/contributors-header';
import { TopContributorsSection } from '@/components/contributors/top-contributors-section';
import { GlobalRankingTable } from '@/components/contributors/global-ranking-table';
import { ProjectStats } from '@/components/contributors/project-stats';
import { HorizontalRoadmap } from '@/components/contributors/horizontal-roadmap';
import { HowToContribute } from '@/components/contributors/how-to-contribute';

export async function generateMetadata(): Promise<Metadata> {
  return {
    title: {
      template: '%s | Contributors',
      default: 'Contributors',
    },
    description: 'List of contributors to the Game Guild website',
    openGraph: {
      title: 'Game Guild Contributors',
      description: 'Meet the amazing people who contribute to Game Guild',
      type: 'website',
    },
  };
}

export default async function Page(): Promise<React.JSX.Element> {
  const contributors = await getContributors();

  // Mock data for demo - in real app this would come from your API
  const totalContributors = contributors.length;
  const totalParticipated = contributors.filter((c) => (c.total_commits || 0) > 0).length;

  return (
    <div className="min-h-screen bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      {/* Project Stats Overview - Full Width */}
      <ProjectStats />

      {/* Other sections with centered content */}
      <div className="max-w-7xl mx-auto px-6">
        {/* Header with stats */}
        <ContributorsHeader totalContributors={totalContributors} totalParticipated={totalParticipated} />

        {/* Top 3 Contributors */}
        <TopContributorsSection contributors={contributors} />

        {/* Global Ranking Table */}
        <div className="py-8">
          <GlobalRankingTable contributors={contributors} />
        </div>
      </div>

      {/* Interactive Roadmap - Full Width */}
      <HorizontalRoadmap />

      {/* How to Contribute Section - Full Width */}
      <HowToContribute />
    </div>
  );
}
