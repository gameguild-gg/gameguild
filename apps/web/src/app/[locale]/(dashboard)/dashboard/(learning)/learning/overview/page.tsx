import React from 'react';
import { getInstructorStats, getRecentActivity } from '@/lib/learning';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { BookOpen, Users, TrendingUp, Star, Activity } from 'lucide-react';

export default async function Page({ params }: PageProps<'/[locale]/dashboard/learning/overview'>): Promise<React.JSX.Element> {
  const { locale } = await params;
  void locale;

  const [stats, activity] = await Promise.all([getInstructorStats(), getRecentActivity()]);

  const totalCourses = stats.courses.length;
  const allEnrollments = stats.courses.flatMap((c) => c.enrollments);
  const totalStudents = new Set(allEnrollments.map((e) => e.id)).size;
  const avgCompletionRate =
    totalCourses > 0
      ? stats.courses.reduce((acc, c) => {
          const rate = c.enrollments.length > 0 ? c.completions.length / c.enrollments.length : 0;
          return acc + rate;
        }, 0) / totalCourses
      : 0;
  const allRatings = stats.courses.flatMap((c) => c.ratings);
  const avgRating = allRatings.length > 0 ? allRatings.reduce((acc, r) => acc + r.score, 0) / allRatings.length : 0;

  const kpis = [
    { label: 'Total Courses', value: totalCourses, icon: BookOpen, description: 'Courses you manage' },
    { label: 'Total Students', value: totalStudents, icon: Users, description: 'Unique enrolled students' },
    { label: 'Avg. Completion', value: `${(avgCompletionRate * 100).toFixed(0)}%`, icon: TrendingUp, description: 'Average completion rate' },
    { label: 'Avg. Rating', value: avgRating > 0 ? avgRating.toFixed(1) : '—', icon: Star, description: 'Average course rating' },
  ];

  return (
    <div className="flex flex-col gap-6 p-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Learning Overview</h1>
        <p className="text-muted-foreground">Your instructor dashboard — courses, students, and performance.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {kpis.map((kpi) => (
          <Card key={kpi.label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2">
              <CardTitle className="text-sm font-medium">{kpi.label}</CardTitle>
              <kpi.icon className="size-4 text-muted-foreground" />
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{kpi.value}</div>
              <CardDescription>{kpi.description}</CardDescription>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Activity className="size-5" />
            Recent Activity
          </CardTitle>
          <CardDescription>Latest enrollments, completions, and reviews</CardDescription>
        </CardHeader>
        <CardContent>
          {activity.activities.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              No recent activity. Activity will appear here as students interact with your courses.
            </p>
          ) : (
            <div className="space-y-3">
              {activity.activities.map((item, i) => (
                <div key={i} className="flex items-center justify-between rounded-md border p-3">
                  <div>
                    <span className="font-medium">{item.studentName}</span>
                    <span className="text-muted-foreground"> — {item.type} on </span>
                    <span className="font-medium">{item.courseName}</span>
                  </div>
                  <span className="text-xs text-muted-foreground">{new Date(item.timestamp).toLocaleDateString()}</span>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
