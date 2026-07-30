import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { Link } from '@/i18n/navigation';
import { getTestingLabAnalytics, getTestingLabDashboard, normalizeTestingRequestStatus, normalizeTestingSessionStatus } from '@/lib/testing-lab';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@game-guild/ui/components/card';
import { ArrowRight, BarChart3, CalendarDays, FlaskConical, FolderKanban, Settings, Users } from 'lucide-react';

const workstreams = [
  {
    title: 'Events',
    description: 'Application windows, approval, independent slots, attendance, and required feedback.',
    href: '/dashboard/testing-lab/events',
    icon: CalendarDays,
  },
  {
    title: 'Projects',
    description: 'Project builds, testing briefs, event applications, and lifecycle.',
    href: '/dashboard/testing-lab/projects',
    icon: FolderKanban,
  },
  {
    title: 'Participants',
    description: 'Testers, registrations, waitlists, and attendance follow-up.',
    href: '/dashboard/testing-lab/participants',
    icon: Users,
  },
  {
    title: 'Analytics',
    description: 'Live demand, capacity, completion, ratings, and recommendation analytics.',
    href: '/dashboard/testing-lab/analytics',
    icon: BarChart3,
  },
  {
    title: 'Settings',
    description: 'General defaults, locations, access roles, and permissions.',
    href: '/dashboard/testing-lab/settings',
    icon: Settings,
  },
];

export default async function TestingLabPage() {
  const [directory, analytics] = await Promise.all([getTestingLabDashboard(), getTestingLabAnalytics()]);
  const issues = [...directory.accessIssues, ...analytics.accessIssues];

  return (
    <div className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={FlaskConical}
        title="Testing Lab"
        description="Operate real project testing from build intake through moderated sessions, participant attendance, feedback, and evidence-backed reports."
        actions={
          <>
            <Button asChild variant="outline">
              <Link href="/testing-lab">
                Public lab
                <ArrowRight className="ml-2 size-4" />
              </Link>
            </Button>
            <Button asChild>
              <Link href="/dashboard/testing-lab/events">
                Manage events
                <ArrowRight className="ml-2 size-4" />
              </Link>
            </Button>
          </>
        }
      />

      <TestingLabAccessIssues issues={[...new Set(issues)]} />

      <section aria-label="Testing Lab metrics" className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
        {[
          ['Open requests', analytics.requests.open + analytics.requests.active, `${analytics.requests.total} total requests`],
          ['Upcoming sessions', analytics.sessions.scheduled + analytics.sessions.active, `${analytics.sessions.completed} completed`],
          ['Capacity fill', `${analytics.capacity.fillRate}%`, `${analytics.capacity.registered}/${analytics.capacity.total} seats`],
          [
            'Feedback',
            analytics.feedback.total,
            analytics.feedback.averageRating === null ? 'No ratings yet' : `${analytics.feedback.averageRating}/5 average`,
          ],
        ].map(([label, value, detail]) => (
          <Card key={label}>
            <CardHeader className="pb-2">
              <CardTitle className="text-sm font-medium">{label}</CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-2xl font-semibold">{value}</p>
              <CardDescription>{detail}</CardDescription>
            </CardContent>
          </Card>
        ))}
      </section>

      <section>
        <div className="mb-3">
          <h2 className="text-lg font-semibold">Operations</h2>
          <p className="text-sm text-muted-foreground">Each workflow has its own focused workspace and API-backed actions.</p>
        </div>
        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {workstreams.map((item) => {
            const Icon = item.icon;
            return (
              <Link key={item.href} href={item.href} className="group rounded-md border p-4 transition-colors hover:bg-muted/35">
                <div className="flex items-start justify-between gap-4">
                  <div className="flex gap-3">
                    <Icon className="mt-0.5 size-5 text-muted-foreground" />
                    <div>
                      <h3 className="font-medium">{item.title}</h3>
                      <p className="mt-1 text-sm text-muted-foreground">{item.description}</p>
                    </div>
                  </div>
                  <ArrowRight className="size-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
                </div>
              </Link>
            );
          })}
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-2">
        <div>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Recent requests</h2>
            <Button asChild variant="ghost" size="sm">
              <Link href="/dashboard/testing-lab/projects">View all</Link>
            </Button>
          </div>
          {directory.requests.length === 0 ? (
            <TestingLabEmptyState title="No testing requests" description="Submit a project build to begin a structured testing cycle." />
          ) : (
            <div className="divide-y rounded-md border">
              {directory.requests.slice(0, 5).map((request) => (
                <Link
                  key={request.id}
                  href={`/dashboard/testing-lab/projects/${request.id}`}
                  className="flex items-center justify-between gap-4 p-3 hover:bg-muted/30"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{request.title}</p>
                    <p className="truncate text-xs text-muted-foreground">{request.description ?? 'No objective provided'}</p>
                  </div>
                  <Badge variant="outline">{normalizeTestingRequestStatus(request.status)}</Badge>
                </Link>
              ))}
            </div>
          )}
        </div>
        <div>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Upcoming sessions</h2>
            <Button asChild variant="ghost" size="sm">
              <Link href="/dashboard/testing-lab/sessions">View all</Link>
            </Button>
          </div>
          {directory.sessions.length === 0 ? (
            <TestingLabEmptyState title="No testing sessions" description="Schedule a moderated window after a testing request is ready." />
          ) : (
            <div className="divide-y rounded-md border">
              {directory.sessions.slice(0, 5).map((session) => (
                <Link
                  key={session.id}
                  href={`/dashboard/testing-lab/sessions/${session.id}`}
                  className="flex items-center justify-between gap-4 p-3 hover:bg-muted/30"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{session.sessionName}</p>
                    <p className="truncate text-xs text-muted-foreground">{session.location?.name ?? 'Location not assigned'}</p>
                  </div>
                  <Badge variant="outline">{normalizeTestingSessionStatus(session.status)}</Badge>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>
    </div>
  );
}
