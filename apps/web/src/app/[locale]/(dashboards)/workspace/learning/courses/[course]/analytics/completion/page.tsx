import React from 'react';
import { getCourseAnalytics, getCourseCompletionAnalytics } from '@/lib/learning';
import { Badge } from '@game-guild/ui/components/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Progress } from '@game-guild/ui/components/progress';
import { BarChart3 } from 'lucide-react';

/**
 * Completion Analytics Page
 *
 * Route: /courses/[course]/analytics/completion
 */
export default async function CompletionAnalyticsPage({
  params,
}: PageProps<'/[locale]/workspace/learning/courses/[course]/analytics/completion'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;
  const [completion, analytics] = await Promise.all([getCourseCompletionAnalytics(courseId), getCourseAnalytics(courseId)]);
  const totalEnrolled = completion.totalEnrolled || analytics.totalUsers;
  const totalCompleted = completion.totalCompleted || analytics.completedUsers;
  const completionRate = totalEnrolled > 0 ? Math.round((totalCompleted / totalEnrolled) * 100) : Math.round(analytics.completionRate || completion.completionRate);

  return (
    <div className="flex flex-col gap-6">
      <div className="grid gap-4 md:grid-cols-3">
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{totalEnrolled}</p><p className="text-sm text-muted-foreground">Enrolled</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{totalCompleted}</p><p className="text-sm text-muted-foreground">Completed</p></CardContent></Card>
        <Card><CardContent className="p-4"><p className="text-2xl font-semibold">{completionRate}%</p><p className="text-sm text-muted-foreground">Completion rate</p></CardContent></Card>
      </div>

      <Card>
        <CardHeader><CardTitle className="flex items-center gap-2"><BarChart3 className="size-5" />Completion Funnel</CardTitle></CardHeader>
        <CardContent className="space-y-4">
          <Progress value={completionRate} />
          {completion.funnel.length === 0 ? (
            <div className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">No detailed funnel events have been recorded yet.</div>
          ) : (
            completion.funnel.map((stage) => (
              <div key={stage.stage} className="flex items-center justify-between rounded-lg border p-4">
                <div><p className="font-medium">{stage.stage}</p><p className="text-sm text-muted-foreground">{stage.percentage}% of learners</p></div>
                <Badge>{stage.count}</Badge>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
