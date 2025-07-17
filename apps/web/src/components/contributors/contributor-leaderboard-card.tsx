import React from 'react';
import Image from 'next/image';
import { Card } from '@game-guild/ui/components/card';
import { Contributor } from '@/lib/contributors';
import { ContributorStats } from './contributor-stats';
import { numberToAbbreviation } from '@/lib/utils';
import { Award, Trophy } from 'lucide-react';

interface ContributorLeaderboardCardProps {
  contributor: Contributor;
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
      className={`bg-gradient-to-br ${getGradientBg(rank)} backdrop-blur-sm ${getBorderColor(rank)} border-2 p-4 relative overflow-hidden transition-all hover:scale-105 hover:shadow-xl hover:brightness-110 hover:shadow-white/10 shadow-lg`}
    >
      {/* Medal */}
      {showMedal && rank <= 3 && <div className="absolute top-4 right-4">{getMedalIcon(rank)}</div>}

      {/* Background Icon */}
      <div className="absolute -bottom-8 -left-8 opacity-30">
        <Trophy className={`w-60 h-60 ${getBackgroundIconColor(rank)} blur-md`} />
      </div>

      {/* Rank Badge */}
      <div className="absolute top-2 left-2 bg-gradient-to-r from-purple-600 to-blue-600 text-white text-sm font-bold px-2 py-1 rounded">#{rank}</div>

      {/* Avatar and Name */}
      <div className="flex items-center gap-4 mb-4 mt-4">
        <Image src={contributor.avatar_url} alt={contributor.name || contributor.login} width={64} height={64} className="rounded-full" />
        <div>
          <h3 className="text-xl font-bold text-white">{contributor.name || contributor.login}</h3>
          <p className="text-slate-400">@{contributor.login}</p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 gap-4 mb-4">
        <div className="text-center">
          <div className="text-sm text-muted-foreground uppercase tracking-wide">Commits</div>
          <div className="text-2xl font-bold text-foreground">{numberToAbbreviation(contributor.total_commits || 0)}</div>
        </div>
        <div className="text-center">
          <div className="text-sm text-muted-foreground uppercase tracking-wide">Contributions</div>
          <div className="text-2xl font-bold text-foreground">{numberToAbbreviation(contributor.contributions || 0)}</div>
        </div>
        <div className="text-center">
          <div className="text-sm text-muted-foreground uppercase tracking-wide">Changes</div>
          <div className="text-2xl font-bold text-foreground">{numberToAbbreviation((contributor.additions || 0) + (contributor.deletions || 0))}</div>
        </div>
      </div>

      {/* Bottom Stats */}
      <div className="flex justify-between items-center">
        <ContributorStats
          label="Additions"
          value={contributor.additions || 0}
          icon={<span className="text-green-600 dark:text-green-400">+</span>}
          variant="primary"
        />
        <ContributorStats
          label="Deletions"
          value={contributor.deletions || 0}
          icon={<span className="text-red-600 dark:text-red-400">-</span>}
          variant="secondary"
        />
      </div>
    </Card>
  );
};
