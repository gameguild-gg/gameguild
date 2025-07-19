'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Users, Trophy, Gamepad2, Monitor, Star } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';
import { useRouter } from 'next/navigation';

interface TestSessionCardProps {
  session: TestSession;
}

export function TestSessionCard({ session }: TestSessionCardProps) {
  const router = useRouter();
  const sessionDate = new Date(session.sessionDate);
  const spotsLeft = session.maxTesters - session.currentTesters;
  const isAlmostFull = spotsLeft <= 2;

  const handleCardClick = () => {
    router.push(`/testing-lab/sessions/${session.slug}`);
  };

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

  return (
    <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-all duration-200 hover:scale-[1.02] hover:shadow-lg hover:shadow-blue-500/10 group cursor-pointer">
      <div onClick={handleCardClick}>
        <CardHeader className="pb-2">
          <div className="flex items-start justify-end mb-1">
            <Badge variant="outline" className={getStatusColor(session.status)}>
              {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
            </Badge>
          </div>
          
          <CardTitle className="text-sm font-bold text-white">
            {session.title}
          </CardTitle>
          
          <CardDescription className="text-slate-400 text-xs">
            Testing Session
          </CardDescription>
        </CardHeader>

      <CardContent className="space-y-3">
        <p className="text-xs text-slate-300 leading-relaxed line-clamp-2">
          {session.description}
        </p>

        {/* Session Details */}
        <div className="flex gap-3 text-xs">
          {/* Left Side - Capacity Information */}
          <div className="flex-1 space-y-1.5">
            <div className="flex items-center gap-2 text-slate-400">
              <Users className="h-3 w-3" />
              <span>{session.currentTesters}/{session.maxTesters} testers</span>
            </div>
            <div className="flex items-center gap-2 text-slate-400">
              <Gamepad2 className="h-3 w-3" />
              <span>{session.currentGames}/{session.maxGames} games</span>
            </div>
            <div className="flex items-center gap-2 text-slate-400">
              {getSessionTypeIcon(session.sessionType)}
              <span className="capitalize">{session.sessionType.replace('-', ' ')}</span>
            </div>
          </div>
          
          {/* Vertical Separator */}
          <div className="w-px bg-slate-600"></div>
          
          {/* Right Side - Date, Time & Duration */}
          <div className="flex-1 space-y-1.5">
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

        {/* Platform Tags */}
        <div className="flex flex-wrap gap-1">
          {session.platform.slice(0, 3).map((platform) => (
            <Badge 
              key={platform} 
              variant="secondary" 
              className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5"
            >
              {platform}
            </Badge>
          ))}
          {session.platform.length > 3 && (
            <Badge 
              variant="secondary" 
              className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs px-1.5 py-0.5"
            >
              +{session.platform.length - 3}
            </Badge>
          )}
        </div>

        {/* Spots Warning */}
        {isAlmostFull && session.status === 'open' && (
          <div className="bg-orange-900/20 border border-orange-700 rounded-lg p-2">
            <p className="text-orange-400 text-xs font-medium">
              Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left!
            </p>
          </div>
        )}

        {/* Rewards */}
        {session.rewards && (
          <div className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border border-blue-700/50 rounded-lg p-2">
            <div className="flex items-center gap-2">
              <Trophy className="h-3 w-3 text-yellow-400" />
              <span className="text-yellow-400 font-medium text-xs">Reward: {session.rewards.value}</span>
            </div>
          </div>
        )}

        {/* Action Button */}
        <div className="pt-1">
          {session.status === 'open' ? (
            <div onClick={(e) => e.stopPropagation()}>
              <Button 
                asChild 
                size="sm"
                className="w-full bg-gradient-to-r from-blue-600/30 to-blue-500/30 backdrop-blur-md border border-blue-400/40 text-white hover:from-blue-600/90 hover:to-blue-500/90 hover:border-blue-300/90 font-semibold transition-all duration-200 text-xs h-8"
              >
                <Link href={`/testing-lab/sessions/${session.slug}/join`}>Join Session</Link>
              </Button>
            </div>
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
      </CardContent>
      </div>
    </Card>
  );
}
