import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Calendar, Clock, Users, Monitor, Trophy, Star, ArrowLeft } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';

interface SessionDetailProps {
  session: TestSession;
}

export function SessionDetail({ session }: SessionDetailProps) {
  const sessionDate = new Date(session.sessionDate);
  const spotsLeft = session.maxTesters - session.currentTesters;
  const isAlmostFull = spotsLeft <= 2;

  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <Monitor className="h-5 w-5" />;
      case 'usability':
        return <Users className="h-5 w-5" />;
      case 'bug-testing':
        return <Star className="h-5 w-5" />;
      default:
        return <Trophy className="h-5 w-5" />;
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
    <div className="container mx-auto px-4 py-8 max-w-4xl">
      {/* Navigation */}
      <div className="flex items-center gap-4 mb-8">
        <Button asChild variant="ghost" className="text-slate-400 hover:text-white">
          <Link href="/testing-lab">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Back to Sessions
          </Link>
        </Button>
      </div>

      {/* Session Header */}
      <div className="mb-8">
        <div className="flex flex-wrap items-start justify-between gap-4 mb-4">
          <div>
            <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent mb-2">
              {session.title}
            </h1>
            <p className="text-xl text-slate-300">{session.gameTitle}</p>
            <p className="text-slate-400">by {session.gameDeveloper}</p>
          </div>
          <Badge variant="outline" className={getStatusColor(session.status) + ' text-lg px-4 py-2'}>
            {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
          </Badge>
        </div>
      </div>

      <div className="grid lg:grid-cols-3 gap-8">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-8">
          {/* Description */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white">Session Overview</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-slate-300 leading-relaxed">{session.description}</p>
            </CardContent>
          </Card>

          {/* Requirements */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white">Requirements</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2">
                {session.requirements.map((requirement, index) => (
                  <li key={index} className="flex items-start gap-2 text-slate-300">
                    <span className="text-blue-400 mt-1">•</span>
                    {requirement}
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>

          {/* Platform Information */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white">Platform & Compatibility</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2">
                {session.platform.map((platform) => (
                  <Badge 
                    key={platform} 
                    variant="secondary" 
                    className="bg-slate-800/50 text-slate-300 border-slate-600"
                  >
                    {platform}
                  </Badge>
                ))}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Session Details */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-lg font-bold text-white">Session Details</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center gap-3">
                <Calendar className="h-5 w-5 text-blue-400" />
                <div>
                  <p className="text-white font-medium">{format(sessionDate, 'EEEE, MMMM dd')}</p>
                  <p className="text-slate-400 text-sm">{format(sessionDate, 'yyyy')}</p>
                </div>
              </div>
              
              <div className="flex items-center gap-3">
                <Clock className="h-5 w-5 text-purple-400" />
                <div>
                  <p className="text-white font-medium">{format(sessionDate, 'h:mm a')}</p>
                  <p className="text-slate-400 text-sm">{session.duration} minutes</p>
                </div>
              </div>

              <div className="flex items-center gap-3">
                <Users className="h-5 w-5 text-green-400" />
                <div>
                  <p className="text-white font-medium">{session.currentTesters}/{session.maxTesters} testers</p>
                  <p className="text-slate-400 text-sm">{spotsLeft} spots remaining</p>
                </div>
              </div>

              <div className="flex items-center gap-3">
                {getSessionTypeIcon(session.sessionType)}
                <div>
                  <p className="text-white font-medium capitalize">{session.sessionType.replace('-', ' ')}</p>
                  <p className="text-slate-400 text-sm">{session.skillLevel} level</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Rewards */}
          {session.rewards && (
            <Card className="bg-gradient-to-br from-blue-900/20 to-purple-900/20 border-blue-700/50 backdrop-blur-sm">
              <CardHeader>
                <CardTitle className="text-lg font-bold text-white flex items-center gap-2">
                  <Trophy className="h-5 w-5 text-yellow-400" />
                  Rewards
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-yellow-400 font-medium">{session.rewards.value}</p>
              </CardContent>
            </Card>
          )}

          {/* Spots Warning */}
          {isAlmostFull && session.status === 'open' && (
            <div className="bg-orange-900/20 border border-orange-700 rounded-lg p-4">
              <p className="text-orange-400 font-medium text-center">
                Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left!
              </p>
            </div>
          )}

          {/* Join Button */}
          <div className="space-y-3">
            {session.status === 'open' ? (
              <Button asChild size="lg" className="w-full bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0">
                <Link href={`/testing-lab/sessions/${session.id}/join`}>Join This Session</Link>
              </Button>
            ) : session.status === 'full' ? (
              <Button disabled size="lg" className="w-full" variant="outline">
                Session Full
              </Button>
            ) : (
              <Button disabled size="lg" className="w-full" variant="outline">
                Not Available
              </Button>
            )}
            
            <Button asChild size="sm" variant="ghost" className="w-full text-slate-400 hover:text-white">
              <Link href="/testing-lab">Browse Other Sessions</Link>
            </Button>
          </div>
        </div>
      </div>
    </div>
  );
}
