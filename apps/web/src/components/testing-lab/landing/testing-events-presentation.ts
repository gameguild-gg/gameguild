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
  testerRegistrationOpen: boolean;
  gameSubmissionsOpen: boolean;
  gameImageUrl: string | null;
  scheduleCount: number;
  timeZoneId: string;
  dateLocale: string;
  hour12: boolean;
}

export interface TestingEventFormatting {
  timeZoneId?: string;
  dateLocale?: string;
  hour12?: boolean;
}

const STATUS_LABELS: Record<TestingEventStatus, string> = {
  open: "Open",
  "in-progress": "In progress",
  completed: "Playtest ended",
  closed: "Registration closed",
};

function timestamp(value?: string | null): number | null {
  if (!value) return null;
  const parsed = new Date(value).valueOf();
  return Number.isFinite(parsed) ? parsed : null;
}

function remainingTesterCapacity(slot: NonNullable<TestingLabPublicTestingEventProjection["slots"]>[number]): number | null {
  if (slot.maxTesters == null) return null;
  if (slot.availableTesterCount != null) return Math.max(0, slot.availableTesterCount);
  return Math.max(0, slot.maxTesters - (slot.registeredTesterCount ?? 0));
}

function sumLimit(values: Array<number | null | undefined>): number | null {
  if (values.length === 0 || values.some((value) => value == null)) return null;
  return values.reduce<number>((total, value) => total + value!, 0);
}

export function presentTestingEvents(
  events: TestingLabPublicTestingEventProjection[],
  now = new Date(),
  formatting: TestingEventFormatting = {},
): TestingEventViewModel[] {
  return events.map((event) => {
    const slots = event.slots ?? [];
    const nowTimestamp = now.valueOf();
    const eventEndsAt = timestamp(event.endsAt);
    const eventEnded =
      event.status === "Completed" ||
      (eventEndsAt != null && eventEndsAt <= nowTimestamp);
    const eligibleTesterStatus = ["ApplicationsClosed", "Scheduled", "Active"].includes(
      event.status ?? "",
    );
    const configurationReady = Boolean(
      event.configuration?.frozenAt && event.configuration.testerRegistrationSchema,
    );
    const testerSlots = slots.filter((slot) => {
      const slotEndsAt = timestamp(slot.endsAt) ?? eventEndsAt;
      return (
        !eventEnded &&
        eligibleTesterStatus &&
        configurationReady &&
        (slotEndsAt == null || slotEndsAt > nowTimestamp)
      );
    });
    const testerRegistrationOpen = testerSlots.length > 0;
    const applicationsOpenAt = timestamp(event.applicationsOpenAt);
    const applicationsCloseAt = timestamp(event.applicationsCloseAt);
    const withinApplicationsWindow =
      (applicationsOpenAt == null || applicationsOpenAt <= nowTimestamp) &&
      (applicationsCloseAt == null || applicationsCloseAt >= nowTimestamp);
    const gameSubmissionsOpen =
      !eventEnded && event.status === "ApplicationsOpen" && withinApplicationsWindow;
    const status: TestingEventStatus = eventEnded
      ? "completed"
      : event.status === "Cancelled"
        ? "closed"
        : event.status === "Active"
          ? "in-progress"
          : testerRegistrationOpen || gameSubmissionsOpen
            ? "open"
            : "closed";
    const statusLabel =
      event.status === "Cancelled"
        ? "Cancelled"
        : event.status === "Active"
          ? STATUS_LABELS[status]
          : testerRegistrationOpen
            ? "Tester sign-up open"
            : gameSubmissionsOpen
              ? "Game submissions open"
              : STATUS_LABELS[status];
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
      statusLabel,
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
      availableTesterCount: !testerRegistrationOpen
        ? 0
        : testerSlots.some((slot) => remainingTesterCapacity(slot) == null)
          ? null
          : testerSlots.reduce(
              (total, slot) => total + (remainingTesterCapacity(slot) ?? 0),
              0,
            ),
      testerRegistrationOpen,
      gameSubmissionsOpen,
      gameImageUrl:
        event.games?.find((game) => Boolean(game.imageUrl?.trim()))?.imageUrl?.trim() ??
        null,
      scheduleCount: slots.length,
      timeZoneId: formatting.timeZoneId ?? "UTC",
      dateLocale: formatting.dateLocale ?? "en-US",
      hour12: formatting.hour12 ?? true,
    };
  });
}
