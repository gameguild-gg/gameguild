import type { TestingLabPublicTestingEventProjection } from "@game-guild/client";

export type TestingEventStatus =
  "open" | "in-progress" | "completed" | "closed";

export interface TestingEventViewModel {
  id: string;
  title: string;
  description: string;
  mode: string;
  status: TestingEventStatus;
  statusLabel: string;
  startsAt?: string;
  endsAt?: string;
  location: string;
  testerCount: number;
  testerLimit: number | null;
  projectCount: number;
  projectLimit: number | null;
  availableTesterCount: number | null;
  scheduleCount: number;
}

const STATUS_LABELS: Record<TestingEventStatus, string> = {
  open: "Open",
  "in-progress": "In Progress",
  completed: "Completed",
  closed: "Closed",
};

function normalizeStatus(status?: string): TestingEventStatus {
  if (status === "ApplicationsOpen" || status === "Scheduled") return "open";
  if (status === "Active") return "in-progress";
  if (status === "Completed") return "completed";
  return "closed";
}

function sumLimit(values: Array<number | null | undefined>): number | null {
  if (values.length === 0 || values.some((value) => value == null)) return null;
  return values.reduce<number>((total, value) => total + (value ?? 0), 0);
}

export function presentTestingEvents(
  events: TestingLabPublicTestingEventProjection[],
): TestingEventViewModel[] {
  return events.map((event) => {
    const slots = event.slots ?? [];
    const status = normalizeStatus(event.status);
    const locations = [
      ...new Set(
        slots.flatMap((slot) => {
          const location = [slot.campusName, slot.roomName]
            .filter(Boolean)
            .join(" - ");
          return location ? [location] : [];
        }),
      ),
    ];
    const startsAt =
      slots
        .map((slot) => slot.startsAt)
        .filter((value): value is string => Boolean(value))
        .sort()[0] ?? event.startsAt;
    const endsAt =
      slots
        .map((slot) => slot.endsAt)
        .filter((value): value is string => Boolean(value))
        .sort()
        .at(-1) ?? event.endsAt;

    return {
      id: event.id ?? "",
      title: event.name?.trim() || "Untitled testing event",
      description:
        event.description?.trim() ||
        "A managed GameGuild project testing event.",
      mode: event.mode === "InPerson" ? "In person" : (event.mode ?? "Online"),
      status,
      statusLabel: STATUS_LABELS[status],
      startsAt,
      endsAt,
      location:
        locations.length > 0
          ? locations.join(", ")
          : event.mode === "Online"
            ? "Online"
            : "Location pending",
      testerCount: slots.reduce(
        (total, slot) => total + (slot.registeredTesterCount ?? 0),
        0,
      ),
      testerLimit: sumLimit(slots.map((slot) => slot.maxTesters)),
      projectCount: slots.reduce(
        (total, slot) => total + (slot.approvedProjectCount ?? 0),
        0,
      ),
      projectLimit: sumLimit(slots.map((slot) => slot.maxProjects)),
      availableTesterCount: slots.some(
        (slot) => slot.availableTesterCount == null,
      )
        ? null
        : slots.reduce(
            (total, slot) => total + (slot.availableTesterCount ?? 0),
            0,
          ),
      scheduleCount: slots.length,
    };
  });
}
