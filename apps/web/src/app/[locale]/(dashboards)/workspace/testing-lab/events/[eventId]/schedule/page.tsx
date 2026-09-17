import {
  ManageTestingEventSlotDialog,
  TestingTimeSlotPlanner,
} from "@/components/testing-lab/testing-event-management";
import { TestingLabPageHeader } from "@/components/testing-lab/testing-lab-page-header";
import {
  formatCapacity,
  formatEventDateTime,
  isTestingEventReadOnly,
} from "@/lib/testing-lab/event-workspace";
import { getTestingEventWorkspaceData } from "@/lib/testing-lab/events-queries";
import { Badge } from "@game-guild/ui/components/badge";
import { CalendarDays, MapPin, UsersRound } from "lucide-react";
import { notFound } from "next/navigation";

export default async function TestingEventSchedulePage({
  params,
}: {
  params: Promise<{ eventId: string }>;
}) {
  const { eventId } = await params;
  const detail = await getTestingEventWorkspaceData(eventId);
  if (!detail.event) notFound();
  const event = detail.event;
  const readOnly = isTestingEventReadOnly(event);

  return (
    <div className="space-y-6">
      <TestingLabPageHeader
        headingLevel={2}
        icon={CalendarDays}
        title="Schedule"
        description="Turn the event window into clear testing slots and control capacity in one place."
      />

      {!readOnly ? (
        <TestingTimeSlotPlanner event={event} existingSlots={detail.slots} />
      ) : null}

      {detail.slots.length > 0 ? (
        <section aria-labelledby="saved-slots-heading" className="space-y-3">
          <div className="flex items-end justify-between gap-4">
            <div>
              <h2 id="saved-slots-heading" className="font-semibold">
                Saved time slots
              </h2>
              <p className="mt-0.5 text-sm text-muted-foreground">
                Manage individual slots without changing the rest of the
                schedule.
              </p>
            </div>
            <span className="text-sm tabular-nums text-muted-foreground">
              {detail.slots.length} total
            </span>
          </div>
          <div className="overflow-hidden rounded-lg border">
            {detail.slots.map((slot, index) => (
              <article
                key={slot.id}
                className="grid gap-4 border-b px-4 py-4 last:border-b-0 md:grid-cols-[minmax(0,1.5fr)_minmax(12rem,0.8fr)_auto] md:items-center"
              >
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="text-xs font-medium text-muted-foreground">
                      Slot {index + 1}
                    </span>
                    <Badge variant="outline">{slot.mode}</Badge>
                  </div>
                  <p className="mt-1.5 text-sm font-semibold tabular-nums">
                    {formatEventDateTime(
                      slot.startsAt,
                      event.timeZoneId ?? "UTC",
                    )}
                    <span className="mx-1.5 text-muted-foreground">to</span>
                    {formatEventDateTime(
                      slot.endsAt,
                      event.timeZoneId ?? "UTC",
                    )}
                  </p>
                  <p className="mt-1.5 flex items-start gap-2 text-sm text-muted-foreground">
                    <MapPin className="mt-0.5 size-4 shrink-0" />
                    <span className="truncate">
                      {slot.mode === "Online"
                        ? (slot.meetingUrl ?? "Online link not configured")
                        : `${slot.campusName ?? "Campus not set"} / ${slot.roomName ?? "Room not set"}`}
                    </span>
                  </p>
                </div>

                <dl className="grid grid-cols-2 gap-4 text-sm">
                  <div>
                    <dt className="flex items-center gap-1.5 text-xs text-muted-foreground">
                      <UsersRound className="size-3.5" />
                      Testers
                    </dt>
                    <dd className="mt-1 font-medium tabular-nums">
                      {formatCapacity(
                        slot.registeredTesterCount,
                        slot.maxTesters,
                      )}
                    </dd>
                  </div>
                  <div>
                    <dt className="text-xs text-muted-foreground">Projects</dt>
                    <dd className="mt-1 font-medium tabular-nums">
                      {formatCapacity(
                        slot.approvedProjectCount,
                        slot.maxProjects,
                      )}
                    </dd>
                  </div>
                </dl>

                {!readOnly ? (
                  <div className="md:justify-self-end">
                    <ManageTestingEventSlotDialog
                      eventId={eventId}
                      slot={slot}
                      eventStartsAt={event.startsAt}
                      eventEndsAt={event.endsAt}
                      timeZoneId={event.timeZoneId ?? "UTC"}
                    />
                  </div>
                ) : null}
              </article>
            ))}
          </div>
        </section>
      ) : readOnly ? (
        <div className="rounded-lg border border-dashed px-6 py-12 text-center">
          <CalendarDays className="mx-auto size-6 text-muted-foreground" />
          <h2 className="mt-3 font-medium">No saved time slots</h2>
          <p className="mt-1 text-sm text-muted-foreground">
            This event does not have a testing schedule yet.
          </p>
        </div>
      ) : null}
    </div>
  );
}
