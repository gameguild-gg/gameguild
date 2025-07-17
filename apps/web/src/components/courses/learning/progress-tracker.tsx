'use client';

import React, { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Progress } from '@game-guild/ui/components/progress';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Award, BookOpen, CheckCircle, Clock, Star, Target, TrendingUp, Trophy } from 'lucide-react';

interface ProgressItem {
  id: string;
  title: string;
  type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  status: 'not-started' | 'in-progress' | 'completed' | 'graded';
  completedAt?: string;
  grade?: number;
  required: boolean;
  estimatedMinutes?: number;
}

interface ProgressData {
  courseId: string;
  courseTitle: string;
  totalItems: number;
  completedItems: number;
  progressPercentage: number;
  currentStreak: number;
  timeSpent: number;
  items: ProgressItem[];
  nextItem?: ProgressItem;
  estimatedTimeToComplete: number;
  certificateEligible: boolean;
}

interface ProgressTrackerProps {
  courseId: string;
  onItemClick?: (item: ProgressItem) => void;
}

export function ProgressTracker({ courseId, onItemClick }: ProgressTrackerProps) {
  const [progressData, setProgressData] = useState<ProgressData | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    fetchProgressData();
  }, [courseId]);

  const fetchProgressData = async () => {
    try {
      const { getCourseProgress } = await import('@/lib/courses/server-actions');
      const data = await getCourseProgress(courseId);
      setProgressData(data);
    } catch (error) {
      console.error('Error fetching progress data:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const getStatusIcon = (status: ProgressItem['status']) => {
    switch (status) {
      case 'completed':
      case 'graded':
        return <CheckCircle className="h-4 w-4 text-green-600" />;
      case 'in-progress':
        return <Clock className="h-4 w-4 text-blue-600" />;
      default:
        return <BookOpen className="h-4 w-4 text-gray-400" />;
    }
  };

  const getStatusColor = (status: ProgressItem['status']) => {
    switch (status) {
      case 'completed':
      case 'graded':
        return 'text-green-400';
      case 'in-progress':
        return 'text-blue-400';
      default:
        return 'text-gray-400';
    }
  };

  const formatTime = (minutes: number) => {
    const hours = Math.floor(minutes / 60);
    const mins = minutes % 60;
    return hours > 0 ? `${hours}h ${mins}m` : `${mins}m`;
  };

  if (isLoading) {
    return (
      <Card className="bg-gray-800 border-gray-700">
        <CardContent className="p-6 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-500 mx-auto"></div>
          <p className="text-gray-400 mt-2">Loading progress...</p>
        </CardContent>
      </Card>
    );
  }

  if (!progressData) {
    return (
      <Card className="bg-gray-800 border-gray-700">
        <CardContent className="p-6 text-center text-gray-500">Failed to load progress data</CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Overall Progress Card */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span className="text-white">Course Progress</span>
            <Badge variant={progressData.certificateEligible ? 'default' : 'secondary'}>{progressData.progressPercentage}% Complete</Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <Progress value={progressData.progressPercentage} className="h-3" />

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-blue-600">{progressData.completedItems}</div>
              <div className="text-sm text-gray-400">Completed</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-orange-600">{progressData.totalItems - progressData.completedItems}</div>
              <div className="text-sm text-gray-400">Remaining</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-green-600">{progressData.currentStreak}</div>
              <div className="text-sm text-gray-400">Day Streak</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-purple-600">{formatTime(progressData.timeSpent)}</div>
              <div className="text-sm text-gray-400">Time Spent</div>
            </div>
          </div>

          {progressData.certificateEligible && (
            <div className="bg-green-900/50 border border-green-700 rounded-lg p-4">
              <div className="flex items-center gap-2 text-green-400">
                <Trophy className="h-5 w-5" />
                <span className="font-medium">Certificate Eligible!</span>
              </div>
              <p className="text-green-300 text-sm mt-1">You've completed enough of the course to earn a certificate.</p>
            </div>
          )}

          {progressData.nextItem && (
            <div className="bg-blue-900/50 border border-blue-700 rounded-lg p-4">
              <div className="flex items-center justify-between">
                <div>
                  <div className="flex items-center gap-2 text-blue-400">
                    <Target className="h-5 w-5" />
                    <span className="font-medium">Up Next</span>
                  </div>
                  <p className="text-blue-300 text-sm mt-1">{progressData.nextItem.title}</p>
                </div>
                {onItemClick && (
                  <Button size="sm" onClick={() => onItemClick(progressData.nextItem!)} className="bg-blue-600 hover:bg-blue-700">
                    Continue
                  </Button>
                )}
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Progress Items List */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="text-white">Learning Progress</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {progressData.items.map((item, index) => (
              <div
                key={item.id}
                className={`flex items-center gap-4 p-3 rounded-lg border transition-colors ${onItemClick ? 'cursor-pointer hover:bg-gray-700' : ''} ${
                  item.status === 'completed' || item.status === 'graded'
                    ? 'bg-green-900/20 border-green-800'
                    : item.status === 'in-progress'
                      ? 'bg-blue-900/20 border-blue-800'
                      : 'bg-gray-900/50 border-gray-700'
                }`}
                onClick={() => onItemClick?.(item)}
              >
                <div className="flex-shrink-0">{getStatusIcon(item.status)}</div>

                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className={`font-medium ${getStatusColor(item.status)}`}>{item.title}</span>
                    {item.required && (
                      <Badge variant="secondary" className="text-xs">
                        Required
                      </Badge>
                    )}
                  </div>

                  <div className="flex items-center gap-4 mt-1 text-xs text-gray-400">
                    <span className="capitalize">{item.type}</span>
                    {item.estimatedMinutes && (
                      <span className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {item.estimatedMinutes}m
                      </span>
                    )}
                    {item.completedAt && <span>Completed {new Date(item.completedAt).toLocaleDateString()}</span>}
                    {item.grade !== undefined && (
                      <span className="flex items-center gap-1 text-green-400">
                        <Star className="h-3 w-3" />
                        {item.grade}%
                      </span>
                    )}
                  </div>
                </div>

                <div className="flex-shrink-0">
                  <span className="text-xs text-gray-500">#{index + 1}</span>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Achievement Summary */}
      <Card className="bg-gray-800 border-gray-700">
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-white">
            <Award className="h-5 w-5" />
            Achievements
          </CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="text-center p-4 bg-gray-900 rounded-lg">
              <TrendingUp className="h-8 w-8 text-blue-500 mx-auto mb-2" />
              <div className="text-lg font-bold text-white">{Math.round(progressData.progressPercentage)}%</div>
              <div className="text-sm text-gray-400">Course Progress</div>
            </div>

            <div className="text-center p-4 bg-gray-900 rounded-lg">
              <Target className="h-8 w-8 text-green-500 mx-auto mb-2" />
              <div className="text-lg font-bold text-white">{progressData.items.filter((item) => item.status === 'completed').length}</div>
              <div className="text-sm text-gray-400">Items Completed</div>
            </div>

            <div className="text-center p-4 bg-gray-900 rounded-lg">
              <Clock className="h-8 w-8 text-purple-500 mx-auto mb-2" />
              <div className="text-lg font-bold text-white">{formatTime(progressData.timeSpent)}</div>
              <div className="text-sm text-gray-400">Time Invested</div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

/* Compact progress component for course cards */
export function CompactProgress({
  progressPercentage,
  completedItems,
  totalItems,
}: {
  readonly progressPercentage: number;
  readonly completedItems: number;
  readonly totalItems: number;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between text-sm">
        <span className="text-gray-400">Progress</span>
        <span className="text-white font-medium">{progressPercentage}%</span>
      </div>
      <Progress value={progressPercentage} className="h-2" />
      <div className="text-xs text-gray-500 text-center">
        {completedItems} of {totalItems} completed
      </div>
    </div>
  );
}
