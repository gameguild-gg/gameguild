import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues } from '@/components/testing-lab/testing-lab-state';
import { getTestingLabAnalytics } from '@/lib/testing-lab';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { BarChart3, CheckCircle2, MessageSquareText, Users } from 'lucide-react';

function DistributionBar({ value, total, label }: { value: number; total: number; label: string }) {
  const percent = total > 0 ? Math.round((value / total) * 100) : 0;
  return (
    <div className="space-y-1.5">
      <div className="flex justify-between text-sm">
        <span>{label}</span>
        <span className="text-muted-foreground">
          {value} · {percent}%
        </span>
      </div>
      <div className="h-2 overflow-hidden rounded-full bg-muted">
        <div className="h-full rounded-full bg-primary" style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}

export default async function TestingLabAnalyticsPage() {
  const analytics = await getTestingLabAnalytics();
  return (
    <div className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={BarChart3}
        title="Testing Lab analytics"
        description="Live operational analytics computed from requests, sessions, locations, registrations, and participant feedback."
      />
      <TestingLabAccessIssues issues={analytics.accessIssues} />
      <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Request completion</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">
              {analytics.requests.completed}/{analytics.requests.total}
            </p>
            <CardDescription>Completed testing cycles</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Session fill rate</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{analytics.capacity.fillRate}%</p>
            <CardDescription>{analytics.capacity.registered} registered seats</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Average rating</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{analytics.feedback.averageRating ?? '-'}/5</p>
            <CardDescription>{analytics.feedback.total} feedback records</CardDescription>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="pb-2">
            <CardTitle className="text-sm">Recommendation</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-semibold">{analytics.feedback.recommendationRate ?? '-'}%</p>
            <CardDescription>Participants who would recommend</CardDescription>
          </CardContent>
        </Card>
      </section>
      <section className="grid gap-6 xl:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <CheckCircle2 className="size-5" />
              Request lifecycle
            </CardTitle>
            <CardDescription>Distribution across the testing pipeline.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <DistributionBar label="Open" value={analytics.requests.open} total={analytics.requests.total} />
            <DistributionBar label="Active" value={analytics.requests.active} total={analytics.requests.total} />
            <DistributionBar label="Completed" value={analytics.requests.completed} total={analytics.requests.total} />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Users className="size-5" />
              Session operations
            </CardTitle>
            <CardDescription>Schedule progress and available capacity.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <DistributionBar label="Scheduled" value={analytics.sessions.scheduled} total={analytics.sessions.total} />
            <DistributionBar label="Active" value={analytics.sessions.active} total={analytics.sessions.total} />
            <DistributionBar label="Completed" value={analytics.sessions.completed} total={analytics.sessions.total} />
            <div className="rounded-md bg-muted/35 p-3 text-sm">{analytics.capacity.available} seats remain across scheduled capacity.</div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <MessageSquareText className="size-5" />
              Feedback health
            </CardTitle>
            <CardDescription>Quality and recommendation signals from real submissions.</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-3xl font-semibold">{analytics.feedback.total}</p>
              <p className="text-sm text-muted-foreground">Total feedback records</p>
            </div>
            <div>
              <p className="text-3xl font-semibold">
                {analytics.locations.active}/{analytics.locations.total}
              </p>
              <p className="text-sm text-muted-foreground">Active testing locations</p>
            </div>
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
