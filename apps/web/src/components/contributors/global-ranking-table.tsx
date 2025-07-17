import React from 'react';
import { Contributor } from '@/lib/contributors/types';
import Image from 'next/image';
import { numberToAbbreviation } from '@/lib/utils';

interface GlobalRankingTableProps {
  contributors: Contributor[];
}

export const GlobalRankingTable: React.FC<GlobalRankingTableProps> = ({ contributors }) => {
  // Start from position 4 since top 3 are shown above
  const remainingContributors = contributors.slice(3);

  return (
    <div className="bg-card rounded-lg border border-border overflow-hidden">
      <div className="p-6 border-b border-border">
        <h2 className="text-2xl font-bold text-foreground">Global Ranking</h2>
      </div>

      {/* Table Header */}
      <div className="grid grid-cols-6 gap-4 p-4 bg-muted text-sm text-muted-foreground font-medium uppercase tracking-wide">
        <div>Rank</div>
        <div>Contributor</div>
        <div>Commits</div>
        <div>Additions</div>
        <div>Deletions</div>
        <div>Total Changes</div>
      </div>

      {/* Table Body */}
      <div className="divide-y divide-border">
        {remainingContributors.map((contributor, index) => {
          const rank = index + 4; // Start from 4th position
          const commits = contributor.total_commits || 0;
          const additions = contributor.additions || 0;
          const deletions = contributor.deletions || 0;
          const totalChanges = additions + deletions;

          return (
            <div key={contributor.login} className="grid grid-cols-6 gap-4 p-4 items-center hover:bg-muted/50 transition-colors">
              {/* Rank */}
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 bg-muted rounded-full flex items-center justify-center text-foreground font-bold text-sm">{rank}</div>
              </div>

              {/* User */}
              <div className="flex items-center gap-3">
                <Image src={contributor.avatar_url} alt={contributor.name || contributor.login} width={32} height={32} className="rounded-full" />
                <div>
                  <div className="text-foreground font-medium">{contributor.name || contributor.login}</div>
                  <div className="text-muted-foreground text-xs">@{contributor.login}</div>
                </div>
              </div>

              {/* Commits */}
              <div className="text-foreground font-medium">{numberToAbbreviation(commits)}</div>

              {/* Additions */}
              <div className="text-green-600 dark:text-green-400 font-medium">+{numberToAbbreviation(additions)}</div>

              {/* Deletions */}
              <div className="text-red-600 dark:text-red-400 font-medium">-{numberToAbbreviation(deletions)}</div>

              {/* Total Changes */}
              <div className="text-foreground font-medium">{numberToAbbreviation(totalChanges)}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
