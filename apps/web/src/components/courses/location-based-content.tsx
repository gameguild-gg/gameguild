'use client';

import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Users, FolderOpen, AlertCircle, Info, Eye, Play } from 'lucide-react';
import type { TestingLocation } from '@/lib/api/testing-types';

interface LocationBasedContentProps {
  selectedLocation: TestingLocation | null;
  maxTests: number;
  maxProjects: number;
}

interface MockTestingContent {
  id: string;
  title: string;
  type: 'test' | 'project';
  description: string;
  duration: string;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  participants: number;
  status: 'available' | 'in-progress' | 'completed';
}

// Mock data - in real implementation this would come from the API
const mockTestingContent: MockTestingContent[] = [
  {
    id: '1',
    title: 'UI/UX Testing Session',
    type: 'test',
    description: 'Test the user interface and user experience of the game',
    duration: '45 min',
    difficulty: 'beginner',
    participants: 8,
    status: 'available',
  },
  {
    id: '2',
    title: 'Gameplay Mechanics Testing',
    type: 'test',
    description: 'Test core gameplay mechanics and balance',
    duration: '60 min',
    difficulty: 'intermediate',
    participants: 12,
    status: 'available',
  },
  {
    id: '3',
    title: 'Performance Testing',
    type: 'test',
    description: 'Test game performance across different devices',
    duration: '30 min',
    difficulty: 'advanced',
    participants: 6,
    status: 'available',
  },
  {
    id: '4',
    title: 'Multiplayer Testing',
    type: 'test',
    description: 'Test multiplayer functionality and networking',
    duration: '90 min',
    difficulty: 'advanced',
    participants: 15,
    status: 'in-progress',
  },
  {
    id: '5',
    title: 'Accessibility Testing',
    type: 'test',
    description: 'Test game accessibility features and compliance',
    duration: '40 min',
    difficulty: 'intermediate',
    participants: 5,
    status: 'available',
  },
  {
    id: '6',
    title: '2D Platformer Game',
    type: 'project',
    description: 'Develop and test a 2D platformer game prototype',
    duration: '4 weeks',
    difficulty: 'intermediate',
    participants: 8,
    status: 'available',
  },
  {
    id: '7',
    title: 'VR Experience Project',
    type: 'project',
    description: 'Create an immersive VR experience for testing',
    duration: '6 weeks',
    difficulty: 'advanced',
    participants: 4,
    status: 'available',
  },
  {
    id: '8',
    title: 'Mobile Game Prototype',
    type: 'project',
    description: 'Develop a mobile game prototype for user testing',
    duration: '3 weeks',
    difficulty: 'beginner',
    participants: 10,
    status: 'in-progress',
  },
  {
    id: '9',
    title: 'AI Testing Framework',
    type: 'project',
    description: 'Build a framework for testing AI behavior in games',
    duration: '8 weeks',
    difficulty: 'advanced',
    participants: 6,
    status: 'available',
  },
  {
    id: '10',
    title: 'Game Analytics Dashboard',
    type: 'project',
    description: 'Create a dashboard for analyzing game testing data',
    duration: '5 weeks',
    difficulty: 'intermediate',
    participants: 7,
    status: 'available',
  },
];

export function LocationBasedContent({ selectedLocation, maxTests, maxProjects }: LocationBasedContentProps) {
  const [displayedContent, setDisplayedContent] = useState<MockTestingContent[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!selectedLocation) {
      setDisplayedContent([]);
      return;
    }

    setIsLoading(true);

    // Simulate API call delay
    const loadContent = setTimeout(() => {
      // Filter and limit content based on location capacity
      const availableTests = mockTestingContent.filter((content) => content.type === 'test').slice(0, maxTests);
      
      const availableProjects = mockTestingContent.filter((content) => content.type === 'project').slice(0, maxProjects);

      setDisplayedContent([...availableTests, ...availableProjects]);
      setIsLoading(false);
    }, 800);

    return () => clearTimeout(loadContent);
  }, [selectedLocation, maxTests, maxProjects]);

  const getDifficultyColor = (difficulty: string) => {
    switch (difficulty) {
      case 'beginner':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'intermediate':
        return 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30';
      case 'advanced':
        return 'bg-red-500/20 text-red-400 border-red-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'available':
        return 'bg-green-500/20 text-green-400 border-green-500/30';
      case 'in-progress':
        return 'bg-blue-500/20 text-blue-400 border-blue-500/30';
      case 'completed':
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
      default:
        return 'bg-gray-500/20 text-gray-400 border-gray-500/30';
    }
  };

  const getTypeIcon = (type: string) => {
    return type === 'test' ? <Users className="h-4 w-4" /> : <FolderOpen className="h-4 w-4" />;
  };

  const getTypeColor = (type: string) => {
    return type === 'test' ? 'text-blue-400' : 'text-purple-400';
  };

  if (!selectedLocation) {
    return (
      <Card className="bg-gray-900 border-gray-800">
        <CardContent className="p-8 text-center">
          <Info className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-300 mb-2">Select a Testing Location</h3>
          <p className="text-sm text-gray-400">Choose a testing location above to view available tests and projects</p>
        </CardContent>
      </Card>
    );
  }

  if (isLoading) {
    return (
      <Card className="bg-gray-900 border-gray-800">
        <CardHeader>
          <CardTitle>Loading Content...</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {Array.from({ length: 6 }).map((_, index) => (
              <div key={index} className="animate-pulse">
                <div className="h-20 bg-gray-700 rounded-lg"></div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    );
  }

  const tests = displayedContent.filter((content) => content.type === 'test');
  const projects = displayedContent.filter((content) => content.type === 'project');

  return (
    <div className="space-y-6">
      {/* Location Summary */}
      <Card className="bg-gray-900 border-gray-800">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Info className="h-5 w-5" />
            Content for {selectedLocation.name}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-2 gap-4">
            <div className="bg-blue-500/10 border border-blue-500/20 rounded-lg p-3">
              <div className="flex items-center gap-2 text-blue-400">
                <Users className="h-4 w-4" />
                <span className="font-medium">
                  {tests.length} / {maxTests}
                </span>
              </div>
              <p className="text-xs text-blue-300 mt-1">Testing Sessions</p>
            </div>
            
            <div className="bg-purple-500/10 border border-purple-500/20 rounded-lg p-3">
              <div className="flex items-center gap-2 text-purple-400">
                <FolderOpen className="h-4 w-4" />
                <span className="font-medium">
                  {projects.length} / {maxProjects}
                </span>
              </div>
              <p className="text-xs text-purple-300 mt-1">Testing Projects</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Tests Section */}
      {tests.length > 0 && (
        <Card className="bg-gray-900 border-gray-800">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="h-5 w-5 text-blue-400" />
              Testing Sessions ({tests.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {tests.map((test) => (
                <div key={test.id} className="flex items-center gap-4 p-4 bg-gray-800/50 rounded-lg border border-gray-700 hover:border-gray-600 transition-colors">
                  <div className={`p-2 rounded-lg ${getTypeColor(test.type)}`}>{getTypeIcon(test.type)}</div>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="font-medium text-white truncate">{test.title}</h4>
                      <Badge className={getDifficultyColor(test.difficulty)}>{test.difficulty}</Badge>
                      <Badge className={getStatusColor(test.status)}>{test.status}</Badge>
                    </div>
                    <p className="text-sm text-gray-400 truncate">{test.description}</p>
                    <div className="flex items-center gap-4 mt-2 text-xs text-gray-500">
                      <span>{test.duration}</span>
                      <span>{test.participants} participants</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm">
                      <Eye className="h-3 w-3 mr-1" />
                      View
                    </Button>
                    {test.status === 'available' && (
                      <Button size="sm">
                        <Play className="h-3 w-3 mr-1" />
                        Join
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Projects Section */}
      {projects.length > 0 && (
        <Card className="bg-gray-900 border-gray-800">
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <FolderOpen className="h-5 w-5 text-purple-400" />
              Testing Projects ({projects.length})
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              {projects.map((project) => (
                <div key={project.id} className="flex items-center gap-4 p-4 bg-gray-800/50 rounded-lg border border-gray-700 hover:border-gray-600 transition-colors">
                  <div className={`p-2 rounded-lg ${getTypeColor(project.type)}`}>{getTypeIcon(project.type)}</div>
                  
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1">
                      <h4 className="font-medium text-white truncate">{project.title}</h4>
                      <Badge className={getDifficultyColor(project.difficulty)}>{project.difficulty}</Badge>
                      <Badge className={getStatusColor(project.status)}>{project.status}</Badge>
                    </div>
                    <p className="text-sm text-gray-400 truncate">{project.description}</p>
                    <div className="flex items-center gap-4 mt-2 text-xs text-gray-500">
                      <span>{project.duration}</span>
                      <span>{project.participants} participants</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2">
                    <Button variant="outline" size="sm">
                      <Eye className="h-3 w-3 mr-1" />
                      View
                    </Button>
                    {project.status === 'available' && (
                      <Button size="sm">
                        <Play className="h-3 w-3 mr-1" />
                        Join
                      </Button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}

      {/* Capacity Warning */}
      {tests.length === maxTests || projects.length === maxProjects ? (
        <Card className="bg-yellow-900/20 border-yellow-500/30">
          <CardContent className="p-4">
            <div className="flex items-center gap-2 text-yellow-400">
              <AlertCircle className="h-4 w-4" />
              <span className="text-sm font-medium">Location Capacity Limit Reached</span>
            </div>
            <p className="text-xs text-yellow-300 mt-1">
              This location has reached its maximum capacity for {tests.length === maxTests ? 'testing sessions' : ''}
              {tests.length === maxTests && projects.length === maxProjects ? ' and ' : ''}
              {projects.length === maxProjects ? 'projects' : ''}. Select a different location to see more content.
            </p>
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
