import { TestingSlotRegistrations } from '@/components/testing-lab/testing-event-management';
import { TestingLabPageHeader } from '@/components/testing-lab/testing-lab-page-header';
import {
  formatEventDateTime,
  isTestingEventReadOnly,
} from '@/lib/testing-lab/event-workspace';
import { getTestingEventWorkspaceData } from '@/lib/testing-lab/events-queries';
import { Badge } from '@game-guild/ui/components/badge';
import { UsersRound } from 'lucide-react';
import { notFound } from 'next/navigation';

export default async function TestingEventTestersPage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const detail = await getTestingEventWorkspaceData(eventId);
  if (!detail.event) notFound();
  const readOnly = isTestingEventReadOnly(detail.event);
  const total = Object.values(detail.registrationsBySlot).flat().length;

  return (
    <div className="space-y-5">
      <TestingLabPageHeader
        icon={UsersRound}
        title="Testers and attendance"
        description="Manage tester participation per slot and preserve check-in, check-out, no-show, and completion evidence."
      />

      <p className="text-sm text-muted-foreground">
        {total === 1 ? '1 tester registered across this event.' : `${total} testers registered across this event.`}
      </p>

      {detail.slots.length === 0 ? (
        <div className="rounded-md border border-dashed p-8 text-center">
          <h2 className="font-medium">No tester schedule available</h2>
          <p className="mt-1 text-sm text-muted-foreground">Create event slots before accepting tester registrations.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {detail.slots.map((slot) => {
            const registrations = slot.id ? detail.registrationsBySlot[slot.id] ?? [] : [];
            return (
              <section key={slot.id} className="rounded-md border p-4">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <div>
                    <h2 className="font-semibold">{formatEventDateTime(slot.startsAt)}</h2>
                    <p className="text-sm text-muted-foreground">
                      {slot.campusName ?? slot.meetingUrl ?? slot.mode}
                    </p>
                  </div>
                  <Badge variant="outline">{registrations.length} registered</Badge>
                </div>
                <TestingSlotRegistrations
                  eventId={eventId}
                  registrations={registrations}
                  readOnly={readOnly}
                />
              </section>
            );
          })}
        </div>
      )}
    </div>
  );
}
