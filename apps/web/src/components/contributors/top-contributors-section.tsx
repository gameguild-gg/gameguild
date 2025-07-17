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
      {/* Top 3 Contributors */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        {topContributors.map((contributor, index) => (
          <ContributorLeaderboardCard key={contributor.login} contributor={contributor} rank={index + 1} showMedal={true} />
        ))}
      </div>
    </div>
  );
};
