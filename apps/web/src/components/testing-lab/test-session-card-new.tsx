'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Users, Monitor, Trophy, Star } from 'lucide-react';
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

  const getSkillLevelColor = (level: string) => {
    switch (level) {
      case 'beginner':
        return 'bg-green-900/20 text-green-400 border-green-700';
      case 'intermediate':
        return 'bg-yellow-900/20 text-yellow-400 border-yellow-700';
      case 'advanced':
        return 'bg-red-900/20 text-red-400 border-red-700';
      default:
        return 'bg-blue-900/20 text-blue-400 border-blue-700';
    }
  };

  return (
    <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm hover:border-slate-600 transition-all duration-200 hover:scale-[1.02] hover:shadow-lg hover:shadow-blue-500/10 group cursor-pointer">
      <div onClick={handleCardClick}>
        <CardHeader className="pb-4">
          <div className="flex items-start justify-between mb-2">
            <Badge variant="outline" className={getStatusColor(session.status)}>
              {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
            </Badge>
            <Badge variant="outline" className={getSkillLevelColor(session.skillLevel)}>
              {session.skillLevel}
            </Badge>
          </div>

          <CardTitle className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent group-hover:from-blue-300 group-hover:to-purple-300 transition-all duration-200">
            {session.title}
          </CardTitle>

          <CardDescription className="text-slate-400">
            <div className="space-y-1">
              <div className="font-semibold text-white">{session.gameTitle}</div>
              <div>by {session.gameDeveloper}</div>
            </div>
          </CardDescription>
        </CardHeader>

        <CardContent className="space-y-4">
          <p className="text-sm text-slate-300 leading-relaxed">{session.description}</p>

          {/* Session Details */}
          <div className="grid grid-cols-2 gap-4 text-sm">
            <div className="flex items-center gap-2 text-slate-400">
              <Calendar className="h-4 w-4" />
              <span>{format(sessionDate, 'MMM dd, yyyy')}</span>
            </div>
            <div className="flex items-center gap-2 text-slate-400">
              <Clock className="h-4 w-4" />
              <span>{session.duration} min</span>
            </div>
            <div className="flex items-center gap-2 text-slate-400">
              <Users className="h-4 w-4" />
              <span>
                {session.currentTesters}/{session.maxTesters} testers
              </span>
            </div>
            <div className="flex items-center gap-2 text-slate-400">
              {getSessionTypeIcon(session.sessionType)}
              <span className="capitalize">{session.sessionType.replace('-', ' ')}</span>
            </div>
          </div>

          {/* Platform Tags */}
          <div className="flex flex-wrap gap-2">
            {session.platform.map((platform) => (
              <Badge
                key={platform}
                variant="secondary"
                className="bg-slate-800/50 text-slate-300 border-slate-600 text-xs"
              >
                {platform}
              </Badge>
            ))}
          </div>

          {/* Spots Warning */}
          {isAlmostFull && session.status === 'open' && (
            <div className="bg-orange-900/20 border border-orange-700 rounded-lg p-3">
              <p className="text-orange-400 text-sm font-medium">
                Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left!
              </p>
            </div>
          )}

          {/* Rewards */}
          {session.rewards && (
            <div className="bg-gradient-to-r from-blue-900/20 to-purple-900/20 border border-blue-700/50 rounded-lg p-3">
              <div className="flex items-center gap-2">
                <Trophy className="h-4 w-4 text-yellow-400" />
                <span className="text-yellow-400 font-medium text-sm">Reward: {session.rewards.value}</span>
              </div>
            </div>
          )}
        </CardContent>
      </div>

      {/* Action Button - Outside the clickable div to prevent nesting */}
      <div className="px-6 pb-6">
        <div className="pt-2 space-y-2">
          {session.status === 'open' ? (
            <>
              <div onClick={(e) => e.stopPropagation()}>
                <Button asChild className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">
                  <Link href={`/testing-lab/sessions/${session.slug}/join`}>Join Session</Link>
                </Button>
              </div>
            </>
          ) : session.status === 'full' ? (
            <>
              <Button disabled className="w-full" variant="outline">
                Session Full
              </Button>
            </>
          ) : (
            <>
              <Button disabled className="w-full" variant="outline">
                Not Available
              </Button>
            </>
          )}
        </div>
      </div>
    </Card>
  );
}
