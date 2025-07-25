'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Gamepad2, Monitor, Star, Trophy, Users } from 'lucide-react';
import { format } from 'date-fns';
import { Link, useRouter } from '@/i18n/navigation';
import { RewardChip } from '@/components/testing-lab/common/ui/reward-chip';
import { LocationChip } from './common/ui/location-chip';

interface TestSessionTableProps {
  sessions: TestSession[];
}

export function TestSessionTable({ sessions }: TestSessionTableProps) {
  const router = useRouter();

  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <Monitor className="h-4 w-4" />;
      case 'usability':
        return <Users className="h-4 w-4" />;
      case 'bug-testing':
        return <Star className="h-4 w-4" />;
      default:
        return <Trophy className="h-4 w-4" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'open':
        return 'bg-green-900/20 text-green-400 border-green-700';
      case 'full':
        return 'bg-red-900/20 text-red-400 border-red-700';
      case 'in-progress':
        return 'bg-blue-900/20 text-blue-400 border-blue-700';
      default:
        return 'bg-slate-900/20 text-slate-400 border-slate-700';
    }
  };

  const handleRowClick = (slug: string) => {
    router.push(`/testing-lab/sessions/${slug}`);
  };

  return (
    <div className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 backdrop-blur-sm rounded-lg overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full">
          <thead className="bg-slate-800/50 border-b border-slate-700">
            <tr>
              <th className="text-left p-4 text-sm font-medium text-slate-300">Session</th>
              <th className="text-center p-4 text-sm font-medium text-slate-300">Location</th>
              <th className="text-right p-4 text-sm font-medium text-slate-300">Date & Time</th>
              <th className="text-right p-4 text-sm font-medium text-slate-300">Duration</th>
              <th className="text-right p-4 text-sm font-medium text-slate-300">Capacity</th>
              <th className="text-left p-4 text-sm font-medium text-slate-300">Type</th>
              <th className="text-left p-4 text-sm font-medium text-slate-300">Platform</th>
              <th className="text-left p-4 text-sm font-medium text-slate-300">Reward</th>
              <th className="text-center p-4 text-sm font-medium text-slate-300">Status</th>
              <th className="text-left p-4 text-sm font-medium text-slate-300">Action</th>
            </tr>
          </thead>
          <tbody>
            {sessions.map((session) => {
              const sessionDate = new Date(session.sessionDate);

              return (
                <tr
                  key={session.id}
                  className="border-b border-slate-700/50 hover:bg-slate-800/30 cursor-pointer transition-colors"
                  onClick={() => handleRowClick(session.slug)}
                >
                  <td className="p-4">
                    <div>
                      <div className="font-medium text-white text-sm">{session.title}</div>
                      <div className="text-xs text-slate-400 mt-1 max-w-xs truncate">{session.description}</div>
                    </div>
                  </td>
                  <td className="p-4 text-center">
                    <LocationChip isOnline={session.isOnline} variant="inline" />
                  </td>
                  <td className="p-4 text-right">
                    <div className="flex flex-col items-end gap-1">
                      <div className="flex items-center gap-2 text-slate-300 text-sm">
                        <Calendar className="h-3 w-3" />
                        <span>{format(sessionDate, 'MMM dd, yyyy')}</span>
                      </div>
                      <div className="flex items-center gap-2 text-slate-300 text-sm">
                        <Clock className="h-3 w-3" />
                        <span>{format(sessionDate, 'h:mm a')}</span>
                      </div>
                    </div>
                  </td>
                  <td className="p-4 text-right">
                    <span className="text-slate-300 text-sm">{session.duration} min</span>
                  </td>
                  <td className="p-4 text-right">
                    <div className="flex flex-col items-end gap-1">
                      <div className="flex items-center gap-2 text-slate-300 text-sm">
                        <Users className="h-3 w-3" />
                        <span>
                          {session.currentTesters}/{session.maxTesters}
                        </span>
                      </div>
                      <div className="flex items-center gap-2 text-slate-300 text-sm">
                        <Gamepad2 className="h-3 w-3" />
                        <span>
                          {session.currentGames}/{session.maxGames}
                        </span>
                      </div>
                    </div>
                  </td>
                  <td className="p-4">
                    <div className="flex items-center gap-2 text-slate-300 text-sm">
                      {getSessionTypeIcon(session.sessionType)}
                      <span className="capitalize">{session.sessionType.replace('-', ' ')}</span>
                    </div>
                  </td>
                  <td className="p-4">
                    <div className="flex flex-wrap gap-1">
                      {session.platform.slice(0, 2).map((platform) => (
                        <Badge key={platform} variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5">
                          {platform}
                        </Badge>
                      ))}
                      {session.platform.length > 2 && (
                        <Badge variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5">
                          +{session.platform.length - 2}
                        </Badge>
                      )}
                    </div>
                  </td>
                  <td className="p-4">
                    {session.rewards ? <RewardChip value={session.rewards.value} variant="inline" /> : <span className="text-slate-500 text-xs">-</span>}
                  </td>
                  <td className="p-4 text-center">
                    <div className="flex justify-center">
                      <Badge variant="outline" className={`${getStatusColor(session.status)} text-xs w-20 justify-center`}>
                        {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                      </Badge>
                    </div>
                  </td>
                  <td className="p-4">
                    <div onClick={(e) => e.stopPropagation()}>
                      {session.status === 'open' ? (
                        <Button
                          asChild
                          size="sm"
                          className="bg-gradient-to-r from-blue-600/30 to-blue-500/30 backdrop-blur-md border border-blue-400/40 text-white hover:from-blue-600/90 hover:to-blue-500/90 hover:border-blue-300/90 font-semibold transition-all duration-200 text-xs"
                        >
                          <Link href={`/testing-lab/sessions/${session.slug}/join`}>Join Session</Link>
                        </Button>
                      ) : session.status === 'full' ? (
                        <Button disabled size="sm" variant="outline" className="text-xs">
                          Full Session
                        </Button>
                      ) : (
                        <Button disabled size="sm" variant="outline" className="text-xs">
                          N/A
                        </Button>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
