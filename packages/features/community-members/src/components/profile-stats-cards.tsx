import { Gamepad2, Heart, Star, Trophy } from 'lucide-react';
import type { MemberProfileStats } from '../types';

function compactNumber(value: number) {
  return new Intl.NumberFormat('en-US', { notation: 'compact', maximumFractionDigits: 1 }).format(value);
}

function getStatsCards(stats?: MemberProfileStats) {
  return [
    {
      icon: Trophy,
      value: compactNumber(stats?.followers ?? 0),
      label: 'Followers',
      className: 'bg-gradient-to-br from-yellow-500/25 to-transparent border-yellow-400/40',
      iconColor: 'text-yellow-500',
    },
    {
      icon: Star,
      value: compactNumber(stats?.following ?? 0),
      label: 'Following',
      className: 'bg-gradient-to-br from-purple-500/25 to-transparent border-purple-400/40',
      iconColor: 'text-purple-400',
    },
    {
      icon: Gamepad2,
      value: compactNumber(stats?.projects ?? 0),
      label: 'Projects',
      className: 'bg-gradient-to-br from-sky-500/25 to-transparent border-sky-400/40',
      iconColor: 'text-sky-400',
    },
    {
      icon: Heart,
      value: compactNumber(stats?.posts ?? 0),
      label: 'Posts',
      className: 'bg-gradient-to-br from-red-500/25 to-transparent border-red-400/40',
      iconColor: 'text-red-400',
    },
  ];
}

export function ProfileStatsCards({ stats }: { stats?: MemberProfileStats }) {
  const cards = getStatsCards(stats);

  return (
    <div className="hidden md:flex gap-3">
      {cards.map((stat) => {
        const Icon = stat.icon;
        return (
          <div
            key={stat.label}
            className={`${stat.className} border rounded-lg p-4 text-center backdrop-blur-md shadow-xl`}
          >
            <Icon className={`w-5 h-5 ${stat.iconColor} mx-auto mb-1`} />
            <div className="text-lg font-bold text-white">{stat.value}</div>
            <div className="text-xs text-slate-300">{stat.label}</div>
          </div>
        );
      })}
    </div>
  );
}
