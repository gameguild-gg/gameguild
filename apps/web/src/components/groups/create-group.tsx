'use client';

import { useState } from 'react';
import { Plus, Users, MessageCircle, Trophy, BarChart3, Lock } from 'lucide-react';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent } from '@game-guild/ui/components/card';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';

interface GroupMember {
  id: string;
  name: string;
  avatar?: string;
  initials: string;
  color: string;
}

interface Group {
  id: string;
  name: string;
  description: string;
  members: GroupMember[];
  isActive?: boolean;
}

interface CreateGroupProps {
  groups?: Group[];
  onCreateGroup?: () => void;
  showFeatures?: boolean;
}

export function CreateGroup({ 
  groups = [], 
  onCreateGroup,
  showFeatures = false 
}: CreateGroupProps) {
  const defaultGroups: Group[] = [
    {
      id: '1',
      name: 'Design Team',
      description: 'Creative design collaboration',
      members: [
        { id: '1', name: 'Sarah Chen', initials: 'SC', color: 'bg-orange-500', avatar: undefined },
        { id: '2', name: 'Alex Rivera', initials: 'AR', color: 'bg-yellow-500', avatar: undefined }
      ]
    },
    {
      id: '2', 
      name: 'Development Squad',
      description: 'Frontend and backend development',
      members: [
        { id: '3', name: 'John Smith', initials: 'JS', color: 'bg-green-500', avatar: undefined },
        { id: '4', name: 'Maria Garcia', initials: 'MG', color: 'bg-gray-600', avatar: undefined }
      ]
    },
    {
      id: '3',
      name: 'Product Team',
      description: 'Product strategy and planning',
      members: [
        { id: '5', name: 'David Kim', initials: 'DK', color: 'bg-blue-500', avatar: undefined }
      ]
    }
  ];

  const displayGroups = groups.length > 0 ? groups : defaultGroups;

  const GroupCard = ({ group }: { group: Group }) => (
    <Card className="bg-background border-border hover:shadow-md transition-shadow cursor-pointer">
      <CardContent className="p-4">
        <div className="flex items-center justify-between">
          <div className="flex-1">
            <div className="h-3 bg-muted rounded mb-2 w-3/4"></div>
            <div className="h-2 bg-muted/60 rounded w-1/2"></div>
          </div>
          <div className="flex -space-x-2">
            {group.members.slice(0, 3).map((member, index) => (
              <Avatar key={member.id} className="w-8 h-8 border-2 border-background">
                <AvatarImage src={member.avatar} alt={member.name} />
                <AvatarFallback className={`${member.color} text-white text-xs font-medium`}>
                  {member.initials}
                </AvatarFallback>
              </Avatar>
            ))}
            {group.members.length > 3 && (
              <div className="w-8 h-8 rounded-full bg-muted border-2 border-background flex items-center justify-center">
                <span className="text-xs font-medium text-muted-foreground">
                  +{group.members.length - 3}
                </span>
              </div>
            )}
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const EmptyStateCard = ({ index }: { index: number }) => {
    const colors = [
      ['bg-orange-400', 'bg-yellow-400'],
      ['bg-pink-400', 'bg-blue-400'], 
      ['bg-green-400', 'bg-purple-400']
    ];
    
    return (
      <Card className="bg-muted/30 border-border/40">
        <CardContent className="p-4">
          <div className="flex items-center justify-between">
            <div className="flex-1">
              <div className="h-3 bg-muted/60 rounded mb-2 w-3/4"></div>
              <div className="h-2 bg-muted/40 rounded w-1/2"></div>
            </div>
            <div className="flex -space-x-2">
              {colors[index]?.map((color, colorIndex) => (
                <div
                  key={colorIndex}
                  className={`w-8 h-8 rounded-full border-2 border-background ${color}`}
                />
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    );
  };

  if (displayGroups.length === 0 || showFeatures) {
    return (
      <div className="max-w-2xl mx-auto text-center space-y-8 p-8">
        {/* Empty State Cards */}
        <div className="space-y-4">
          {[0, 1, 2].map((index) => (
            <EmptyStateCard key={index} index={index} />
          ))}
        </div>

        {/* Main Content */}
        <div className="space-y-4">
          <h2 className="text-2xl font-semibold text-foreground">
            {showFeatures ? 'Start by creating a group' : 'Create a Group'}
          </h2>
          <p className="text-muted-foreground max-w-lg mx-auto">
            {showFeatures 
              ? 'Creating a group makes working in teams easier. Share content with the people who need it and find content where you need it. Just like that.'
              : 'A group easily allows you to share Content, Inputs and Templates with the right people quicker.'
            }
          </p>
        </div>

        {/* Create Button */}
        <Button onClick={onCreateGroup} className="bg-primary hover:bg-primary/90 text-primary-foreground">
          <Plus className="w-4 h-4 mr-2" />
          {showFeatures ? 'Create a group' : 'Create a new Group'}
        </Button>

        {/* Feature Cards (if showFeatures is true) */}
        {showFeatures && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-12">
            {/* Enhanced Teams */}
            <Card className="bg-background border-border text-left">
              <CardContent className="p-6">
                <div className="mb-4">
                  <div className="relative">
                    {/* Team avatars cluster */}
                    <div className="flex items-center justify-center mb-4">
                      <div className="relative">
                        <Avatar className="w-12 h-12 absolute -top-2 -left-2 border-2 border-background">
                          <AvatarFallback className="bg-red-500 text-white">SC</AvatarFallback>
                        </Avatar>
                        <Avatar className="w-12 h-12 absolute -top-2 left-6 border-2 border-background">
                          <AvatarFallback className="bg-blue-500 text-white">AR</AvatarFallback>
                        </Avatar>
                        <Avatar className="w-12 h-12 absolute top-4 left-2 border-2 border-background">
                          <AvatarFallback className="bg-green-500 text-white">JS</AvatarFallback>
                        </Avatar>
                        <Avatar className="w-8 h-8 absolute top-2 right-2 border-2 border-background bg-green-500">
                          <AvatarFallback className="bg-green-500 text-white text-xs">
                            <MessageCircle className="w-3 h-3" />
                          </AvatarFallback>
                        </Avatar>
                        <div className="w-16 h-16"></div> {/* Spacer */}
                      </div>
                    </div>
                  </div>
                </div>
                <h3 className="text-lg font-semibold text-foreground mb-2">Enhanced teams</h3>
                <p className="text-sm text-muted-foreground">
                  Access enhanced team features like voice chat and much more.
                </p>
              </CardContent>
            </Card>

            {/* Expert Feedback */}
            <Card className="bg-background border-border text-left">
              <CardContent className="p-6">
                <div className="mb-4">
                  <div className="flex items-center justify-center mb-4">
                    <div className="relative">
                      <Avatar className="w-10 h-10 border-2 border-background">
                        <AvatarFallback className="bg-orange-500 text-white">EX</AvatarFallback>
                      </Avatar>
                      <div className="absolute -right-2 -top-1 bg-green-500 rounded-lg px-3 py-1">
                        <div className="h-2 bg-white/20 rounded w-8 mb-1"></div>
                        <div className="h-2 bg-white/40 rounded w-6"></div>
                      </div>
                    </div>
                  </div>
                </div>
                <h3 className="text-lg font-semibold text-foreground mb-2">Expert feedback</h3>
                <p className="text-sm text-muted-foreground">
                  Get expert feedback on every project you submit to a competition.
                </p>
              </CardContent>
            </Card>

            {/* Leaderboards */}
            <Card className="bg-background border-border text-left">
              <CardContent className="p-6">
                <div className="mb-4">
                  <div className="space-y-2">
                    <div className="flex items-center gap-3 p-2 bg-red-500/10 rounded-lg">
                      <Avatar className="w-8 h-8">
                        <AvatarFallback className="bg-red-500 text-white text-xs">JL</AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <div className="text-sm font-medium text-foreground">Jo Ling</div>
                      </div>
                      <Trophy className="w-4 h-4 text-yellow-500" />
                    </div>
                    <div className="flex items-center gap-3 p-2 bg-green-500/10 rounded-lg">
                      <Avatar className="w-8 h-8">
                        <AvatarFallback className="bg-green-500 text-white text-xs">JA</AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <div className="text-sm font-medium text-foreground">John Antler</div>
                      </div>
                      <div className="w-2 h-2 bg-yellow-500 rounded-full"></div>
                    </div>
                    <div className="flex items-center gap-3 p-2 bg-blue-500/10 rounded-lg">
                      <Avatar className="w-8 h-8">
                        <AvatarFallback className="bg-blue-500 text-white text-xs">SP</AvatarFallback>
                      </Avatar>
                      <div className="flex-1">
                        <div className="text-sm font-medium text-foreground">Stacey Pi</div>
                      </div>
                      <div className="w-2 h-2 bg-gray-400 rounded-full"></div>
                    </div>
                  </div>
                </div>
                <h3 className="text-lg font-semibold text-foreground mb-2">Leaderboards</h3>
                <p className="text-sm text-muted-foreground">
                  Get a full leaderboard to see how you rank across the world.
                </p>
              </CardContent>
            </Card>

            {/* Unlock Stats */}
            <Card className="bg-background border-border text-left">
              <CardContent className="p-6">
                <div className="mb-4">
                  <div className="relative bg-muted/20 rounded-lg p-4 h-20 overflow-hidden">
                    {/* Chart representation */}
                    <div className="absolute inset-0 flex items-end justify-center p-4">
                      <svg className="w-full h-full" viewBox="0 0 100 40">
                        <polyline
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="2"
                          className="text-green-500"
                          points="0,35 20,30 40,20 60,15 80,10 100,5"
                        />
                        <circle cx="60" cy="15" r="2" className="fill-green-500" />
                      </svg>
                      <Lock className="absolute top-2 right-2 w-4 h-4 text-muted-foreground" />
                    </div>
                  </div>
                </div>
                <h3 className="text-lg font-semibold text-foreground mb-2">Unlock stats</h3>
                <p className="text-sm text-muted-foreground">
                  Get more visibility into how you're doing in each competition with ranks.
                </p>
              </CardContent>
            </Card>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto text-center space-y-8 p-8">
      {/* Existing Groups */}
      <div className="space-y-4">
        {displayGroups.map((group) => (
          <GroupCard key={group.id} group={group} />
        ))}
      </div>

      {/* Main Content */}
      <div className="space-y-4">
        <h2 className="text-2xl font-semibold text-foreground">Create a Group</h2>
        <p className="text-muted-foreground max-w-lg mx-auto">
          A group easily allows you to share Content, Inputs and Templates with the right people quicker.
        </p>
      </div>

      {/* Create Button */}
      <Button onClick={onCreateGroup} className="bg-primary hover:bg-primary/90 text-primary-foreground">
        <Plus className="w-4 h-4 mr-2" />
        Create a new Group
      </Button>
    </div>
  );
}
