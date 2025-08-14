import React from 'react';
import Image from 'next/image';
import { Card } from '@/components/ui/card';
import { EnhancedContributor } from '@/lib/integrations/github';
import { ContributorStats } from './contributor-stats';
import { numberToAbbreviation } from '@/lib/utils';
import { Award, Trophy } from 'lucide-react';

interface ContributorLeaderboardCardProps {
  contributor: EnhancedContributor;
  rank: number;
  showMedal?: boolean;
}

export const ContributorLeaderboardCard: React.FC<ContributorLeaderboardCardProps> = ({ contributor, rank, showMedal = false }) => {
  const getMedalIcon = (rank: number) => {
    if (rank === 1) return <Trophy className="w-8 h-8 text-yellow-400" />;
    if (rank === 2) return <Award className="w-8 h-8 text-slate-400" />;
    if (rank === 3) return <Award className="w-8 h-8 text-amber-600" />;
    return null;
  };

  const getBorderColor = (rank: number) => {
    if (rank === 1) return 'border-yellow-500/50';
    if (rank === 2) return 'border-slate-400/50';
    if (rank === 3) return 'border-amber-600/50';
    return 'border-slate-700/50';
  };

  const getGradientBg = (rank: number) => {
    if (rank === 1) return 'from-yellow-500/10 via-orange-500/5 to-yellow-600/10 hover:from-yellow-500/20 hover:via-orange-500/10 hover:to-yellow-600/20';
    if (rank === 2) return 'from-slate-500/10 via-slate-400/5 to-slate-600/10 hover:from-slate-500/20 hover:via-slate-400/10 hover:to-slate-600/20';
    if (rank === 3) return 'from-amber-500/10 via-orange-500/5 to-amber-600/10 hover:from-amber-500/20 hover:via-orange-500/10 hover:to-amber-600/20';
    return 'from-slate-900/50 to-slate-800/50 hover:from-slate-900/70 hover:to-slate-800/70';
  };

  const getBackgroundIconColor = (rank: number) => {
    if (rank === 1) return 'text-yellow-400';
    if (rank === 2) return 'text-slate-400';
    if (rank === 3) return 'text-amber-600';
    return 'text-slate-400';
  };

  return (
    <Card
      className={`w-80 bg-gradient-to-br ${getGradientBg(rank)} backdrop-blur-sm ${getBorderColor(rank)} border-2 p-4 relative overflow-hidden transition-all hover:scale-105 hover:shadow-xl hover:brightness-110 hover:shadow-white/10 shadow-lg`}
    >
      {/* Medal */}
      {showMedal && rank <= 3 && <div className="absolute top-4 right-4 z-10">{getMedalIcon(rank)}</div>}

      {/* Background Icon */}
      <div className="absolute -bottom-8 -left-8 opacity-5 z-0">
        <Trophy className={`w-60 h-60 ${getBackgroundIconColor(rank)} blur-md`} />
      </div>

      {/* Rank Badge */}
      <div className="absolute top-2 left-2 bg-gradient-to-r from-purple-600 to-blue-600 text-white text-sm font-bold px-2 py-1 rounded z-10">#{rank}</div>

      {/* Avatar and Name */}
      <div className="flex items-center gap-4 mb-4 mt-4 relative z-10">
        <Image src={contributor.avatar_url || ''} alt={contributor.name || contributor.login || 'Contributor'} width={64} height={64} className="rounded-full" />
        <div className="flex-1 min-w-0">
          <h3 className="text-xl font-bold text-white truncate">{contributor.name || contributor.login}</h3>
          <p className="text-slate-400 truncate">@{contributor.login}</p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 gap-1 mb-4 relative z-10">
        <div className="text-center">
          <div className="text-lg font-bold text-white">{numberToAbbreviation(contributor.total_commits || 0)}</div>
          <div className="text-xs text-slate-400">Commits</div>
        </div>
        <div className="text-center">
          <div className="text-lg font-bold text-white">{numberToAbbreviation(contributor.contributions || 0)}</div>
          <div className="text-xs text-slate-400">Contribs</div>
        </div>
        <div className="text-center">
          <div className="text-lg font-bold text-white">{numberToAbbreviation((contributor.additions || 0) + (contributor.deletions || 0))}</div>
          <div className="text-xs text-slate-400">Changes</div>
        </div>
      </div>

      {/* Bottom Stats */}
      <div className="grid grid-cols-2 gap-4 pt-4 border-t border-slate-700/50 relative z-10">
        <div className="text-center">
          <div className="text-green-400 text-lg font-bold">+{numberToAbbreviation(contributor.additions || 0)}</div>
          <div className="text-xs text-slate-400">Adds</div>
        </div>
        <div className="text-center">
          <div className="text-red-400 text-lg font-bold">-{numberToAbbreviation(contributor.deletions || 0)}</div>
          <div className="text-xs text-slate-400">Dels</div>
        </div>
      </div>
    </Card>
  );
};
