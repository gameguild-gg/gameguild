import React from 'react';
import Image from 'next/image';
import { Card } from '@game-guild/ui/components/card';
import { Contributor } from '@/lib/contributors';
import { ContributorStats } from './contributor-stats';
import { numberToAbbreviation } from '@/lib/utils';

interface ContributorLeaderboardCardProps {
  contributor: Contributor;
  rank: number;
  showMedal?: boolean;
}

export const ContributorLeaderboardCard: React.FC<ContributorLeaderboardCardProps> = ({ contributor, rank, showMedal = false }) => {
  const getMedalIcon = (rank: number) => {
    if (rank === 1) return '🥇';
    if (rank === 2) return '🥈';
    if (rank === 3) return '🥉';
    return null;
  };

  const getBorderColor = (rank: number) => {
    if (rank === 1) return 'border-yellow-500';
    if (rank === 2) return 'border-zinc-400';
    if (rank === 3) return 'border-amber-600';
    return 'border-border';
  };

  return (
    <Card className={`bg-card ${getBorderColor(rank)} border-2 p-6 relative overflow-hidden transition-all hover:scale-105`}>
      {/* Medal */}
      {showMedal && rank <= 3 && <div className="absolute top-4 right-4 text-4xl">{getMedalIcon(rank)}</div>}

      {/* Rank Badge */}
      <div className="absolute top-2 left-2 bg-primary text-primary-foreground text-sm font-bold px-2 py-1 rounded">#{rank}</div>

      {/* Avatar and Name */}
      <div className="flex items-center gap-4 mb-6 mt-4">
        <Image
          src={contributor.avatar_url}
          alt={contributor.name || contributor.login}
          width={64}
          height={64}
          className="rounded-full border-2 border-border"
        />
        <div>
          <h3 className="text-xl font-bold text-foreground">{contributor.name || contributor.login}</h3>
          <p className="text-muted-foreground">@{contributor.login}</p>
        </div>
      </div>

      {/* Stats Grid */}
      <div className="grid grid-cols-3 gap-4 mb-6">
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
