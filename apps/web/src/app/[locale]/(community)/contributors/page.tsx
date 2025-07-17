import React from 'react';
import { Metadata } from 'next';
import { getContributors } from '@/lib/contributors';
import { ContributorsHeader, TopContributorsSection, GlobalRankingTable, HowToContribute } from '@/components/contributors';

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
    <div className="min-h-screen bg-background p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header with stats */}
        <ContributorsHeader totalContributors={totalContributors} totalParticipated={totalParticipated} />

        {/* Top 3 Contributors */}
        <TopContributorsSection contributors={contributors} />

        {/* Global Ranking Table */}
        <GlobalRankingTable contributors={contributors} />

        {/* How to Contribute Section */}
        <HowToContribute />
      </div>
    </div>
  );
}
