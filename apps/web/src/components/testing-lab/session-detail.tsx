import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { ArrowLeft, Calendar, Clock, GamepadIcon, Monitor, Target, Trophy, Users, Zap } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';

interface SessionDetailProps {
  session: TestSession;
}

export function SessionDetail({ session }: SessionDetailProps) {
  const sessionDate = new Date(session.sessionDate);
  const spotsLeft = session.maxTesters - session.currentTesters;
  const isAlmostFull = spotsLeft <= 2;
  const fillPercentage = (session.currentTesters / session.maxTesters) * 100;

  const getSessionTypeIcon = (type: string) => {
    switch (type) {
      case 'gameplay':
        return <GamepadIcon className="h-6 w-6" />;
      case 'usability':
        return <Users className="h-6 w-6" />;
      case 'bug-testing':
        return <Target className="h-6 w-6" />;
      default:
        return <Trophy className="h-6 w-6" />;
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
    <div className="min-h-screen">
      {/* Hero Section */}
      <div className="relative overflow-hidden bg-gradient-to-br from-slate-950 via-blue-950/50 to-purple-950/50">
        <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top,_var(--tw-gradient-stops))] from-blue-900/20 via-slate-900/50 to-purple-900/20" />

        {/* Navigation */}
        <div className="relative container mx-auto px-4 pt-8">
          <Button asChild variant="ghost" className="text-slate-400 hover:text-white mb-8">
            <Link href="/testing-lab">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Back to Sessions
            </Link>
          </Button>
        </div>

        {/* Hero Content */}
        <div className="relative container mx-auto px-4 pb-16">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            {/* Left Column - Main Info */}
            <div className="space-y-8">
              <div className="space-y-4">
                <div className="flex items-center gap-3">
                  <Badge variant="outline" className={getStatusColor(session.status) + ' text-lg px-4 py-2'}>
                    {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                  </Badge>
                  <Badge variant="outline" className="bg-blue-900/20 text-blue-400 border-blue-700 text-sm px-3 py-1">
                    {session.skillLevel}
                  </Badge>
                </div>

                <h1 className="text-5xl md:text-6xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent leading-tight">
                  {session.title}
                </h1>

                <div className="space-y-2">
                  <h2 className="text-2xl md:text-3xl font-bold text-white">{session.gameTitle}</h2>
                  <p className="text-xl text-slate-300">by {session.gameDeveloper}</p>
                </div>

                <p className="text-lg text-slate-300 leading-relaxed max-w-2xl">{session.description}</p>
              </div>

              {/* Quick Stats */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 border border-slate-600 rounded-lg p-4 text-center backdrop-blur-sm">
                  <Calendar className="h-6 w-6 text-blue-400 mx-auto mb-2" />
                  <p className="text-white font-semibold">{format(sessionDate, 'MMM dd')}</p>
                  <p className="text-slate-400 text-sm">{format(sessionDate, 'h:mm a')}</p>
                </div>

                <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 border border-slate-600 rounded-lg p-4 text-center backdrop-blur-sm">
                  <Clock className="h-6 w-6 text-purple-400 mx-auto mb-2" />
                  <p className="text-white font-semibold">{session.duration}m</p>
                  <p className="text-slate-400 text-sm">Duration</p>
                </div>

                <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 border border-slate-600 rounded-lg p-4 text-center backdrop-blur-sm">
                  <Users className="h-6 w-6 text-green-400 mx-auto mb-2" />
                  <p className="text-white font-semibold">
                    {session.currentTesters}/{session.maxTesters}
                  </p>
                  <p className="text-slate-400 text-sm">Testers</p>
                </div>

                <div className="bg-gradient-to-br from-slate-800/50 to-slate-700/50 border border-slate-600 rounded-lg p-4 text-center backdrop-blur-sm">
                  {getSessionTypeIcon(session.sessionType)}
                  <p className="text-white font-semibold text-sm mt-2 capitalize">{session.sessionType.replace('-', ' ')}</p>
                  <p className="text-slate-400 text-xs">Type</p>
                </div>
              </div>

              {/* CTA Buttons */}
              <div className="flex flex-col sm:flex-row gap-4">
                {session.status === 'open' ? (
                  <Button
                    asChild
                    size="lg"
                    className="bg-gradient-to-r from-blue-600 to-blue-700 hover:from-blue-700 hover:to-blue-800 text-white border-0 px-8 py-4 text-lg"
                  >
                    <Link href={`/testing-lab/sessions/${session.slug}/join`}>
                      <Zap className="mr-2 h-5 w-5" />
                      Join This Session
                    </Link>
                  </Button>
                ) : (
                  <Button disabled size="lg" className="px-8 py-4 text-lg" variant="outline">
                    {session.status === 'full' ? 'Session Full' : 'Not Available'}
                  </Button>
                )}

                <Button asChild size="lg" variant="outline" className="border-slate-600 bg-slate-800/50 text-slate-200 hover:bg-slate-700/50 px-8 py-4 text-lg">
                  <Link href="/testing-lab">Browse Other Sessions</Link>
                </Button>
              </div>
            </div>

            {/* Right Column - Visual Elements */}
            <div className="space-y-6">
              {/* Session Fill Status */}
              <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="text-white flex items-center gap-2">
                    <Users className="h-5 w-5 text-green-400" />
                    Session Capacity
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="space-y-3">
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-400">Filled</span>
                      <span className="text-white">
                        {session.currentTesters} / {session.maxTesters}
                      </span>
                    </div>
                    <div className="w-full bg-slate-700 rounded-full h-3">
                      <div
                        className="bg-gradient-to-r from-green-500 to-blue-500 h-3 rounded-full transition-all duration-300"
                        style={{ width: `${fillPercentage}%` }}
                      />
                    </div>
                    <p className="text-slate-400 text-sm">
                      {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} remaining
                    </p>
                  </div>
                </CardContent>
              </Card>

              {/* Platform Support */}
              <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
                <CardHeader>
                  <CardTitle className="text-white flex items-center gap-2">
                    <Monitor className="h-5 w-5 text-blue-400" />
                    Platform Support
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="flex flex-wrap gap-2">
                    {session.platform.map((platform) => (
                      <Badge
                        key={platform}
                        variant="secondary"
                        className="bg-gradient-to-r from-blue-900/30 to-purple-900/30 text-blue-300 border-blue-600 px-3 py-1"
                      >
                        {platform}
                      </Badge>
                    ))}
                  </div>
                </CardContent>
              </Card>

              {/* Rewards */}
              {session.rewards && (
                <Card className="bg-gradient-to-br from-yellow-900/20 to-orange-900/20 border-yellow-700/50 backdrop-blur-sm">
                  <CardHeader>
                    <CardTitle className="text-white flex items-center gap-2">
                      <Trophy className="h-5 w-5 text-yellow-400" />
                      Rewards
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="flex items-center gap-3">
                      <div className="bg-gradient-to-r from-yellow-500 to-orange-500 p-2 rounded-full">
                        <Trophy className="h-4 w-4 text-white" />
                      </div>
                      <p className="text-yellow-300 font-semibold">{session.rewards.value}</p>
                    </div>
                  </CardContent>
                </Card>
              )}

              {/* Urgency Warning */}
              {isAlmostFull && session.status === 'open' && (
                <div className="bg-gradient-to-r from-orange-900/30 to-red-900/30 border border-orange-700 rounded-lg p-6 text-center backdrop-blur-sm">
                  <Zap className="h-8 w-8 text-orange-400 mx-auto mb-2" />
                  <p className="text-orange-300 font-bold text-lg">Almost Full!</p>
                  <p className="text-orange-400">
                    Only {spotsLeft} spot{spotsLeft !== 1 ? 's' : ''} left
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Details Section */}
      <div className="container mx-auto px-4 py-16">
        <div className="grid lg:grid-cols-3 gap-8">
          {/* Requirements */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white">Requirements</CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-3">
                {session.requirements.map((requirement, index) => (
                  <li key={index} className="flex items-start gap-3 text-slate-300">
                    <div className="bg-blue-600 rounded-full p-1 mt-0.5 flex-shrink-0">
                      <div className="w-2 h-2 bg-white rounded-full" />
                    </div>
                    {requirement}
                  </li>
                ))}
              </ul>
            </CardContent>
          </Card>

          {/* Session Type Details */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white flex items-center gap-2">
                {getSessionTypeIcon(session.sessionType)}
                Session Type
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <h4 className="text-white font-semibold mb-2 capitalize">{session.sessionType.replace('-', ' ')}</h4>
                <p className="text-slate-400 text-sm">
                  {session.sessionType === 'gameplay' && 'Focus on game mechanics, balance, and overall player experience'}
                  {session.sessionType === 'usability' && 'Evaluate user interface, user experience, and accessibility'}
                  {session.sessionType === 'bug-testing' && 'Systematically find and report bugs, glitches, and issues'}
                  {session.sessionType === 'feedback' && 'Provide detailed feedback on various game aspects'}
                </p>
              </div>
              <div>
                <h4 className="text-white font-semibold mb-2">Skill Level</h4>
                <Badge variant="outline" className="bg-purple-900/20 text-purple-400 border-purple-700">
                  {session.skillLevel}
                </Badge>
              </div>
            </CardContent>
          </Card>

          {/* How to Participate */}
          <Card className="bg-gradient-to-br from-slate-900/50 to-slate-800/50 border-slate-700 backdrop-blur-sm">
            <CardHeader>
              <CardTitle className="text-xl font-bold text-white">How to Participate</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                <div className="flex gap-3">
                  <div className="bg-blue-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold flex-shrink-0">1</div>
                  <p className="text-slate-300 text-sm">Join the session by clicking the button above</p>
                </div>
                <div className="flex gap-3">
                  <div className="bg-purple-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold flex-shrink-0">2</div>
                  <p className="text-slate-300 text-sm">Receive session details and download instructions</p>
                </div>
                <div className="flex gap-3">
                  <div className="bg-green-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold flex-shrink-0">3</div>
                  <p className="text-slate-300 text-sm">Participate in the testing session and provide feedback</p>
                </div>
                <div className="flex gap-3">
                  <div className="bg-yellow-600 text-white rounded-full w-6 h-6 flex items-center justify-center text-sm font-bold flex-shrink-0">4</div>
                  <p className="text-slate-300 text-sm">Earn rewards for your valuable contributions</p>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}
