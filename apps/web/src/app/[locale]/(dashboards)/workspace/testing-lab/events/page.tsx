import { CreateTestingEventDialog, RestoreTestingEventDialog } from '@/components/testing-lab/testing-event-management';
import { TestingEventDirectoryFilters } from '@/components/testing-lab/testing-event-directory-filters';
import { formatTestingEventStatus } from '@/lib/testing-lab/format';
import { countLabel, formatEventDateRange } from '@/lib/testing-lab/event-workspace';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { TestingLabAccessIssues, TestingLabEmptyState } from '@/components/testing-lab/testing-lab-state';
import { Link } from '@/i18n/navigation';
import { getTestingLabSettings } from '@/lib/testing-lab';
import { getArchivedTestingEventsDirectory, getTestingEventTemplates, getTestingEventsDirectory } from '@/lib/testing-lab/events-queries';
import type { TestingLabTestingEventStatus } from '@game-guild/client';
import { Badge } from '@game-guild/ui/components/badge';
import { buttonVariants } from '@game-guild/ui/components/button-variants';
import { CalendarDays, ChevronRight, FlaskConical, Layers3 } from 'lucide-react';

const statuses: TestingLabTestingEventStatus[] = [
  'Draft',
  'ApplicationsOpen',
  'ApplicationsClosed',
  'Scheduled',
  'Active',
  'Completed',
  'Cancelled',
];

function eventDateRange(startsAt?: string, endsAt?: string, timeZoneId?: string | null) {
  return formatEventDateRange(startsAt, endsAt, timeZoneId || 'UTC');
}

export default async function TestingEventsPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; q?: string; page?: string; archived?: string }>;
}) {
  const query = await searchParams;
  const archived = query.archived === 'true';
  const selectedStatus = statuses.find((status) => status === query.status);
  const directory = archived
    ? await getArchivedTestingEventsDirectory({ skip: 0, take: 100 })
    : await getTestingEventsDirectory({ status: selectedStatus, skip: 0, take: 100 });
  const templates = archived ? { templates: [], accessIssues: [] } : await getTestingEventTemplates();
  const labSettings = await getTestingLabSettings();
  const searchTerm = query.q?.trim().toLocaleLowerCase() ?? '';
  const filteredEvents = directory.events.filter((event) =>
    searchTerm
      ? `${event.name ?? ''} ${event.description ?? ''} ${event.mode ?? ''}`.toLocaleLowerCase().includes(searchTerm)
      : true,
  );
  const pageSize = 25;
  const page = Math.max(1, Number.parseInt(query.page ?? '1', 10) || 1);
  const pageCount = Math.max(1, Math.ceil(filteredEvents.length / pageSize));
  const visibleEvents = filteredEvents.slice((Math.min(page, pageCount) - 1) * pageSize, Math.min(page, pageCount) * pageSize);
  const querySuffix = `${archived ? '&archived=true' : ''}${selectedStatus ? `&status=${selectedStatus}` : ''}${query.q ? `&q=${encodeURIComponent(query.q)}` : ''}`;

  return (
    <div className="flex flex-col gap-6 p-4 lg:p-6">
      <TestingLabPageHeader
        icon={CalendarDays}
        title="Testing events"
        description="Publish playtests, review game applications, and organize tester sessions."
        actions={
          <CreateTestingEventDialog
            templates={templates.templates}
            defaultTimeZone={labSettings.settings?.timezone ?? 'UTC'}
          />
        }
        navigation={
          <TestingEventDirectoryFilters
            search={query.q}
            status={selectedStatus}
            archived={archived}
          />
        }
      />
      <TestingLabAccessIssues issues={[...directory.accessIssues, ...templates.accessIssues, ...labSettings.accessIssues]} />
      {visibleEvents.length === 0 ? (
        <TestingLabEmptyState
          title={searchTerm ? 'No matching events' : archived ? 'No archived events' : 'No testing events'}
          description={searchTerm ? 'Adjust the search or status filter.' : archived ? 'Completed and cancelled events can be archived from their management workspace.' : 'Create an event to collect project applications and organize independent online or campus test slots.'}
          action={
            archived ? undefined : (
              <CreateTestingEventDialog
                templates={templates.templates}
                defaultTimeZone={labSettings.settings?.timezone ?? 'UTC'}
              />
            )
          }
        />
      ) : (
        <section className="divide-y border-y" aria-label="Testing event directory">
          {visibleEvents.map((event) => (
            <article key={event.id} className="grid gap-4 p-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
              <div className="min-w-0">
                <div className="flex flex-wrap items-center gap-2">
                  <h2 className="truncate font-semibold">{event.name ?? 'Untitled testing event'}</h2>
                  <Badge variant="outline">{formatTestingEventStatus(event.status)}</Badge>
                  <Badge variant="secondary">{event.mode ?? 'Online'}</Badge>
                </div>
                <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">{event.description ?? 'No event brief.'}</p>
                <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1 text-xs text-muted-foreground">
                  <span className="flex items-center gap-1.5"><CalendarDays className="size-3.5" />{eventDateRange(event.startsAt, event.endsAt, event.timeZoneId)}</span>
                  <span className="flex items-center gap-1.5"><Layers3 className="size-3.5" />{event.slotCount ?? 0} {event.slotCount === 1 ? 'session' : 'sessions'}</span>
                  <span className="flex items-center gap-1.5"><FlaskConical className="size-3.5" />{countLabel(event.applicationCount ?? 0, 'game application')}</span>
                </div>
              </div>
              {archived ? (
                <RestoreTestingEventDialog event={event} />
              ) : event.id ? (
                <Link
                  href={`/workspace/testing-lab/events/${event.id}`}
                  className={buttonVariants({ variant: 'outline' })}
                >
                  Open event workspace<ChevronRight className="ml-2 size-4" />
                </Link>
              ) : null}
            </article>
          ))}
        </section>
      )}
      {pageCount > 1 ? (
        <nav aria-label="Testing event pages" className="flex items-center justify-end gap-2">
          {page <= 1 ? (
            <span
              aria-disabled="true"
              className={buttonVariants({ size: 'sm', variant: 'outline', className: 'pointer-events-none opacity-50' })}
            >
              Previous
            </span>
          ) : (
            <Link
              href={`/workspace/testing-lab/events?page=${page - 1}${querySuffix}`}
              className={buttonVariants({ size: 'sm', variant: 'outline' })}
            >
              Previous
            </Link>
          )}
          <span className="text-sm text-muted-foreground">Page {Math.min(page, pageCount)} of {pageCount}</span>
          {page >= pageCount ? (
            <span
              aria-disabled="true"
              className={buttonVariants({ size: 'sm', variant: 'outline', className: 'pointer-events-none opacity-50' })}
            >
              Next
            </span>
          ) : (
            <Link
              href={`/workspace/testing-lab/events?page=${page + 1}${querySuffix}`}
              className={buttonVariants({ size: 'sm', variant: 'outline' })}
            >
              Next
            </Link>
          )}
        </nav>
      ) : null}
    </div>
  );
}
