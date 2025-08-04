import { ContributorsHeader } from '@/components/contributors/contributors-header';
import { GitHubProjectStats } from '@/components/contributors/git-hub-project-stats';
import { GlobalRankingTable } from '@/components/contributors/global-ranking-table';
import { HorizontalRoadmap } from '@/components/contributors/horizontal-roadmap';
import { HowToContribute } from '@/components/contributors/how-to-contribute';
import { TopContributorsSection } from '@/components/contributors/top-contributors-section';
import { getContributors, getGitHubRepositoryData } from '@/lib/integrations/github';
import { Metadata } from 'next';
import React from 'react';

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
  const [contributors, repositoryData] = await Promise.all([getContributors(), getGitHubRepositoryData()]);

  // Calculate real stats from contributors data
  const totalContributors = contributors.length;
  const totalParticipated = contributors.filter((contributor) => (contributor.total_commits || 0) > 0).length;

  return (
    <div className="flex flex-col flex-1 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900">
      {/* Project Stats Overview - Full Width */}
      <GitHubProjectStats repositoryData={repositoryData} />

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
