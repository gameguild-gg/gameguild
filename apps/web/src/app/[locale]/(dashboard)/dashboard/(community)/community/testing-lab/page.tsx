import { TestingLabCalendar } from '@/components/testing-lab/testing-lab-calendar';
import { TestingLabOperationsNavigation, TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { Link } from '@/i18n/navigation';
import {
  getTestingLabAnalytics,
  getTestingLabDashboard,
  normalizeTestingRequestStatus,
  normalizeTestingSessionStatus,
} from '@/lib/testing-lab';
import { getTestingEventsDirectory } from '@/lib/testing-lab/events-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { Button } from '@game-guild/ui/components/button';
import { ArrowRight, FlaskConical } from 'lucide-react';

export default async function TestingLabPage() {
  const [directory, analytics, events] = await Promise.all([
    getTestingLabDashboard(),
    getTestingLabAnalytics(),
    getTestingEventsDirectory({ take: 100 }),
  ]);
  const issues = [...directory.accessIssues, ...analytics.accessIssues, ...events.accessIssues];
  const capacityMetric = analytics.current.capacity > 0
    ? {
        value: `${analytics.current.fillRate}%`,
        detail: `${analytics.current.registeredTesters}/${analytics.current.capacity} seats`,
      }
    : analytics.current.registeredTesters > 0
      ? {
          value: 'Unlimited',
          detail: `${analytics.current.registeredTesters} registered`,
        }
      : { value: '-', detail: 'No capacity configured' };

  return (
    <div className="space-y-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={FlaskConical}
        title="Testing Lab"
        description="Operate real project testing from build intake through moderated sessions, participant attendance, feedback, and evidence-backed reports."
        navigation={<TestingLabOperationsNavigation />}
        actions={
          <>
            <Button asChild variant="outline">
              <Link href="/testing-lab">
                Public lab
                <ArrowRight className="ml-2 size-4" />
              </Link>
            </Button>
            <Button asChild>
              <Link href="/dashboard/community/testing-lab/events">
                Manage events
                <ArrowRight className="ml-2 size-4" />
              </Link>
            </Button>
          </>
        }
      />

      <TestingLabAccessIssues issues={[...new Set(issues)]} />

      <section
        aria-label="Testing Lab metrics"
        className="grid overflow-hidden rounded-md border sm:grid-cols-2 xl:grid-cols-4"
      >
        {[
          ['Applications', analytics.current.applications, `${analytics.current.approvedProjects} approved projects`],
          ['Events', analytics.current.events, `${analytics.current.completedEvents} completed`],
          [
            'Capacity fill',
            capacityMetric.value,
            capacityMetric.detail,
          ],
          [
            'Feedback',
            analytics.current.feedback,
            analytics.current.averageRating === null
              ? 'No ratings yet'
              : `${analytics.current.averageRating}/10 average`,
          ],
        ].map(([label, value, detail]) => (
          <div
            key={label}
            className="border-b p-4 last:border-b-0 sm:odd:border-r sm:[&:nth-last-child(-n+2)]:border-b-0 xl:border-b-0 xl:border-r xl:last:border-r-0"
          >
            <p className="text-sm font-medium text-muted-foreground">{label}</p>
            <p className="mt-1 text-2xl font-semibold">{value}</p>
            <p className="mt-1 text-sm text-muted-foreground">{detail}</p>
          </div>
        ))}
      </section>

      <TestingLabCalendar events={events.events} eventAnalytics={analytics.events} />

      <section className="grid gap-6 xl:grid-cols-2">
        <div>
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold">Recent requests</h2>
            <Button asChild variant="ghost" size="sm">
              <Link href="/dashboard/community/testing-lab/projects">View all</Link>
            </Button>
          </div>
          {directory.requests.length === 0 ? (
            <TestingLabEmptyState
              title="No testing requests"
              description="Submit a project build to begin a structured testing cycle."
            />
          ) : (
            <div className="divide-y rounded-md border">
              {directory.requests.slice(0, 5).map((request) => (
                <Link
                  key={request.id}
                  href={`/dashboard/community/testing-lab/projects/${request.id}`}
                  className="flex items-center justify-between gap-4 p-3 hover:bg-muted/30"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{request.title}</p>
                    <p className="truncate text-xs text-muted-foreground">
                      {request.description ?? 'No objective provided'}
                    </p>
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
              <Link href="/dashboard/community/testing-lab/sessions">View all</Link>
            </Button>
          </div>
          {directory.sessions.length === 0 ? (
            <TestingLabEmptyState
              title="No testing sessions"
              description="Schedule a moderated window after a testing request is ready."
            />
          ) : (
            <div className="divide-y rounded-md border">
              {directory.sessions.slice(0, 5).map((session) => (
                <Link
                  key={session.id}
                  href={`/dashboard/community/testing-lab/sessions/${session.id}`}
                  className="flex items-center justify-between gap-4 p-3 hover:bg-muted/30"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium">{session.sessionName}</p>
                    <p className="truncate text-xs text-muted-foreground">
                      {session.location?.name ?? 'Location not assigned'}
                    </p>
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
