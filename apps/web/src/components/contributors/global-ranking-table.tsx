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
    <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 backdrop-blur-sm rounded-lg overflow-hidden shadow-lg">
      <div className="p-6">
        <h2 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Global Ranking</h2>
      </div>

      {/* Table Header */}
      <div className="grid grid-cols-6 gap-4 p-4 bg-slate-800/50 text-sm text-slate-400 font-medium uppercase tracking-wide">
        <div>Rank</div>
        <div>Contributor</div>
        <div>Commits</div>
        <div>Additions</div>
        <div>Deletions</div>
        <div>Total Changes</div>
      </div>

      {/* Table Body */}
      <div className="divide-y divide-slate-700/30">
        {remainingContributors.map((contributor, index) => {
          const rank = index + 4; // Start from 4th position
          const commits = contributor.total_commits || 0;
          const additions = contributor.additions || 0;
          const deletions = contributor.deletions || 0;
          const totalChanges = additions + deletions;

          return (
            <div key={contributor.login} className="grid grid-cols-6 gap-4 p-4 items-center hover:bg-slate-800/30 transition-colors">
              {/* Rank */}
              <div className="flex items-center gap-3">
                <div className="w-8 h-8 bg-gradient-to-br from-slate-700 to-slate-800 rounded-full flex items-center justify-center text-white font-bold text-sm">{rank}</div>
              </div>

              {/* User */}
              <div className="flex items-center gap-3">
                <Image src={contributor.avatar_url} alt={contributor.name || contributor.login} width={32} height={32} className="rounded-full" />
                <div>
                  <div className="text-white font-medium">{contributor.name || contributor.login}</div>
                  <div className="text-slate-400 text-sm">@{contributor.login}</div>
                </div>
              </div>

              {/* Commits */}
              <div className="text-blue-400 font-medium">{numberToAbbreviation(commits)}</div>

              {/* Additions */}
              <div className="text-green-400 font-medium">+{numberToAbbreviation(additions)}</div>

              {/* Deletions */}
              <div className="text-red-400 font-medium">-{numberToAbbreviation(deletions)}</div>

              {/* Total Changes */}
              <div className="text-purple-400 font-medium">{numberToAbbreviation(totalChanges)}</div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
