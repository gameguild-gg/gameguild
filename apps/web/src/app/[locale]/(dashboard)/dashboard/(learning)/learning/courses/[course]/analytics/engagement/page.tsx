import React from 'react';
import { getCourseEngagementAnalytics, getCourseAnalytics } from '@/lib/learning';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { Badge } from '@game-guild/ui/components/badge';
import { Progress } from '@game-guild/ui/components/progress';
import { BookOpen, Clock, Eye, Users, BarChart3, Activity } from 'lucide-react';

function StatCard({
  title,
  value,
  subtitle,
  icon: Icon,
  iconBg,
  iconColor,
}: {
  title: string;
  value: string | number;
  subtitle?: string;
  icon: React.ElementType;
  iconBg: string;
  iconColor: string;
}) {
  return (
    <Card>
      <CardContent className="p-6">
        <div className="flex items-start justify-between">
          <div className="space-y-2">
            <p className="text-sm font-medium text-muted-foreground">{title}</p>
            <p className="text-3xl font-bold">{value}</p>
            {subtitle && <p className="text-xs text-muted-foreground">{subtitle}</p>}
          </div>
          <div className={`rounded-lg p-3 ${iconBg}`}>
            <Icon className={`size-5 ${iconColor}`} />
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function MiniBarChart({ data, maxValue }: { data: { label: string; value: number }[]; maxValue: number }) {
  if (maxValue === 0) return <p className="py-8 text-center text-sm text-muted-foreground">No activity data yet</p>;
  return (
    <div className="flex h-20 items-end justify-between gap-1">
      {data.map((item, index) => (
        <div key={index} className="flex flex-1 flex-col items-center gap-1">
          <div
            className="w-full rounded-t bg-primary/80 transition-all hover:bg-primary"
            style={{ height: `${(item.value / maxValue) * 100}%`, minHeight: item.value > 0 ? '2px' : '0' }}
          />
          <span className="text-[10px] text-muted-foreground">{item.label}</span>
        </div>
      ))}
    </div>
  );
}

export default async function EngagementAnalyticsPage({
  params,
}: PageProps<'/[locale]/dashboard/learning/courses/[course]/analytics/engagement'>): Promise<React.JSX.Element> {
  const { course: courseId } = await params;

  const [engagement, analytics] = await Promise.all([
    getCourseEngagementAnalytics(courseId),
    getCourseAnalytics(courseId),
  ]);

  const totalEnrolled = analytics.totalUsers;
  const completedCount = analytics.completedUsers;
  const completionRate = Math.round(analytics.completionRate);

  const formatDuration = (seconds: number) => {
    if (seconds === 0) return '—';
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    return h > 0 ? `${h}h ${m}m` : `${m}m`;
  };

  // Prepare daily activity chart data (last 7 entries)
  const dailySlice = engagement.dailyActivity.slice(-7);
  const dailyLabels = dailySlice.map((d) => {
    const date = new Date(d.date);
    return date.toLocaleDateString('en-US', { weekday: 'short' });
  });
  const dailyChartData = dailySlice.map((d, i) => ({ label: dailyLabels[i] ?? '', value: d.activeUsers }));
  const maxDaily = Math.max(...dailySlice.map((d) => d.activeUsers), 1);

  // Peak hours chart
  const peakData = engagement.peakHours.map((p) => ({
    label: `${p.hour}`,
    value: p.activity,
  }));
  const maxPeak = Math.max(...engagement.peakHours.map((p) => p.activity), 1);

  const hasData = engagement.activeStudents > 0 || engagement.totalViews > 0 || totalEnrolled > 0;

  return (
    <div className="mx-auto flex max-w-7xl flex-col gap-6">
      {/* Header */}
      <div>
        <h2 className="text-2xl font-bold tracking-tight">Engagement Analytics</h2>
        <p className="text-muted-foreground">
          Insights into student activity and content engagement
        </p>
      </div>

      {/* Key Metrics */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total Enrollments"
          value={totalEnrolled.toLocaleString('en-US')}
          subtitle="Lifetime students"
          icon={Users}
          iconBg="bg-blue-100 dark:bg-blue-900/30"
          iconColor="text-blue-600 dark:text-blue-400"
        />
        <StatCard
          title="Active Students"
          value={engagement.activeStudents.toLocaleString('en-US')}
          subtitle={totalEnrolled > 0 ? `${Math.round((engagement.activeStudents / totalEnrolled) * 100)}% of enrolled` : undefined}
          icon={Activity}
          iconBg="bg-green-100 dark:bg-green-900/30"
          iconColor="text-green-600 dark:text-green-400"
        />
        <StatCard
          title="Completion Rate"
          value={`${completionRate}%`}
          subtitle={`${completedCount} students finished`}
          icon={BookOpen}
          iconBg="bg-purple-100 dark:bg-purple-900/30"
          iconColor="text-purple-600 dark:text-purple-400"
        />
        <StatCard
          title="Avg. Session Duration"
          value={formatDuration(engagement.avgSessionDuration)}
          subtitle="Per student session"
          icon={Clock}
          iconBg="bg-orange-100 dark:bg-orange-900/30"
          iconColor="text-orange-600 dark:text-orange-400"
        />
      </div>

      {!hasData ? (
        <Card>
          <CardContent className="flex flex-col items-center justify-center py-16 text-center">
            <BarChart3 className="mb-4 size-12 text-muted-foreground" />
            <h3 className="text-lg font-semibold">No analytics data yet</h3>
            <p className="max-w-md text-sm text-muted-foreground">
              Analytics will appear here once students start engaging with
              your course content. Enroll students to begin tracking.
            </p>
          </CardContent>
        </Card>
      ) : (
        <>
          {/* Charts Row */}
          <div className="grid gap-4 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Daily Active Users</CardTitle>
                <CardDescription>Active users over the last 7 days</CardDescription>
              </CardHeader>
              <CardContent>
                <MiniBarChart data={dailyChartData} maxValue={maxDaily} />
                <div className="mt-4 flex items-center justify-between text-sm">
                  <span className="text-muted-foreground">Total views</span>
                  <span className="font-semibold">{engagement.totalViews.toLocaleString('en-US')}</span>
                </div>
              </CardContent>
            </Card>

            {peakData.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle className="text-base">Peak Activity Hours</CardTitle>
                  <CardDescription>When students are most active (24h)</CardDescription>
                </CardHeader>
                <CardContent>
                  <MiniBarChart data={peakData} maxValue={maxPeak} />
                </CardContent>
              </Card>
            )}
          </div>

          {/* Content Performance */}
          {engagement.contentViews.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Content Performance</CardTitle>
                <CardDescription>Engagement metrics per content item</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {engagement.contentViews.map((item, index) => (
                    <div key={index} className="flex items-center justify-between rounded-lg border p-4">
                      <div className="flex items-center gap-3">
                        <div className="flex size-8 items-center justify-center rounded bg-muted text-sm font-medium">
                          {index + 1}
                        </div>
                        <div>
                          <div className="font-medium">{item.contentTitle}</div>
                          <div className="text-sm text-muted-foreground">
                            Avg. watch time: {formatDuration(item.avgWatchTime)}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center gap-6">
                        <div className="flex items-center gap-2">
                          <Eye className="size-4 text-muted-foreground" />
                          <span className="text-sm">{item.views.toLocaleString('en-US')}</span>
                        </div>
                        <div className="flex w-24 items-center gap-2">
                          <Progress value={item.completionRate} className="h-2" />
                          <span className="text-xs text-muted-foreground">{item.completionRate}%</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
