import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Calendar, MapPin, Users } from 'lucide-react';
import Image from 'next/image';
import type { MemberProfileStats } from '../types';
import { ProfileActions } from './profile-actions';
import { ProfileStatsCards } from './profile-stats-cards';
import { UserBadges } from './user-badges';

interface ProfileHeaderProps {
  username: string;
  displayName: string;
  initials: string;
  joinDate: Date;
  headline?: string;
  location?: string;
  avatarUrl?: string;
  bannerUrl?: string;
  stats?: MemberProfileStats;
}

export function ProfileHeader({
  username,
  displayName,
  initials,
  joinDate,
  headline,
  location,
  avatarUrl,
  bannerUrl,
  stats,
}: ProfileHeaderProps) {
  return (
    <div className="relative h-80 md:h-96 overflow-hidden">
      <Image
        src={bannerUrl || '/placeholder.svg?height=384&width=1200'}
        alt="Profile Cover"
        className="w-full h-full object-cover"
        width={1200}
        height={384}
        unoptimized={Boolean(bannerUrl)}
      />
      <div className="absolute inset-0 bg-gradient-to-t from-slate-900/80 via-slate-900/40 to-transparent" />
      <div className="absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20" />

      <ProfileActions />

      <div className="absolute inset-0 flex items-end">
        <div className="max-w-7xl mx-auto w-full px-6 pb-6">
          <div className="flex items-end justify-between gap-8">
            <div className="flex items-end gap-4">
              <Avatar className="w-28 h-28 border-4 border-purple-500/30 shadow-2xl">
                <AvatarImage src={avatarUrl || `/avatars/${username}.png`} />
                <AvatarFallback className="text-lg bg-gradient-to-br from-indigo-600 to-blue-600 text-white">
                  {initials}
                </AvatarFallback>
              </Avatar>
              <div className="space-y-2 flex-1">
                <h1 className="text-2xl font-bold text-white">{displayName}</h1>
                <p className="text-purple-300">{headline || 'Game Guild community member'}</p>
                <UserBadges />
                <div className="flex flex-wrap gap-4 text-sm text-gray-400 mt-2">
                  {location ? (
                    <div className="flex items-center gap-1">
                      <MapPin className="w-4 h-4" />
                      {location}
                    </div>
                  ) : null}
                  <div className="flex items-center gap-1">
                    <Calendar className="w-4 h-4" />
                    Joined {joinDate.toLocaleDateString('en-US', { year: 'numeric', month: 'long' })}
                  </div>
                  <div className="flex items-center gap-1">
                    <Users className="w-4 h-4" />
                    Active member
                  </div>
                </div>
              </div>
            </div>
            <ProfileStatsCards stats={stats} />
          </div>
        </div>
      </div>
    </div>
  );
}
