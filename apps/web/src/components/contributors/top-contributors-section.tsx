import React from 'react';
import { EnhancedContributor } from '@/lib/integrations/github';
import { ContributorLeaderboardCard } from './contributor-leaderboard-card';

interface TopContributorsSectionProps {
  contributors: EnhancedContributor[];
  topCount?: number;
}

export const TopContributorsSection: React.FC<TopContributorsSectionProps> = ({ contributors, topCount = 3 }) => {
  const topContributors = contributors.slice(0, topCount);

  return (
    <div className="mb-16 py-12">
      {/* Section Header */}
      <div className="text-center mb-16">
        <h2 className="text-3xl font-bold text-white mb-4">Top Contributors</h2>
        <p className="text-slate-400 text-lg">Our most dedicated community members</p>
      </div>

      {/* Top 3 Contributors - Podium Layout */}
      <div className="flex flex-col md:flex-row items-center md:items-end justify-center gap-6 md:gap-12 mb-12 relative min-h-[320px]">
        {/* 1st Place - First on mobile, Center on desktop */}
        {topContributors[0] && (
          <div className="transform translate-y-0 z-10 md:scale-110 md:order-2">
            <ContributorLeaderboardCard key={topContributors[0].login} contributor={topContributors[0]} rank={1} showMedal={true} />
          </div>
        )}

        {/* 2nd Place - Second on mobile, Left on desktop */}
        {topContributors[1] && (
          <div className="transform md:translate-y-16 md:order-1">
            <ContributorLeaderboardCard key={topContributors[1].login} contributor={topContributors[1]} rank={2} showMedal={true} />
          </div>
        )}

        {/* 3rd Place - Third on mobile, Right on desktop */}
        {topContributors[2] && (
          <div className="transform md:translate-y-16 md:order-3">
            <ContributorLeaderboardCard key={topContributors[2].login} contributor={topContributors[2]} rank={3} showMedal={true} />
          </div>
        )}
      </div>
    </div>
  );
};
