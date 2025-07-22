'use client';

import { TestSession } from '@/lib/api/testing-lab/test-sessions';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Carousel, CarouselContent, CarouselItem, type CarouselApi } from '@/components/ui/carousel';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetDescription } from '@/components/ui/sheet';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
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
import { ArrowLeft, Calendar, Clock, GamepadIcon, Monitor, Target, Trophy, Users, Zap, Star, Info, FileText, ChevronLeft, ChevronRight } from 'lucide-react';
import { format } from 'date-fns';
import Link from 'next/link';
import { useState, useEffect, useCallback } from 'react';
import Autoplay from 'embla-carousel-autoplay';
import { JoinProcess } from './join-process';

interface SessionDetailProps {
  session: TestSession;
}

export function SessionDetail({ session }: SessionDetailProps) {
  const [showGameDetails, setShowGameDetails] = useState(false);
  const [showRequirements, setShowRequirements] = useState(false);
  const [showJoinModal, setShowJoinModal] = useState(false);
  const [carouselApi, setCarouselApi] = useState<CarouselApi>();
  const [currentGameIndex, setCurrentGameIndex] = useState(0);
  const [autoplayPlugin] = useState(() =>
    Autoplay({
      delay: 5000,
      stopOnInteraction: true,
      stopOnMouseEnter: true,
      stopOnFocusIn: true,
    }),
  );
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

  // Carousel API handlers
  const nextGame = useCallback(() => {
    if (carouselApi) {
      carouselApi.scrollNext();
    }
  }, [carouselApi]);

  const prevGame = useCallback(() => {
    if (carouselApi) {
      carouselApi.scrollPrev();
    }
  }, [carouselApi]);

  const selectGame = useCallback(
    (index: number) => {
      if (carouselApi) {
        carouselApi.scrollTo(index);
      }
    },
    [carouselApi],
  );

  // Listen to carousel changes
  useEffect(() => {
    if (!carouselApi) return;

    const onSelect = () => {
      setCurrentGameIndex(carouselApi.selectedScrollSnap());
    };

    carouselApi.on('select', onSelect);
    onSelect(); // Set initial state

    return () => {
      carouselApi.off('select', onSelect);
    };
  }, [carouselApi]);

  // Control autoplay based on sheet states
  useEffect(() => {
    if (!carouselApi) return;

    // Access the autoplay plugin directly from our reference
    if (showGameDetails || showRequirements) {
      autoplayPlugin.stop();
    } else {
      autoplayPlugin.play();
    }
  }, [carouselApi, showGameDetails, showRequirements, autoplayPlugin]);

  // Keyboard navigation
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (showGameDetails || showRequirements) return; // Don't navigate when sheets are open

      if (event.key === 'ArrowLeft') {
        event.preventDefault();
        prevGame();
      } else if (event.key === 'ArrowRight') {
        event.preventDefault();
        nextGame();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [showGameDetails, showRequirements, prevGame, nextGame]);

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
          <SidebarHeader className="px-3 py-4">
            <div className="flex items-center gap-3">
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
              <SidebarGroupLabel className="text-white font-semibold flex items-center gap-2 px-2 py-2">
                {getSessionTypeIcon(session.sessionType)}
                <span className="group-data-[collapsible=icon]:hidden">Session Info</span>
              </SidebarGroupLabel>

              <div className="space-y-6 p-3 group-data-[collapsible=icon]:hidden">
                <div className="space-y-4">
                  <div className="flex items-center gap-2">
                    <Badge variant="outline" className={getStatusColor(session.status) + ' text-sm px-3 py-1'}>
                      {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                    </Badge>
                    <Badge variant="outline" className="bg-blue-900/20 text-blue-400 border-blue-700 text-xs px-2 py-1">
                      {session.skillLevel}
                    </Badge>
                  </div>

                  <h1 className="text-xl font-bold bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">{session.title}</h1>

                  <div className="flex flex-col gap-3 text-sm text-slate-400">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-4 w-4" />
                      {format(sessionDate, 'MMM dd, h:mm a')}
                    </div>
                    <div className="flex items-center gap-2">
                      <Clock className="h-4 w-4" />
                      {session.duration}m
                    </div>
                  </div>

                  {/* Testers Progress Bar */}
                  <div className="space-y-3">
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

                  {/* Games Progress Bar */}
                  <div className="space-y-3">
                    <div className="flex justify-between text-xs text-slate-400">
                      <span>Games Capacity</span>
                      <span>
                        {session.currentGames}/{session.maxGames}
                      </span>
                    </div>
                    <div className="w-full bg-slate-700 rounded-full h-2">
                      <div
                        className="bg-gradient-to-r from-purple-500 to-pink-500 h-2 rounded-full transition-all duration-300"
                        style={{ width: `${(session.currentGames / session.maxGames) * 100}%` }}
                      />
                    </div>
                  </div>

                  {/* Join Session Button */}
                  <div className="pt-3">
                    <Button
                      asChild
                      className="w-full bg-gradient-to-r from-blue-600 to-purple-600 hover:from-blue-700 hover:to-purple-700 text-white border-0 h-10 group-data-[collapsible=icon]:px-2"
                      disabled={session.status !== 'open'}
                    >
                      <Link href={`/testing-lab/sessions/${session.slug}/join`}>
                        <Zap className="mr-2 h-4 w-4 group-data-[collapsible=icon]:mr-0" />
                        <span className="group-data-[collapsible=icon]:hidden">{session.status === 'open' ? 'Join Session' : 'Session Full'}</span>
                      </Link>
                    </Button>
                  </div>
                </div>
              </div>
            </SidebarGroup>

            {/* Separator */}
            <div className="h-px bg-slate-700/50 mx-3 group-data-[collapsible=icon]:hidden" />

            <SidebarGroup className="mt-6">
              <SidebarGroupLabel className="text-white font-semibold flex items-center gap-2 px-2 py-2">
                <GamepadIcon className="h-4 w-4 text-blue-400" />
                <span className="group-data-[collapsible=icon]:hidden">Games ({allGames.length})</span>
              </SidebarGroupLabel>

              <SidebarMenu className="space-y-4 px-2">
                {allGames.map((game, index) => (
                  <SidebarMenuItem key={game.id}>
                    <SidebarMenuButton
                      isActive={currentGameIndex === index}
                      className="group-data-[collapsible=icon]:justify-center h-auto py-4 cursor-pointer"
                      onClick={() => selectGame(index)}
                    >
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
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                ))}
              </SidebarMenu>

              {/* Submit Game CTA - Developer Spotlight */}
              <div className="px-2 pt-4 group-data-[collapsible=icon]:hidden">
                <div className="relative overflow-hidden rounded-xl bg-gradient-to-br from-purple-600/20 via-blue-600/20 to-cyan-500/20 border border-purple-500/30 backdrop-blur-sm">
                  {/* Animated background elements */}
                  <div className="absolute inset-0 bg-gradient-to-r from-purple-600/10 to-blue-600/10 animate-pulse" />
                  <div className="absolute top-0 right-0 w-20 h-20 bg-gradient-to-br from-yellow-400/20 to-orange-500/20 rounded-full blur-xl animate-bounce" />
                  <div className="absolute bottom-0 left-0 w-16 h-16 bg-gradient-to-tr from-pink-500/20 to-purple-600/20 rounded-full blur-lg animate-pulse delay-1000" />

                  {/* Content */}
                  <div className="relative z-10 p-4">
                    <div className="flex items-start gap-3 mb-3">
                      <div className="p-2 rounded-lg bg-gradient-to-br from-purple-500 to-blue-600 shadow-lg">
                        <GamepadIcon className="h-5 w-5 text-white" />
                      </div>
                      <div className="flex-1">
                        <h3 className="font-bold text-white text-sm mb-1">Got a Game?</h3>
                        <p className="text-xs text-slate-300 leading-relaxed">Get real feedback from players and level up your game!</p>
                      </div>
                    </div>

                    <div className="space-y-2 mb-4">
                      <div className="flex items-center gap-2 text-xs text-slate-400">
                        <div className="w-1.5 h-1.5 bg-green-400 rounded-full animate-pulse" />
                        <span>Free playtesting sessions</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-slate-400">
                        <div className="w-1.5 h-1.5 bg-blue-400 rounded-full animate-pulse delay-300" />
                        <span>Detailed feedback reports</span>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-slate-400">
                        <div className="w-1.5 h-1.5 bg-purple-400 rounded-full animate-pulse delay-500" />
                        <span>Connect with real gamers</span>
                      </div>
                    </div>

                    <Button
                      asChild
                      className="w-full bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700 text-white border-0 h-9 shadow-lg hover:shadow-purple-500/25 transition-all duration-300 group font-medium"
                    >
                      <Link href="/testing-lab/submit-game">Submit Your Game</Link>
                    </Button>
                  </div>
                </div>
              </div>
            </SidebarGroup>
          </SidebarContent>

          <SidebarFooter className="border-t border-slate-700/50">
            <div className="space-y-3 p-2">{/* Footer content can be added here if needed */}</div>
          </SidebarFooter>
        </Sidebar>

        {/* Main Content Area */}
        <SidebarInset className="flex-1">
          <div className="relative min-h-screen">
            {/* Header with sidebar trigger */}
            <div className="absolute top-4 left-4 z-20">
              <SidebarTrigger className="bg-slate-900/80 border-slate-700 text-slate-200 backdrop-blur-sm" />
            </div>

            {/* Full Screen Game Carousel */}
            <Carousel setApi={setCarouselApi} className="absolute inset-0" plugins={[autoplayPlugin]} opts={{ loop: true }}>
              <CarouselContent>
                {allGames.map((game, index) => (
                  <CarouselItem key={game.id}>
                    <div className="relative h-screen bg-gradient-to-br from-indigo-900/50 via-purple-900/30 to-blue-900/50">
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

                      {/* Game Info Overlay - Bottom */}
                      <div className="absolute bottom-0 left-0 right-0 bg-gradient-to-t from-slate-900/95 via-slate-900/80 to-transparent p-8">
                        <div className="max-w-4xl">
                          {/* Game Title */}
                          <div className="mb-6">
                            <div className="flex items-center gap-4 mb-4">
                              <div className="bg-black/40 backdrop-blur-sm rounded-lg px-3 py-1 border border-slate-700/50">
                                <p className="text-white font-semibold text-sm">
                                  {index + 1} / {allGames.length}
                                </p>
                              </div>
                            </div>

                            <h2 className="text-5xl md:text-6xl font-bold text-white mb-3">{game.title}</h2>
                            <p className="text-2xl text-blue-300 mb-4">by {game.developer}</p>
                            <p className="text-lg text-slate-300 leading-relaxed max-w-3xl mb-6">{game.description}</p>

                            {/* Action buttons for details and requirements */}
                            <div className="flex gap-3 mb-6">
                              <Button
                                variant="outline"
                                onClick={() => setShowGameDetails(true)}
                                className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80 hover:border-slate-500"
                              >
                                <Info className="h-4 w-4 mr-2" />
                                Game Details
                              </Button>
                              <Button
                                variant="outline"
                                onClick={() => setShowRequirements(true)}
                                className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80 hover:border-slate-500"
                              >
                                <FileText className="h-4 w-4 mr-2" />
                                Requirements
                              </Button>
                            </div>
                          </div>

                          {/* Game Details Grid */}
                          <div className="grid md:grid-cols-3 gap-6 mb-8">
                            {/* Genre */}
                            <div>
                              <h4 className="text-white font-semibold mb-2 text-sm">Genre</h4>
                              <div className="flex flex-wrap gap-2">
                                {game.genre?.map((genre) => (
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
                                {game.testingFocus?.map((focus) => (
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
                                {(('platforms' in game ? game.platforms : 'platform' in game ? game.platform : session.platform) as string[]).map(
                                  (platform: string) => (
                                    <Badge
                                      key={platform}
                                      variant="outline"
                                      className="bg-gradient-to-r from-green-900/30 to-emerald-900/30 text-green-300 border-green-600/50 text-sm px-3 py-1"
                                    >
                                      {platform}
                                    </Badge>
                                  ),
                                )}
                              </div>
                            </div>
                          </div>

                          {/* Rewards */}
                          {session.rewards && (
                            <div className="mb-8 inline-block bg-gradient-to-r from-yellow-900/20 to-orange-900/20 border border-yellow-700/50 rounded-lg p-4">
                              <div className="flex items-center gap-3">
                                <Trophy className="h-6 w-6 text-yellow-400" />
                                <div>
                                  <p className="text-yellow-300 font-semibold text-lg">{session.rewards.value}</p>
                                  <p className="text-yellow-400/80 text-sm">Testing Reward</p>
                                </div>
                              </div>
                            </div>
                          )}

                          {/* Carousel Navigation - Centered at bottom */}
                          {allGames.length > 1 && (
                            <div className="flex items-center justify-center gap-4 pt-6 mt-6 border-t border-slate-700/30">
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={prevGame}
                                className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80 hover:border-slate-500"
                              >
                                <ChevronLeft className="h-4 w-4" />
                              </Button>
                              <div className="flex gap-3">
                                {allGames.map((_, dotIndex) => (
                                  <button
                                    key={dotIndex}
                                    onClick={() => selectGame(dotIndex)}
                                    className={`w-3 h-3 rounded-full transition-all duration-300 ${
                                      index === dotIndex ? 'bg-blue-400 scale-125' : 'bg-slate-600 hover:bg-slate-400'
                                    }`}
                                  />
                                ))}
                              </div>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={nextGame}
                                className="bg-slate-800/80 border-slate-600 text-slate-200 hover:bg-slate-700/80 hover:border-slate-500"
                              >
                                <ChevronRight className="h-4 w-4" />
                              </Button>
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </CarouselItem>
                ))}
              </CarouselContent>
            </Carousel>
          </div>
        </SidebarInset>

        {/* Game Details Sheet */}
        <Sheet open={showGameDetails} onOpenChange={setShowGameDetails}>
          <SheetContent side="right" className="w-[400px] sm:w-[540px] bg-slate-900 border-slate-700 p-6">
            <SheetHeader className="pb-6 border-b border-slate-700/50">
              <SheetTitle className="text-white text-2xl font-bold">Game Details</SheetTitle>
              <SheetDescription className="text-slate-400 text-base mt-2">Detailed information about {allGames[currentGameIndex]?.title}</SheetDescription>
            </SheetHeader>

            <div className="space-y-8 mt-8 pb-6">
              <div className="space-y-8 mt-8 pb-6">
                <div className="space-y-6">
                  <div className="bg-slate-800/30 rounded-lg p-5 border border-slate-700/30">
                    <h3 className="text-xl font-bold text-white mb-3">{allGames[currentGameIndex]?.title}</h3>
                    <p className="text-blue-300 mb-4 text-lg">by {allGames[currentGameIndex]?.developer}</p>
                    <p className="text-slate-300 leading-relaxed text-base">{allGames[currentGameIndex]?.description}</p>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                    <div className="bg-slate-800/30 rounded-lg p-5 border border-slate-700/30">
                      <h4 className="text-white font-semibold mb-4 text-lg">Genre</h4>
                      <div className="space-y-2">
                        {allGames[currentGameIndex]?.genre?.map((genre) => (
                          <span key={genre} className="block text-sm text-purple-300 bg-purple-900/30 px-3 py-2 rounded-md border border-purple-700/30">
                            {genre}
                          </span>
                        ))}
                      </div>
                    </div>

                    <div className="bg-slate-800/30 rounded-lg p-5 border border-slate-700/30">
                      <h4 className="text-white font-semibold mb-4 text-lg">Platforms</h4>
                      <div className="space-y-2">
                        {(
                          ('platforms' in allGames[currentGameIndex]
                            ? allGames[currentGameIndex].platforms
                            : 'platform' in allGames[currentGameIndex]
                              ? allGames[currentGameIndex].platform
                              : session.platform) as string[]
                        ).map((platform: string) => (
                          <span key={platform} className="block text-sm text-green-300 bg-green-900/30 px-3 py-2 rounded-md border border-green-700/30">
                            {platform}
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  <div className="bg-slate-800/30 rounded-lg p-5 border border-slate-700/30">
                    <h4 className="text-white font-semibold mb-4 text-lg">Testing Focus Areas</h4>
                    <div className="space-y-3">
                      {allGames[currentGameIndex]?.testingFocus?.map((focus) => (
                        <div
                          key={focus}
                          className="flex items-center gap-3 text-sm text-blue-300 bg-blue-900/30 px-4 py-3 rounded-md border border-blue-700/30"
                        >
                          <Target className="h-5 w-5 flex-shrink-0" />
                          <span className="font-medium">{focus}</span>
                        </div>
                      ))}
                    </div>
                  </div>

                  <div className="bg-slate-800/30 rounded-lg p-5 border border-slate-700/30">
                    <h4 className="text-white font-semibold mb-4 text-lg">Session Information</h4>
                    <div className="space-y-4 text-sm text-slate-300">
                      <div className="flex justify-between items-center py-2 border-b border-slate-700/30">
                        <span className="font-medium">Session Type:</span>
                        <span className="text-blue-300 capitalize font-medium">{session.sessionType.replace('-', ' ')}</span>
                      </div>
                      <div className="flex justify-between items-center py-2 border-b border-slate-700/30">
                        <span className="font-medium">Duration:</span>
                        <span className="text-blue-300 font-medium">{session.duration} minutes</span>
                      </div>
                      <div className="flex justify-between items-center py-2 border-b border-slate-700/30">
                        <span className="font-medium">Skill Level:</span>
                        <span className="text-blue-300 capitalize font-medium">{session.skillLevel}</span>
                      </div>
                      <div className="flex justify-between items-center py-2">
                        <span className="font-medium">Mode:</span>
                        <span className="text-blue-300 font-medium">{session.isOnline ? 'Online' : 'On-site'}</span>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </SheetContent>
        </Sheet>

        {/* Testing Requirements Sheet */}
        <Sheet open={showRequirements} onOpenChange={setShowRequirements}>
          <SheetContent side="right" className="w-[400px] sm:w-[540px] bg-slate-900 border-slate-700 p-6">
            <SheetHeader className="pb-6 border-b border-slate-700/50">
              <SheetTitle className="text-white text-2xl font-bold">Testing Requirements</SheetTitle>
              <SheetDescription className="text-slate-400 text-base mt-2">Requirements and expectations for this testing session</SheetDescription>
            </SheetHeader>

            <div className="space-y-8 mt-8 pb-6">
              <div className="space-y-8 mt-8 pb-6">
                <div className="bg-slate-800/30 rounded-lg p-6 border border-slate-700/30">
                  <h3 className="text-xl font-bold text-white mb-6">Session Requirements</h3>
                  <div className="space-y-4">
                    {session.requirements.map((requirement, index) => (
                      <div key={index} className="flex items-start gap-4 p-4 bg-slate-700/30 rounded-lg border border-slate-600/30">
                        <div className="w-3 h-3 bg-blue-400 rounded-full mt-2 flex-shrink-0" />
                        <span className="text-slate-300 text-base leading-relaxed">{requirement}</span>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-slate-800/30 rounded-lg p-6 border border-slate-700/30">
                  <h3 className="text-xl font-bold text-white mb-6">What to Expect</h3>
                  <div className="space-y-6 text-base text-slate-300">
                    <div className="flex items-start gap-4 p-4 bg-slate-700/30 rounded-lg border border-slate-600/30">
                      <Clock className="h-6 w-6 text-blue-400 mt-1 flex-shrink-0" />
                      <div>
                        <p className="font-semibold text-white mb-2">Duration</p>
                        <p className="leading-relaxed">This session will run for approximately {session.duration} minutes</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-4 p-4 bg-slate-700/30 rounded-lg border border-slate-600/30">
                      <Users className="h-6 w-6 text-green-400 mt-1 flex-shrink-0" />
                      <div>
                        <p className="font-semibold text-white mb-2">Participants</p>
                        <p className="leading-relaxed">Up to {session.maxTesters} testers will participate in this session</p>
                      </div>
                    </div>

                    <div className="flex items-start gap-4 p-4 bg-slate-700/30 rounded-lg border border-slate-600/30">
                      <Target className="h-6 w-6 text-purple-400 mt-1 flex-shrink-0" />
                      <div>
                        <p className="font-semibold text-white mb-2">Focus</p>
                        <p className="leading-relaxed">Primary focus will be on {session.sessionType.replace('-', ' ')} testing</p>
                      </div>
                    </div>

                    {session.rewards && (
                      <div className="flex items-start gap-4 p-4 bg-gradient-to-r from-yellow-900/30 to-orange-900/30 rounded-lg border border-yellow-700/50">
                        <Trophy className="h-6 w-6 text-yellow-400 mt-1 flex-shrink-0" />
                        <div>
                          <p className="font-semibold text-white mb-2">Rewards</p>
                          <p className="text-yellow-200 leading-relaxed">{session.rewards.value}</p>
                        </div>
                      </div>
                    )}
                  </div>
                </div>

                <div className="bg-blue-900/30 border border-blue-700/50 rounded-lg p-6">
                  <h4 className="text-blue-300 font-semibold mb-4 text-lg">Session Status</h4>
                  <div className="flex items-center gap-3">
                    <Badge className={getStatusColor(session.status) + ' text-base px-4 py-2'}>
                      {session.status.charAt(0).toUpperCase() + session.status.slice(1)}
                    </Badge>
                    <span className="text-slate-400 text-base">{spotsLeft > 0 ? `${spotsLeft} spots remaining` : 'Session is full'}</span>
                  </div>
                </div>
              </div>
            </div>
          </SheetContent>
        </Sheet>

        {/* Join Session Modal */}
        <Dialog open={showJoinModal} onOpenChange={setShowJoinModal}>
          <DialogContent className="max-w-5xl h-[85vh] bg-transparent border-0 overflow-hidden p-0 shadow-2xl">
            <DialogHeader className="sr-only">
              <DialogTitle>Join Testing Session</DialogTitle>
            </DialogHeader>
            <div className="h-full overflow-y-auto rounded-lg border border-slate-700/50 bg-gradient-to-br from-slate-950 via-slate-900 to-slate-950">
              <JoinProcess sessionSlug={session.slug} />
            </div>
          </DialogContent>
        </Dialog>
      </div>
    </SidebarProvider>
  );
}
