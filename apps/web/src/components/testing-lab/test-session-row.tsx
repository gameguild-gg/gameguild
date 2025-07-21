'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Gamepad2, Monitor, Star, Trophy, Users } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { RewardChip } from './reward-chip';
import { StatusChip } from './status-chip';
import { LocationChip } from './location-chip';

interface TestSessionRowProps {
  sessions: TestSession[];
}

export function TestSessionRow({ sessions }: TestSessionRowProps) {
  const router = useRouter();

  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <Monitor className="h-3 w-3" />;
      case 'usability':
        return <Users className="h-3 w-3" />;
      case 'bug-testing':
        return <Star className="h-3 w-3" />;
      default:
        return <Trophy className="h-3 w-3" />;
    }
  };

  const handleRowClick = (slug: string) => {
    router.push(`/testing-lab/sessions/${slug}`);
  };

  return (
    <div className="space-y-2">
      {sessions.map((session) => {
        const sessionDate = new Date(session.sessionDate);
        const spotsLeft = session.maxTesters - session.currentTesters;
        const isAlmostFull = spotsLeft <= 2;

        return (
          <div
            key={session.id}
            onClick={() => handleRowClick(session.slug)}
            className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-all duration-200 hover:scale-[1.005] hover:shadow-lg hover:shadow-blue-500/10 rounded-lg p-4 cursor-pointer group"
          >
            <div className="flex flex-col lg:flex-row gap-4">
              {/* Left Section - Main Info */}
              <div className="flex-1">
                <div className="flex items-start justify-between mb-2">
                  <div className="flex-1">
                    <div className="flex items-center justify-between gap-2 mb-1">
                      <h3 className="text-lg font-bold text-white flex-1">{session.title}</h3>
                    </div>
                    <div className="flex items-center justify-between">
                      <div className="text-sm text-slate-400">Testing Session</div>
                      <div className="flex items-center gap-2">
                        <LocationChip isOnline={session.isOnline} variant="compact" />
                        <StatusChip status={session.status as 'open' | 'full' | 'in-progress' | 'closed'} variant="compact" />
                      </div>
                    </div>
                  </div>
                </div>

                <p className="text-sm text-slate-300 leading-relaxed mb-3 line-clamp-2">{session.description}</p>

                {/* Platform Tags */}
                <div className="flex flex-wrap gap-1">
                  {session.platform.slice(0, 4).map((platform) => (
                    <Badge key={platform} variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-2 py-0.5">
                      {platform}
                    </Badge>
                  ))}
                  {session.platform.length > 4 && (
                    <Badge variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-2 py-0.5">
                      +{session.platform.length - 4}
                    </Badge>
                  )}
                </div>
              </div>

              {/* Center Section - Session Details */}
              <div className="lg:w-72">
                <div className="flex gap-4 text-sm">
                  {/* Left Side - Capacity Information */}
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2 text-slate-400">
                      <Users className="h-3 w-3" />
                      <span>
                        {session.currentTesters}/{session.maxTesters} testers
                      </span>
                    </div>
                    <div className="flex items-center gap-2 text-slate-400">
                      <Gamepad2 className="h-3 w-3" />
                      <span>
                        {session.currentGames}/{session.maxGames} games
                      </span>
                    </div>
                    <div className="flex items-center gap-2 text-slate-400">
                      {getSessionTypeIcon(session.sessionType)}
                      <span className="capitalize text-xs">{session.sessionType.replace('-', ' ')}</span>
                    </div>
                  </div>

                  {/* Vertical Separator */}
                  <div className="w-px bg-slate-600"></div>

                  {/* Right Side - Date, Time & Duration */}
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2 text-slate-400">
                      <Calendar className="h-3 w-3" />
                      <span>{format(sessionDate, 'MMM dd, yyyy')}</span>
                    </div>
                    <div className="flex items-center gap-2 text-slate-400">
                      <Clock className="h-3 w-3" />
                      <span>{format(sessionDate, 'h:mm a')}</span>
                    </div>
                    <div className="flex items-center gap-2 text-slate-400">
                      <Clock className="h-3 w-3" />
                      <span>{session.duration} min</span>
                    </div>
                  </div>
                </div>

                {/* Rewards */}
                {session.rewards && (
                  <div className="flex justify-start mt-3">
                    <RewardChip value={session.rewards.value} variant="compact" />
                  </div>
                )}
              </div>

              {/* Right Section - Action & Alerts */}
              <div className="lg:w-40 space-y-2">
                {/* Spots Warning */}
                {isAlmostFull && session.status === 'open' && (
                  <div className="bg-orange-900/20 border border-orange-700 rounded-lg p-2">
                    <p className="text-orange-400 text-xs font-medium">
                      Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left!
                    </p>
                  </div>
                )}

                {/* Action Button */}
                <div onClick={(e) => e.stopPropagation()}>
                  {session.status === 'open' ? (
                    <Button
                      asChild
                      size="sm"
                      className="w-full bg-gradient-to-r from-blue-600/30 to-blue-500/30 backdrop-blur-md border border-blue-400/40 text-white hover:from-blue-600/90 hover:to-blue-500/90 hover:border-blue-300/90 font-semibold transition-all duration-200 text-xs h-8"
                    >
                      <Link href={`/testing-lab/sessions/${session.slug}/join`}>Join Session</Link>
                    </Button>
                  ) : session.status === 'full' ? (
                    <Button disabled className="w-full h-8 text-xs" variant="outline">
                      Session Full
                    </Button>
                  ) : (
                    <Button disabled className="w-full h-8 text-xs" variant="outline">
                      Not Available
                    </Button>
                  )}
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
