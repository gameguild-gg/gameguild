'use client';

import { useMemo } from 'react';
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

interface TestingContentSummary {
  id: string;
  title: string;
  type: 'test' | 'project';
  description: string;
  duration: string;
  difficulty: 'beginner' | 'intermediate' | 'advanced';
  participants: number;
  status: 'available' | 'in-progress' | 'completed';
}

export function LocationBasedContent({ selectedLocation, maxTests, maxProjects }: LocationBasedContentProps) {
  const displayedContent = useMemo<TestingContentSummary[]>(() => {
    if (!selectedLocation) {
      return [];
    }

    const locationName = selectedLocation.name;
    const tests: TestingContentSummary[] = [
      {
        id: `${selectedLocation.id}-usability`,
        title: `${locationName} usability session`,
        type: 'test' as const,
        description: 'Moderated UX, onboarding, and accessibility feedback for active submissions.',
        duration: '45 min',
        difficulty: 'beginner' as const,
        participants: Math.min(selectedLocation.maxTestersCapacity, 8),
        status: 'available' as const,
      },
      {
        id: `${selectedLocation.id}-gameplay`,
        title: `${locationName} gameplay balance session`,
        type: 'test' as const,
        description: 'Structured mechanics, difficulty, and retention testing with enrolled participants.',
        duration: '60 min',
        difficulty: 'intermediate' as const,
        participants: Math.min(selectedLocation.maxTestersCapacity, 12),
        status: 'available' as const,
      },
      {
        id: `${selectedLocation.id}-technical`,
        title: `${locationName} technical validation`,
        type: 'test' as const,
        description: 'Performance, compatibility, and build-readiness verification for launch candidates.',
        duration: '30 min',
        difficulty: 'advanced' as const,
        participants: Math.min(selectedLocation.maxTestersCapacity, 6),
        status: 'available' as const,
      },
    ].slice(0, maxTests);

    const projects: TestingContentSummary[] = [
      {
        id: `${selectedLocation.id}-prototype-review`,
        title: 'Prototype review cohort',
        type: 'project' as const,
        description: 'Team-based review cycle for playable prototypes and milestone demos.',
        duration: '4 weeks',
        difficulty: 'intermediate' as const,
        participants: Math.min(selectedLocation.maxProjectsCapacity, 8),
        status: 'available' as const,
      },
      {
        id: `${selectedLocation.id}-launch-readiness`,
        title: 'Launch readiness project',
        type: 'project' as const,
        description: 'Release candidate testing, feedback triage, and launch checklist completion.',
        duration: '3 weeks',
        difficulty: 'advanced' as const,
        participants: Math.min(selectedLocation.maxProjectsCapacity, 6),
        status: 'available' as const,
      },
    ].slice(0, maxProjects);

    return [...tests, ...projects];
  }, [selectedLocation, maxProjects, maxTests]);

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
