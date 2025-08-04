'use client';

import { adaptTestingSessionForComponent, SESSION_STATUS, TestSession } from '@/lib/admin';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Gamepad2, Users } from 'lucide-react';
import { format } from 'date-fns';
import { Link, useRouter } from '@/i18n/navigation';

interface TestSessionCardProps {
  session: TestSession;
}

export function TestSessionCard({ session }: TestSessionCardProps) {
  const router = useRouter();

  // Adapt the API session data to component-friendly format
  const adaptedSession = adaptTestingSessionForComponent(session);

  const sessionDate = new Date(adaptedSession.sessionDate);
  const spotsLeft = adaptedSession.maxTesters - adaptedSession.currentTesters;
  const isAlmostFull = spotsLeft <= 2;

  // Helper function to convert numeric status to string
  const getStatusString = (status: number): 'open' | 'full' | 'in-progress' | 'closed' => {
    switch (status) {
      case SESSION_STATUS.SCHEDULED:
        return 'open';
      case SESSION_STATUS.ACTIVE:
        return 'in-progress';
      case SESSION_STATUS.COMPLETED:
        return 'closed';
      case SESSION_STATUS.CANCELLED:
        return 'closed';
      default:
        return 'closed';
    }
  };

  const handleCardClick = () => {
    router.push(`/testing-lab/sessions/${adaptedSession.slug}`);
  };

  return (
    <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-all duration-200 hover:scale-[1.02] hover:shadow-lg hover:shadow-blue-500/10 group cursor-pointer">
      <div onClick={handleCardClick}>
        <CardHeader className="pb-2">
          <div className="flex items-start justify-between gap-2 mb-1">
            <Badge variant="secondary">{adaptedSession.isOnline ? 'Online' : 'Offline'}</Badge>
            <Badge variant="outline">{getStatusString(adaptedSession.status)}</Badge>
          </div>

          <div className="mb-1">
            <CardTitle className="text-sm font-bold text-white">{adaptedSession.title}</CardTitle>
          </div>

          <CardDescription className="text-slate-400 text-xs">Testing Session</CardDescription>
        </CardHeader>

        <CardContent className="space-y-3">
          <p className="text-xs text-slate-300 leading-relaxed line-clamp-2">{adaptedSession.description}</p>

          {/* Meta info */}
          <div className="grid grid-cols-2 gap-2 mt-2">
            <div className="flex items-center text-xs text-slate-400">
              <Users className="h-3 w-3 mr-1" />
              <span>
                {adaptedSession.currentTesters}/{adaptedSession.maxTesters} testers
              </span>
            </div>
            <div className="flex items-center text-xs text-slate-400">
              <Gamepad2 className="h-3 w-3 mr-1" />
              <span>
                {adaptedSession.currentGames}/{adaptedSession.maxGames} games
              </span>
            </div>
          </div>

          {/* Date and Time */}
          <div className="flex items-center gap-4 text-xs text-slate-400">
            <div className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              <span>{format(sessionDate, 'MMM dd, yyyy')}</span>
            </div>
            <div className="flex items-center gap-1">
              <Clock className="h-3 w-3" />
              <span>{format(sessionDate, 'h:mm a')}</span>
            </div>
          </div>

          {/* Platform Tags */}
          <div className="flex flex-wrap gap-1">
            {adaptedSession.platform.slice(0, 3).map((platform) => (
              <Badge key={platform} variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5">
                {platform}
              </Badge>
            ))}
            {adaptedSession.platform.length > 3 && (
              <Badge variant="secondary" className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5">
                +{adaptedSession.platform.length - 3}
              </Badge>
            )}
          </div>

          {/* Spots Warning */}
          {isAlmostFull && getStatusString(adaptedSession.status) === 'open' && (
            <div className="bg-orange-900/20 border border-orange-700 rounded-lg p-2">
              <p className="text-orange-400 text-xs font-medium">
                Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left!
              </p>
            </div>
          )}

          {/* Rewards */}
          {adaptedSession.rewards && <div className="text-xs text-green-400">Reward: {adaptedSession.rewards.value} points</div>}

          {/* Action Button */}
          <div className="pt-1">
            {getStatusString(adaptedSession.status) === 'open' ? (
              <div onClick={(e) => e.stopPropagation()}>
                <Button
                  asChild
                  size="sm"
                  className="w-full bg-gradient-to-r from-blue-600/30 to-blue-500/30 backdrop-blur-md border border-blue-400/40 text-white hover:from-blue-600/90 hover:to-blue-500/90 hover:border-blue-300/90 font-semibold transition-all duration-200 text-xs h-8"
                >
                  <Link href={`/testing-lab/sessions/${adaptedSession.slug}/join`}>Join Session</Link>
                </Button>
              </div>
            ) : (
              <Button disabled className="w-full h-8 text-xs" variant="outline">
                Not Available
              </Button>
            )}
          </div>
        </CardContent>
      </div>
    </Card>
  );
}
