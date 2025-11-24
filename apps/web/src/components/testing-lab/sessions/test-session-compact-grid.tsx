'use client';

// Local lightweight session type for compact grid (avoids invalid import path)
interface TestSession {
  id: string;
  slug: string;
  title: string;
  gameTitle: string;
  gameDeveloper: string;
  sessionDate: string;
  maxTesters: number;
  currentTesters: number;
  sessionType: string;
  status: 'open' | 'full' | string;
  platform: string[];
}
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link, useRouter } from '@/i18n/navigation';
import { format } from 'date-fns';
import { Calendar, Clock, Monitor, Star, Trophy, Users } from 'lucide-react';

interface TestSessionCompactGridProps {
  sessions: TestSession[];
}

export function TestSessionCompactGrid({ sessions }: TestSessionCompactGridProps) {
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

  const handleGridItemClick = (slug: string) => {
    router.push(`/testing-lab/sessions/${slug}`);
  };

  return (
    <div className="grid gap-3 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6">
      {sessions.map((session) => {
        const sessionDate = new Date(session.sessionDate);
        const spotsLeft = session.maxTesters - session.currentTesters;

        return (
          <div
            key={session.id}
            onClick={() => handleGridItemClick(session.slug)}
            className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-all duration-200 hover:scale-[1.02] rounded-lg p-3 cursor-pointer group"
          >
            {/* Status Badge */}
            <div className="flex justify-end mb-2">
              <Badge variant="outline" className={getStatusColor(session.status) + ' text-xs'}>
                {session.status === 'open' ? 'Open' : session.status === 'full' ? 'Full' : 'N/A'}
              </Badge>
            </div>

            {/* Title */}
            <h3 className="font-semibold text-white text-sm mb-2 line-clamp-2 leading-tight">{session.title}</h3>

            {/* Game Info */}
            <div className="mb-3">
              <div className="text-xs font-medium text-slate-300 truncate">{session.gameTitle}</div>
              <div className="text-xs text-slate-400 truncate">by {session.gameDeveloper}</div>
            </div>

            {/* Key Details */}
            <div className="space-y-1 mb-3 text-xs">
              <div className="flex items-center gap-2 text-slate-400">
                <Calendar className="h-3 w-3" />
                <span>{format(sessionDate, 'MMM dd')}</span>
              </div>
              <div className="flex items-center gap-2 text-slate-400">
                <Clock className="h-3 w-3" />
                <span>{format(sessionDate, 'h:mm a')}</span>
              </div>
              <div className="flex items-center gap-2 text-slate-400">
                <Users className="h-3 w-3" />
                <span>
                  {session.currentTesters}/{session.maxTesters}
                </span>
              </div>
              <div className="flex items-center gap-2 text-slate-400">
                {getSessionTypeIcon(session.sessionType)}
                <span className="capitalize text-xs">{session.sessionType.replace('-', ' ')}</span>
              </div>
            </div>

            {/* Platform Tags */}
            <div className="flex flex-wrap gap-1 mb-3">
              {session.platform.slice(0, 2).map((platform: string) => (
                <Badge key={platform} variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1 py-0 h-5">
                  {platform}
                </Badge>
              ))}
              {session.platform.length > 2 && (
                <Badge variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1 py-0 h-5">
                  +{session.platform.length - 2}
                </Badge>
              )}
            </div>

            {/* Action Button */}
            <div onClick={(e) => e.stopPropagation()}>
              {session.status === 'open' ? (
                <Button
                  asChild
                  size="sm"
                  className="w-full h-7 bg-gradient-to-r from-blue-600/30 to-blue-500/30 backdrop-blur-md border border-blue-400/40 text-white hover:from-blue-600/90 hover:to-blue-500/90 hover:border-blue-300/90 font-semibold transition-all duration-200 text-xs"
                >
                  <Link href={`/testing-lab/sessions/${session.slug}`}>View</Link>
                </Button>
              ) : (
                <Button disabled size="sm" variant="outline" className="w-full h-7 text-xs">
                  {session.status === 'full' ? 'Full' : 'N/A'}
                </Button>
              )}
            </div>
          </div>
        );
      })}
    </div>
  );
}
