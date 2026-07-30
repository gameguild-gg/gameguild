import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import { formatEventDateTime } from '@/lib/testing-lab/event-workspace';
import { getTestingEventWorkspaceData } from '@/lib/testing-lab/events-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { BarChart3, CheckCircle2, Clock3 } from 'lucide-react';
import { notFound } from 'next/navigation';

export default async function TestingEventFeedbackPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const detail = await getTestingEventWorkspaceData(eventId);
  if (!detail.event) notFound();

  const registrations = detail.slots.flatMap((slot) =>
    (slot.id ? detail.registrationsBySlot[slot.id] ?? [] : []).map((registration) => ({
      ...registration,
      slot,
    })),
  );
  const pending = registrations.reduce(
    (sum, registration) => sum + (registration.pendingFeedbackCount ?? 0),
    0,
  );

  return (
    <div className="space-y-5">
      <TestingLabPageHeader
        icon={BarChart3}
        title="Feedback completion"
        description="Track required submissions by tester and slot before attendance is marked complete."
      />

      <section className="grid gap-3 sm:grid-cols-2">
        <article className="rounded-md border p-4">
          <div className="flex items-center gap-2 text-muted-foreground">
            <Clock3 className="size-4" />
            <span className="text-sm">Pending submissions</span>
          </div>
          <p className="mt-2 text-2xl font-semibold">{pending}</p>
        </article>
        <article className="rounded-md border p-4">
          <div className="flex items-center gap-2 text-muted-foreground">
            <CheckCircle2 className="size-4" />
            <span className="text-sm">Testers complete</span>
          </div>
          <p className="mt-2 text-2xl font-semibold">
            {registrations.filter((registration) => (registration.pendingFeedbackCount ?? 0) === 0).length}
          </p>
        </article>
      </section>

      {registrations.length === 0 ? (
        <div className="rounded-md border border-dashed p-8 text-center">
          <h2 className="font-medium">No tester feedback expected yet</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            Feedback obligations appear after testers register for an event slot.
          </p>
        </div>
      ) : (
        <div className="divide-y rounded-md border">
          {registrations.map((registration) => (
            <article key={registration.id} className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="min-w-0">
                <h2 className="truncate font-medium">{registration.userId}</h2>
                <p className="text-sm text-muted-foreground">
                  {formatEventDateTime(registration.slot.startsAt)}
                </p>
              </div>
              <Badge variant={(registration.pendingFeedbackCount ?? 0) > 0 ? 'outline' : 'secondary'}>
                {(registration.pendingFeedbackCount ?? 0) > 0
                  ? `${registration.pendingFeedbackCount} pending`
                  : 'Complete'}
              </Badge>
            </article>
          ))}
        </div>
      )}
    </div>
  );
}
