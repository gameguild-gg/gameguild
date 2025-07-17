import React from 'react';
import { Contributor } from '@/lib/contributors';
import { ContributorLeaderboardCard } from './contributor-leaderboard-card';

interface TopContributorsSectionProps {
  contributors: Contributor[];
  topCount?: number;
}

export const TopContributorsSection: React.FC<TopContributorsSectionProps> = ({ contributors, topCount = 3 }) => {
  const topContributors = contributors.slice(0, topCount);

  return (
    <div className="mb-12">
      {/* Section Header */}
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold bg-gradient-to-r from-yellow-400 to-orange-400 bg-clip-text text-transparent mb-4">Top Contributors</h2>
        <p className="text-slate-400 text-lg">Our most dedicated community members</p>
      </div>

      {/* Top 3 Contributors - Podium Layout */}
      <div className="flex items-end justify-center gap-6 mb-8 relative">
        {/* 2nd Place - Left */}
        {topContributors[1] && (
          <div className="transform translate-y-8">
            <ContributorLeaderboardCard key={topContributors[1].login} contributor={topContributors[1]} rank={2} showMedal={true} />
          </div>
        )}
        
        {/* 1st Place - Center (Elevated) */}
        {topContributors[0] && (
          <div className="transform -translate-y-4 z-10 scale-110">
            <ContributorLeaderboardCard key={topContributors[0].login} contributor={topContributors[0]} rank={1} showMedal={true} />
          </div>
        )}
        
        {/* 3rd Place - Right */}
        {topContributors[2] && (
          <div className="transform translate-y-8">
            <ContributorLeaderboardCard key={topContributors[2].login} contributor={topContributors[2]} rank={3} showMedal={true} />
          </div>
        )}
      </div>
    </div>
  );
};
