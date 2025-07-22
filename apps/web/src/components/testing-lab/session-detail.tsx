'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '@/components/ui/sheet';
import {
  Sidebar,
  SidebarContent,
  SidebarProvider,
  SidebarTrigger,
  SidebarHeader,
  SidebarFooter,
  SidebarMenu,
  SidebarMenuItem,
  SidebarMenuButton,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarInset,
} from '@/components/ui/sidebar';
import { ArrowLeft, Calendar, Clock, GamepadIcon, Monitor, Target, Trophy, Users, Zap, Star, Play, Info, FileText } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';
import { useState } from 'react';

interface SessionDetailProps {
  session: TestSession;
  selectedGameId?: string;
}

export function SessionDetail({ session, selectedGameId }: SessionDetailProps) {
  const [showGameDetails, setShowGameDetails] = useState(false);
  const [showRequirements, setShowRequirements] = useState(false);
  const sessionDate = new Date(session.sessionDate);
  const spotsLeft = session.maxTesters - session.currentTesters;
  const fillPercentage = (session.currentTesters / session.maxTesters) * 100;

  // Get all games (main game + featured games)
  const allGames = [
    {
      id: 'main',
      title: session.gameTitle,
      developer: session.gameDeveloper,
      description: session.description,
      genre: ['Action', 'Adventure'], // Default genres
      status: 'primary' as const,
      testingFocus: [session.sessionType.replace('-', ' ')],
      platforms: session.platform,
    },
    ...(session.featuredGames || []),
  ];

  // Find current game by ID or default to first game
  const selectedGameIndex = selectedGameId ? allGames.findIndex((game) => game.id === selectedGameId) : 0;
  const currentGameIndex = selectedGameIndex >= 0 ? selectedGameIndex : 0;
  const currentGame = allGames[currentGameIndex];

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
    <SidebarProvider defaultOpen={true}>
      <div className="min-h-screen bg-gradient-to-br from-slate-950 via-blue-950/30 to-purple-950/40 flex w-full">
        {/* Collapsible Sidebar */}
        <Sidebar collapsible="icon" className="border-r border-slate-700/50">
          <SidebarHeader>
            <div className="flex items-center gap-2 px-2 py-1">
              <Button asChild variant="ghost" size="sm" className="text-slate-400 hover:text-white">
                <Link href="/testing-lab">
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  <span className="group-data-[collapsible=icon]:hidden">Back to Sessions</span>
                </Link>
              </Button>
            </div>
          </SidebarHeader>

          <SidebarContent className="bg-gradient-to-b from-slate-900/95 via-slate-900/90 to-slate-800/95 backdrop-blur-xl">
            <SidebarGroup>
              <SidebarGroupLabel className="text-white font-semibold flex items-center gap-2">
                {getSessionTypeIcon(session.sessionType)}
                <span className="group-data-[collapsible=icon]:hidden">Session Info</span>
              </SidebarGroupLabel>

              <div className="space-y-4 p-2 group-data-[collapsible=icon]:hidden">
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className={getStatusColor(session.status) + ' text-sm px-3 py-1'}>
                      {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                    </Badge>
                    <Badge variant="outline" className="bg-blue-900/20 text-blue-400 border-blue-700 text-xs px-2 py-1">
                      {session.skillLevel}
                    </Badge>
                  </div>

                  <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">{session.title}</h1>

                  <div className="flex flex-col gap-2 text-sm text-slate-400">
                    <div className="flex items-center gap-1">
                      <Calendar className="h-4 w-4" />
                      {format(sessionDate, 'MMM dd, h:mm a')}
                    </div>
                    <div className="flex items-center gap-1">
                      <Clock className="h-4 w-4" />
                      {session.duration}m
                    </div>
                  </div>

                  {/* Progress Bar */}
                  <div className="space-y-2">
                    <div className="flex justify-between text-xs text-slate-400">
                      <span>Session Capacity</span>
                      <span>
                        {session.currentTesters}/{session.maxTesters}
                      </span>
                    </div>
                    <div className="w-full bg-slate-700 rounded-full h-2">
                      <div
                        className="bg-gradient-to-r from-green-500 to-blue-500 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${fillPercentage}%` }}
                      />
                    </div>
                  </div>
                </div>
              </div>
            </SidebarGroup>

            <SidebarGroup>
              <SidebarGroupLabel className="text-white font-semibold flex items-center gap-2">
                <GamepadIcon className="h-4 w-4 text-blue-400" />
                <span className="group-data-[collapsible=icon]:hidden">Games ({allGames.length})</span>
              </SidebarGroupLabel>

              <SidebarMenu>
                {allGames.map((game, index) => (
                  <SidebarMenuItem key={game.id}>
                    <SidebarMenuButton asChild isActive={currentGameIndex === index} className="group-data-[collapsible=icon]:justify-center">
                      <Link href={`?game=${game.id}`}>
                        <div className="flex items-center gap-3 w-full">
                          <div className={`p-2 rounded-lg ${currentGameIndex === index ? 'bg-gradient-to-r from-blue-600 to-purple-600' : 'bg-slate-700'}`}>
                            <GamepadIcon className="h-4 w-4 text-white" />
                          </div>

                          <div className="flex-1 min-w-0 group-data-[collapsible=icon]:hidden">
                            <h4 className="font-semibold text-sm truncate text-slate-300">{game.title}</h4>
                            <p className="text-xs truncate text-slate-500">by {game.developer}</p>
                          </div>

                          {game.status === 'primary' && <Star className="h-3 w-3 text-yellow-400 flex-shrink-0 group-data-[collapsible=icon]:hidden" />}
                        </div>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>
            </SidebarGroup>
          </SidebarContent>

          <SidebarFooter className="border-t border-slate-700/50">
            <div className="space-y-3 p-2">
              <Button
                asChild
                className="w-full bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white border-0 h-11 group-data-[collapsible=icon]:px-2"
                disabled={session.status !== 'open'}
              >
                <Link href={`/testing-lab/sessions/${session.slug}/join`}>
                  <Zap className="mr-2 h-4 w-4 group-data-[collapsible=icon]:mr-0" />
                  <span className="group-data-[collapsible=icon]:hidden">{session.status === 'open' ? 'Join Session' : 'Session Full'}</span>
                </Link>
              </Button>
            </div>
          </SidebarFooter>
        </Sidebar>

        {/* Main Content Area */}
        <SidebarInset className="flex-1">
          <div className="relative min-h-screen">
            {/* Header with sidebar trigger */}
            <div className="absolute top-4 left-4 z-20">
              <SidebarTrigger className="bg-slate-900/80 border-slate-700 text-slate-200 backdrop-blur-sm" />
            </div>

            {/* Full Screen Game Hero */}
            <div className="absolute inset-0 bg-gradient-to-br from-indigo-900/50 via-purple-900/30 to-blue-900/50">
              {/* Animated Background Pattern */}
              <div className="absolute inset-0 opacity-10">
                <div className="absolute inset-0 bg-[linear-gradient(45deg,transparent_25%,rgba(68,71,84,0.1)_25%,rgba(68,71,84,0.1)_50%,transparent_50%,transparent_75%,rgba(68,71,84,0.1)_75%)] bg-[length:40px_40px] animate-pulse" />
              </div>

              {/* Game Character/Art Placeholder */}
              <div className="absolute inset-0 flex items-center justify-center">
                <div className="text-center space-y-6">
                  <div className="relative">
                    <GamepadIcon className="h-32 w-32 text-blue-400/60 mx-auto mb-6" />
                    <div className="absolute inset-0 bg-gradient-to-r from-blue-600/20 to-purple-600/20 rounded-full blur-xl scale-150" />
                  </div>
                </div>
              </div>
            </div>

            {/* Game Info Overlay - Bottom */}
            <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-slate-900/95 via-slate-900/80 to-transparent p-8">
              <div className="max-w-4xl">
                {/* Game Title */}
                <div className="mb-6">
                  <div className="flex items-center gap-4 mb-4">
                    <Badge
                      className={`text-white border-0 text-lg px-4 py-2 ${
                        currentGame.status === 'primary'
                          ? 'bg-gradient-to-r from-green-600/80 to-emerald-600/80'
                          : 'bg-gradient-to-r from-blue-600/80 to-cyan-600/80'
                      }`}
                    >
                      <Play className="h-4 w-4 mr-2" />
                      {currentGame.status === 'primary' ? 'Main Game' : 'Featured'}
                    </Badge>

                    <div className="bg-black/40 backdrop-blur-sm rounded-lg px-3 py-1 border border-slate-700/50">
                      <p className="text-white font-semibold text-sm">
                        {currentGameIndex + 1} / {allGames.length}
                      </p>
                    </div>

                    {/* Action buttons for details and requirements */}
                    <div className="flex gap-2 ml-auto">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setShowGameDetails(true)}
                        className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80"
                      >
                        <Info className="h-4 w-4 mr-2" />
                        Game Details
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setShowRequirements(true)}
                        className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80"
                      >
                        <FileText className="h-4 w-4 mr-2" />
                        Requirements
                      </Button>
                    </div>
                  </div>

                  <h2 className="text-5xl md:text-6xl font-bold text-white mb-3">{currentGame.title}</h2>
                  <p className="text-2xl text-blue-300 mb-4">by {currentGame.developer}</p>
                  <p className="text-lg text-slate-300 leading-relaxed max-w-3xl">{currentGame.description}</p>
                </div>

                {/* Game Details Grid */}
                <div className="grid md:grid-cols-3 gap-6">
                  {/* Genre */}
                  <div>
                    <h4 className="text-white font-semibold mb-2 text-sm">Genre</h4>
                    <div className="flex flex-wrap gap-2">
                      {currentGame.genre?.map((genre) => (
                        <Badge
                          key={genre}
                          variant="outline"
                          className="bg-gradient-to-r from-purple-900/30 to-pink-900/30 text-purple-300 border-purple-600/50 text-sm px-3 py-1"
                        >
                          {genre}
                        </Badge>
                      ))}
                    </div>
                  </div>

                  {/* Testing Focus */}
                  <div>
                    <h4 className="text-white font-semibold mb-2 text-sm flex items-center gap-1">
                      <Target className="h-4 w-4 text-blue-400" />
                      Testing Focus
                    </h4>
                    <div className="flex flex-wrap gap-2">
                      {currentGame.testingFocus?.map((focus) => (
                        <span key={focus} className="bg-blue-900/20 text-blue-300 text-sm px-3 py-1 rounded border border-blue-700/30">
                          {focus}
                        </span>
                      ))}
                    </div>
                  </div>

                  {/* Platforms */}
                  <div>
                    <h4 className="text-white font-semibold mb-2 text-sm flex items-center gap-1">
                      <Monitor className="h-4 w-4 text-green-400" />
                      Platforms
                    </h4>
                    <div className="flex flex-wrap gap-2">
                      {(
                        ('platforms' in currentGame ? currentGame.platforms : 'platform' in currentGame ? currentGame.platform : session.platform) as string[]
                      ).map((platform: string) => (
                        <Badge
                          key={platform}
                          variant="outline"
                          className="bg-gradient-to-r from-green-900/30 to-emerald-900/30 text-green-300 border-green-600/50 text-sm px-3 py-1"
                        >
                          {platform}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Rewards */}
                {session.rewards && (
                  <div className="mt-6 inline-block bg-gradient-to-r from-yellow-900/20 to-orange-900/20 border border-yellow-700/50 rounded-lg p-4">
                    <div className="flex items-center gap-3">
                      <Trophy className="h-6 w-6 text-yellow-400" />
                      <div>
                        <p className="text-yellow-300 font-semibold text-lg">{session.rewards.value}</p>
                        <p className="text-yellow-400/80 text-sm">Testing Reward</p>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </SidebarInset>

        {/* Game Details Sheet */}
        <Sheet open={showGameDetails} onOpenChange={setShowGameDetails}>
          <SheetContent side="right" className="w-[400px] sm:w-[540px] bg-slate-900 border-slate-700">
            <SheetHeader>
              <SheetTitle className="text-white text-xl">Game Details</SheetTitle>
              <SheetDescription className="text-slate-400">Detailed information about {currentGame.title}</SheetDescription>
            </SheetHeader>

            <div className="space-y-6 mt-6">
              <div className="space-y-4">
                <div>
                  <h3 className="text-lg font-semibold text-white mb-2">{currentGame.title}</h3>
                  <p className="text-blue-300 mb-4">by {currentGame.developer}</p>
                  <p className="text-slate-300 leading-relaxed">{currentGame.description}</p>
                </div>

                <div className="grid grid-cols-2 gap-4">
                  <div>
                    <h4 className="text-white font-medium mb-2">Genre</h4>
                    <div className="space-y-1">
                      {currentGame.genre?.map((genre) => (
                        <span key={genre} className="block text-sm text-purple-300 bg-purple-900/20 px-2 py-1 rounded">
                          {genre}
                        </span>
                      ))}
                    </div>
                  </div>

                  <div>
                    <h4 className="text-white font-medium mb-2">Platforms</h4>
                    <div className="space-y-1">
                      {(
                        ('platforms' in currentGame ? currentGame.platforms : 'platform' in currentGame ? currentGame.platform : session.platform) as string[]
                      ).map((platform: string) => (
                        <span key={platform} className="block text-sm text-green-300 bg-green-900/20 px-2 py-1 rounded">
                          {platform}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>

                <div>
                  <h4 className="text-white font-medium mb-2">Testing Focus Areas</h4>
                  <div className="space-y-2">
                    {currentGame.testingFocus?.map((focus) => (
                      <div key={focus} className="flex items-center gap-2 text-sm text-blue-300 bg-blue-900/20 px-3 py-2 rounded">
                        <Target className="h-4 w-4" />
                        {focus}
                      </div>
                    ))}
                  </div>
                </div>

                <div>
                  <h4 className="text-white font-medium mb-2">Session Information</h4>
                  <div className="space-y-2 text-sm text-slate-300">
                    <div className="flex justify-between">
                      <span>Session Type:</span>
                      <span className="text-blue-300 capitalize">{session.sessionType.replace('-', ' ')}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Duration:</span>
                      <span className="text-blue-300">{session.duration} minutes</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Skill Level:</span>
                      <span className="text-blue-300 capitalize">{session.skillLevel}</span>
                    </div>
                    <div className="flex justify-between">
                      <span>Mode:</span>
                      <span className="text-blue-300">{session.isOnline ? 'Online' : 'On-site'}</span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </SheetContent>
        </Sheet>

        {/* Testing Requirements Sheet */}
        <Sheet open={showRequirements} onOpenChange={setShowRequirements}>
          <SheetContent side="right" className="w-[400px] sm:w-[540px] bg-slate-900 border-slate-700">
            <SheetHeader>
              <SheetTitle className="text-white text-xl">Testing Requirements</SheetTitle>
              <SheetDescription className="text-slate-400">Requirements and expectations for this testing session</SheetDescription>
            </SheetHeader>

            <div className="space-y-6 mt-6">
              <div>
                <h3 className="text-lg font-semibold text-white mb-4">Session Requirements</h3>
                <div className="space-y-3">
                  {session.requirements.map((requirement, index) => (
                    <div key={index} className="flex items-start gap-3 p-3 bg-slate-800/50 rounded-lg">
                      <div className="w-2 h-2 bg-blue-400 rounded-full mt-2 flex-shrink-0" />
                      <span className="text-slate-300">{requirement}</span>
                    </div>
                  ))}
                </div>
              </div>

              <div>
                <h3 className="text-lg font-semibold text-white mb-4">What to Expect</h3>
                <div className="space-y-4 text-sm text-slate-300">
                  <div className="flex items-start gap-3">
                    <Clock className="h-5 w-5 text-blue-400 mt-0.5" />
                    <div>
                      <p className="font-medium text-white">Duration</p>
                      <p>This session will run for approximately {session.duration} minutes</p>
                    </div>
                  </div>

                  <div className="flex items-start gap-3">
                    <Users className="h-5 w-5 text-green-400 mt-0.5" />
                    <div>
                      <p className="font-medium text-white">Participants</p>
                      <p>Up to {session.maxTesters} testers will participate in this session</p>
                    </div>
                  </div>

                  <div className="flex items-start gap-3">
                    <Target className="h-5 w-5 text-purple-400 mt-0.5" />
                    <div>
                      <p className="font-medium text-white">Focus</p>
                      <p>Primary focus will be on {session.sessionType.replace('-', ' ')} testing</p>
                    </div>
                  </div>

                  {session.rewards && (
                    <div className="flex items-start gap-3">
                      <Trophy className="h-5 w-5 text-yellow-400 mt-0.5" />
                      <div>
                        <p className="font-medium text-white">Rewards</p>
                        <p>{session.rewards.value}</p>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="bg-blue-900/20 border border-blue-700/50 rounded-lg p-4">
                <h4 className="text-blue-300 font-medium mb-2">Session Status</h4>
                <div className="flex items-center gap-2">
                  <Badge className={getStatusColor(session.status)}>{session.status.charAt(0).toUpperCase() + session.status.slice(1)}</Badge>
                  <span className="text-sm text-slate-400">{spotsLeft > 0 ? `${spotsLeft} spots remaining` : 'Session is full'}</span>
                </div>
              </div>
            </div>
          </SheetContent>
        </Sheet>
      </div>
    </SidebarProvider>
  );
}
