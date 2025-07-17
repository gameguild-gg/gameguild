import React from 'react';
import { Contributor } from '@/lib/contributors/types';
import Image from 'next/image';
import { numberToAbbreviation } from '@/lib/utils';

interface GlobalRankingTableProps {
  contributors: Contributor[];
}

export const GlobalRankingTable: React.FC<GlobalRankingTableProps> = ({ contributors }) => {
  // Show all contributors starting from position 1
  const allContributors = contributors;

  const getRankColorClasses = (rank: number) => {
    if (rank === 1) return {
      bg: 'bg-gradient-to-r from-yellow-500/20 to-orange-500/10',
      border: 'border-yellow-500/30',
      rankBg: 'bg-gradient-to-r from-yellow-500 to-orange-500',
      textColor: 'text-yellow-400'
    };
    if (rank === 2) return {
      bg: 'bg-gradient-to-r from-slate-500/20 to-slate-400/10',
      border: 'border-slate-400/30',
      rankBg: 'bg-gradient-to-r from-slate-500 to-slate-400',
      textColor: 'text-slate-400'
    };
    if (rank === 3) return {
      bg: 'bg-gradient-to-r from-amber-500/20 to-orange-500/10',
      border: 'border-amber-600/30',
      rankBg: 'bg-gradient-to-r from-amber-500 to-orange-500',
      textColor: 'text-amber-500'
    };
    return {
      bg: 'hover:bg-slate-800/30',
      border: '',
      rankBg: 'bg-gradient-to-br from-slate-700 to-slate-800',
      textColor: 'text-slate-300'
    };
  };

  return (
    <div className="bg-gradient-to-br from-slate-900/80 to-slate-800/80 backdrop-blur-sm rounded-xl overflow-hidden shadow-2xl border border-slate-700/50 relative">
      {/* Add depth with multiple shadows */}
      <div className="absolute inset-0 bg-gradient-to-br from-slate-800/20 to-slate-900/40 rounded-xl"></div>
      <div className="relative z-10">
        <div className="p-8">
          <h2 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Global Ranking</h2>
          <p className="text-slate-400 mt-2">Complete leaderboard of all contributors</p>
        </div>

        {/* Table Header */}
        <div className="grid grid-cols-6 gap-4 p-4 bg-slate-800/70 text-sm text-slate-400 font-medium uppercase tracking-wide border-b border-slate-700/50">
          <div>Rank</div>
          <div>Contributor</div>
          <div>Commits</div>
          <div>Additions</div>
          <div>Deletions</div>
          <div>Total Changes</div>
        </div>

        {/* Table Body */}
        <div className="divide-y divide-slate-700/30">
          {allContributors.map((contributor, index) => {
            const rank = index + 1;
            const commits = contributor.total_commits || 0;
            const additions = contributor.additions || 0;
            const deletions = contributor.deletions || 0;
            const totalChanges = additions + deletions;
            const colorClasses = getRankColorClasses(rank);

            return (
              <div 
                key={contributor.login} 
                className={`grid grid-cols-6 gap-4 p-4 items-center transition-all duration-300 ${colorClasses.bg} ${colorClasses.border ? `border-l-4 ${colorClasses.border}` : ''}`}
              >
                {/* Rank */}
                <div className="flex items-center gap-3">
                  <div className={`w-10 h-10 ${colorClasses.rankBg} rounded-full flex items-center justify-center text-white font-bold text-sm shadow-lg`}>
                    {rank}
                  </div>
                </div>

                {/* User */}
                <div className="flex items-center gap-3">
                  <div className="relative">
                    <Image 
                      src={contributor.avatar_url} 
                      alt={contributor.name || contributor.login} 
                      width={40} 
                      height={40} 
                      className="rounded-full ring-2 ring-slate-600/50" 
                    />
                    {rank <= 3 && (
                      <div className={`absolute -top-1 -right-1 w-4 h-4 ${colorClasses.rankBg} rounded-full border-2 border-slate-800`}></div>
                    )}
                  </div>
                  <div>
                    <div className={`${colorClasses.textColor} font-semibold`}>{contributor.name || contributor.login}</div>
                    <div className="text-slate-400 text-sm">@{contributor.login}</div>
                  </div>
                </div>

                {/* Commits */}
                <div className="text-blue-400 font-semibold">{numberToAbbreviation(commits)}</div>

                {/* Additions */}
                <div className="text-green-400 font-semibold">+{numberToAbbreviation(additions)}</div>

                {/* Deletions */}
                <div className="text-red-400 font-semibold">-{numberToAbbreviation(deletions)}</div>

                {/* Total Changes */}
                <div className="text-purple-400 font-semibold">{numberToAbbreviation(totalChanges)}</div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
