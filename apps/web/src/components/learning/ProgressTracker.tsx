'use client';

import { useEffect, useState } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Badge } from '@/components/ui/badge';
import { CheckCircle, Clock, Trophy, Target, Book, Activity } from 'lucide-react';

interface ProgressItem {
  readonly id: string;
  readonly title: string;
  readonly type: 'lesson' | 'activity' | 'quiz' | 'assignment' | 'peer-review';
  readonly status: 'not-started' | 'in-progress' | 'completed' | 'graded';
  readonly completedAt?: string;
  readonly grade?: number;
  readonly required: boolean;
}

interface ProgressData {
  readonly courseId: string;
  readonly courseTitle: string;
  readonly totalItems: number;
  readonly completedItems: number;
  readonly progressPercentage: number;
  readonly currentStreak: number;
  readonly timeSpent: number; // in minutes
  readonly items: ProgressItem[];
  readonly nextItem?: ProgressItem;
  readonly estimatedTimeToComplete: number; // in hours
  readonly certificateEligible: boolean;
}

interface ProgressTrackerProps {
  readonly courseId: string;
  readonly onItemClick?: (item: ProgressItem) => void;
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
        return <div className="h-4 w-4 rounded-full border-2 border-gray-300" />;
    }
  };

  const getTypeIcon = (type: ProgressItem['type']) => {
    switch (type) {
      case 'lesson':
        return <Book className="h-4 w-4" />;
      case 'activity':
        return <Activity className="h-4 w-4" />;
      case 'quiz':
        return <Target className="h-4 w-4" />;
      case 'assignment':
        return <Trophy className="h-4 w-4" />;
      case 'peer-review':
        return <CheckCircle className="h-4 w-4" />;
      default:
        return <Book className="h-4 w-4" />;
    }
  };

  const formatTime = (minutes: number): string => {
    if (minutes < 60) return `${minutes}m`;
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}m` : `${hours}h`;
  };

  if (isLoading) {
    return (
      <Card>
        <CardContent className="p-6">
          <div className="animate-pulse space-y-4">
            <div className="h-4 bg-gray-200 rounded w-3/4"></div>
            <div className="h-6 bg-gray-200 rounded"></div>
            <div className="space-y-2">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-12 bg-gray-200 rounded"></div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>
    );
  }

  if (!progressData) {
    return (
      <Card>
        <CardContent className="p-6 text-center text-gray-500">
          Failed to load progress data
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      {/* Overall Progress Card */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center justify-between">
            <span>Course Progress</span>
            <Badge variant={progressData.certificateEligible ? 'default' : 'secondary'}>
              {progressData.progressPercentage}% Complete
            </Badge>
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-6">
          <Progress value={progressData.progressPercentage} className="h-3" />
          
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-blue-600">
                {progressData.completedItems}
              </div>
              <div className="text-sm text-gray-600">Completed</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-orange-600">
                {progressData.totalItems - progressData.completedItems}
              </div>
              <div className="text-sm text-gray-600">Remaining</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-green-600">
                {progressData.currentStreak}
              </div>
              <div className="text-sm text-gray-600">Day Streak</div>
            </div>
            <div className="text-center space-y-1">
              <div className="text-2xl font-bold text-purple-600">
                {formatTime(progressData.timeSpent)}
              </div>
              <div className="text-sm text-gray-600">Time Spent</div>
            </div>
          </div>

          {progressData.nextItem && (
            <div className="p-4 bg-blue-50 rounded-lg border border-blue-200">
              <h4 className="font-medium text-blue-800 mb-2">Up Next:</h4>
              <div 
                className="flex items-center gap-3 cursor-pointer hover:bg-blue-100 p-2 rounded transition-colors"
                onClick={() => onItemClick?.(progressData.nextItem!)}
              >
                {getTypeIcon(progressData.nextItem.type)}
                <span className="font-medium">{progressData.nextItem.title}</span>
                <Badge variant="outline" className="ml-auto">
                  {progressData.nextItem.type}
                </Badge>
              </div>
            </div>
          )}

          {progressData.certificateEligible && (
            <div className="p-4 bg-green-50 rounded-lg border border-green-200">
              <div className="flex items-center gap-2 text-green-800">
                <Trophy className="h-5 w-5" />
                <span className="font-medium">Certificate Ready!</span>
              </div>
              <p className="text-sm text-green-600 mt-1">
                You've completed all requirements and can now generate your certificate.
              </p>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Detailed Progress Items */}
      <Card>
        <CardHeader>
          <CardTitle>Course Content</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {progressData.items.map((item) => (
              <div
                key={item.id}
                className={`flex items-center gap-4 p-3 rounded-lg border transition-colors cursor-pointer ${
                  item.status === 'completed' || item.status === 'graded'
                    ? 'bg-green-50 border-green-200 hover:bg-green-100'
                    : item.status === 'in-progress'
                    ? 'bg-blue-50 border-blue-200 hover:bg-blue-100'
                    : 'bg-gray-50 border-gray-200 hover:bg-gray-100'
                }`}
                onClick={() => onItemClick?.(item)}
              >
                <div className="flex-shrink-0">
                  {getStatusIcon(item.status)}
                </div>
                
                <div className="flex-grow min-w-0">
                  <div className="flex items-center gap-2 mb-1">
                    {getTypeIcon(item.type)}
                    <h4 className="font-medium truncate">{item.title}</h4>
                    {item.required && (
                      <Badge variant="outline" className="text-xs">
                        Required
                      </Badge>
                    )}
                  </div>
                  
                  <div className="flex items-center gap-4 text-sm text-gray-600">
                    <Badge variant="secondary" className="text-xs">
                      {item.type}
                    </Badge>
                    
                    {item.completedAt && (
                      <span>Completed {new Date(item.completedAt).toLocaleDateString()}</span>
                    )}
                    
                    {item.grade !== undefined && (
                      <span className="font-medium">Grade: {item.grade}%</span>
                    )}
                  </div>
                </div>

                <div className="flex-shrink-0">
                  <Badge 
                    variant={
                      item.status === 'completed' || item.status === 'graded' 
                        ? 'default' 
                        : item.status === 'in-progress'
                        ? 'secondary'
                        : 'outline'
                    }
                    className="capitalize"
                  >
                    {item.status.replace('-', ' ')}
                  </Badge>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Estimated Time to Complete */}
      {progressData.estimatedTimeToComplete > 0 && (
        <Card className="border-orange-200 bg-orange-50/50">
          <CardContent className="p-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Clock className="h-5 w-5 text-orange-600" />
                <span className="font-medium text-orange-800">
                  Estimated time to complete:
                </span>
              </div>
              <Badge className="bg-orange-600 text-white">
                ~{progressData.estimatedTimeToComplete}h remaining
              </Badge>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
}

/* Compact progress component for course cards */
export function CompactProgress({ 
  progressPercentage, 
  completedItems, 
  totalItems 
}: {
  readonly progressPercentage: number;
  readonly completedItems: number;
  readonly totalItems: number;
}) {
  return (
    <div className="space-y-2">
      <div className="flex items-center justify-between text-sm">
        <span className="text-gray-600">Progress</span>
        <span className="font-medium">{completedItems}/{totalItems}</span>
      </div>
      <Progress value={progressPercentage} className="h-2" />
      <div className="text-right text-xs text-gray-500">
        {progressPercentage}% complete
      </div>
    </div>
  );
}
