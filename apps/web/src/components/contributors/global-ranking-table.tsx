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
    if (rank === 1)
      return {
        bg: 'bg-gradient-to-r from-yellow-400/10 to-amber-300/5 hover:from-yellow-400/15 hover:to-amber-300/8',
        border: 'border-yellow-400/40',
        rankBg: 'bg-gradient-to-r from-yellow-400 to-amber-300',
        textColor: 'text-yellow-300',
      };
    if (rank === 2)
      return {
        bg: 'bg-gradient-to-r from-slate-300/10 to-gray-400/5 hover:from-slate-300/15 hover:to-gray-400/8',
        border: 'border-slate-300/40',
        rankBg: 'bg-gradient-to-r from-slate-300 to-gray-400',
        textColor: 'text-slate-300',
      };
    if (rank === 3)
      return {
        bg: 'bg-gradient-to-r from-orange-400/10 to-amber-500/5 hover:from-orange-400/15 hover:to-amber-500/8',
        border: 'border-orange-400/40',
        rankBg: 'bg-gradient-to-r from-orange-400 to-amber-500',
        textColor: 'text-orange-300',
      };
    return {
      bg: 'hover:bg-slate-800/30 hover:shadow-lg',
      border: '',
      rankBg: 'bg-gradient-to-br from-slate-700 to-slate-800',
      textColor: 'text-slate-300',
    };
  };

  return (
    <div className="bg-gradient-to-br from-slate-900/90 to-slate-800/90 backdrop-blur-sm rounded-xl overflow-hidden shadow-2xl border border-slate-700/70 relative">
      {/* Add depth with multiple shadows */}
      <div className="absolute inset-0 bg-gradient-to-br from-slate-800/30 to-slate-900/50 rounded-xl shadow-inner"></div>
      {/* Additional depth layer */}
      <div className="absolute inset-0 bg-gradient-to-t from-slate-900/20 to-transparent rounded-xl"></div>
      <div className="relative z-10">
        <div className="p-8">
          <h2 className="text-3xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Global Ranking</h2>
          <p className="text-slate-400 mt-2">Complete leaderboard of all contributors</p>
        </div>

        {/* Table Header */}
        <div className="grid grid-cols-[auto_1fr_auto] md:grid-cols-[0.5fr_2fr_1fr_1fr_1fr_1fr] gap-3 md:gap-6 px-4 md:px-6 py-3 bg-slate-800/70 text-sm text-slate-400 font-medium uppercase tracking-wide border-b border-slate-700/70">
          <div>Rank</div>
          <div>Contributor</div>
          <div className="text-right">Commits</div>
          <div className="text-right hidden md:block">Additions</div>
          <div className="text-right hidden md:block">Deletions</div>
          <div className="text-right hidden md:block">Total Changes</div>
        </div>

        {/* Table Body */}
        <div className="divide-y divide-slate-700/60">
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
                className={`grid grid-cols-[auto_1fr_auto] md:grid-cols-[0.5fr_2fr_1fr_1fr_1fr_1fr] gap-3 md:gap-6 px-4 md:px-6 py-3 items-center transition-all duration-300 cursor-pointer ${colorClasses.bg} ${colorClasses.border ? `border-l-4 ${colorClasses.border}` : ''}`}
              >
                {/* Rank */}
                <div className="flex items-center gap-3">
                  <div className={`w-10 h-10 ${colorClasses.rankBg} rounded-full flex items-center justify-center text-white font-bold text-sm shadow-lg`}>{rank}</div>
                </div>

                {/* User */}
                <div className="flex items-center gap-3">
                  <div className="relative">
                    <Image src={contributor.avatar_url || ''} alt={contributor.name || contributor.login || 'Contributor'} width={40} height={40} className="rounded-full ring-2 ring-slate-600/50" />
                    {rank <= 3 && <div className={`absolute -top-1 -right-1 w-4 h-4 ${colorClasses.rankBg} rounded-full border-2 border-slate-800`}></div>}
                  </div>
                  <div>
                    <div className={`${colorClasses.textColor} font-semibold`}>{contributor.name || contributor.login}</div>
                    <div className="text-slate-400 text-sm">@{contributor.login}</div>
                  </div>
                </div>

                {/* Commits */}
                <div className="text-blue-400 font-semibold text-right">{numberToAbbreviation(commits)}</div>

                {/* Additions */}
                <div className="text-green-400 font-semibold text-right hidden md:block">+{numberToAbbreviation(additions)}</div>

                {/* Deletions */}
                <div className="text-red-400 font-semibold text-right hidden md:block">-{numberToAbbreviation(deletions)}</div>

                {/* Total Changes */}
                <div className="text-purple-400 font-semibold text-right hidden md:block">{numberToAbbreviation(totalChanges)}</div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
