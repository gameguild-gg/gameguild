'use client';

import { useCoursesSync } from '@/lib/courses/context/course-management.context';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Loader2, RefreshCw, BookOpen, Users, BarChart3, TrendingUp } from 'lucide-react';

export function CoursesOverviewContent() {
  const { state, getEnhancedCourses, syncWithServer } = useCoursesSync();

  const courses = getEnhancedCourses();

  // Calculate stats
  const totalCourses = courses.length;
  const coursesByArea = courses.reduce(
    (acc, course) => {
      acc[course.area] = (acc[course.area] || 0) + 1;
      return acc;
    },
    {} as Record<string, number>,
  );

  const coursesByLevel = courses.reduce(
    (acc, course) => {
      const levelName = course.level === 1 ? 'Beginner' : course.level === 2 ? 'Intermediate' : course.level === 3 ? 'Advanced' : 'Arcane';
      acc[levelName] = (acc[levelName] || 0) + 1;
      return acc;
    },
    {} as Record<string, number>,
  );

  const handleRefresh = async () => {
    await syncWithServer();
  };

  if (state.isLoading && courses.length === 0) {
    return (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Sync Status */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <h2 className="text-xl font-semibold">Course Statistics</h2>
          {state.syncStatus === 'syncing' && <Loader2 className="h-4 w-4 animate-spin text-blue-500" />}
          {state.pendingChanges.size > 0 && (
            <Badge variant="outline" className="text-orange-600">
              {state.pendingChanges.size} pending changes
            </Badge>
          )}
        </div>

        <Button variant="outline" size="sm" onClick={handleRefresh}>
          <RefreshCw className="h-4 w-4 mr-2" />
          Refresh
        </Button>
      </div>

      {/* Overview Stats */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Total Courses</CardTitle>
            <BookOpen className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totalCourses}</div>
            <p className="text-xs text-muted-foreground">Across all areas and levels</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Most Popular Area</CardTitle>
            <TrendingUp className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold capitalize">{Object.entries(coursesByArea).sort(([, a], [, b]) => b - a)[0]?.[0] || 'N/A'}</div>
            <p className="text-xs text-muted-foreground">{Object.entries(coursesByArea).sort(([, a], [, b]) => b - a)[0]?.[1] || 0} courses</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Sync Status</CardTitle>
            <BarChart3 className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold capitalize">{state.syncStatus}</div>
            <p className="text-xs text-muted-foreground">{state.lastSyncTime ? `Last: ${state.lastSyncTime.toLocaleTimeString()}` : 'Never synced'}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Pending Changes</CardTitle>
            <Users className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{state.pendingChanges.size}</div>
            <p className="text-xs text-muted-foreground">Waiting to sync</p>
          </CardContent>
        </Card>
      </div>

      {/* Courses by Area */}
      <Card>
        <CardHeader>
          <CardTitle>Courses by Area</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {Object.entries(coursesByArea).map(([area, count]) => (
              <div key={area} className="flex items-center justify-between">
                <span className="capitalize font-medium">{area}</span>
                <Badge variant="secondary">{count}</Badge>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Courses by Level */}
      <Card>
        <CardHeader>
          <CardTitle>Courses by Level</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {Object.entries(coursesByLevel).map(([level, count]) => (
              <div key={level} className="flex items-center justify-between">
                <span className="font-medium">{level}</span>
                <Badge variant="outline">{count}</Badge>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Recent Courses */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Courses</CardTitle>
        </CardHeader>
        <CardContent>
          {courses.length === 0 ? (
            <p className="text-center text-muted-foreground py-4">No courses available</p>
          ) : (
            <div className="space-y-3">
              {courses.slice(0, 5).map((course) => (
                <div key={course.id} className="flex items-start gap-3 p-3 border rounded-lg">
                  {course.image && <img src={course.image} alt={course.title} className="h-12 w-12 rounded object-cover" />}
                  <div className="flex-1 min-w-0">
                    <h4 className="font-medium truncate">{course.title}</h4>
                    <p className="text-sm text-muted-foreground truncate">{course.description}</p>
                    <div className="flex gap-2 mt-2">
                      <Badge variant="outline" className="text-xs">
                        {course.area}
                      </Badge>
                      <Badge variant="secondary" className="text-xs">
                        Level {course.level}
                      </Badge>
                    </div>
                  </div>
                  {state.pendingChanges.has(course.id) && (
                    <Badge variant="outline" className="text-orange-600">
                      Pending
                    </Badge>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
