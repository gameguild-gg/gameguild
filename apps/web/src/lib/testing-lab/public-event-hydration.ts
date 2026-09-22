"use server";

import { getPublicTestingEvent } from "./events-public-queries";

export interface TestingEventHydration {
  name: string;
  description: string | null;
  startsAt: string | null;
  endsAt: string | null;
  mode: string | null;
  status: string | null;
  registeredTesterCount: number;
  maxTesters: number | null;
  availableTesterCount: number | null;
}

const EVENT_ID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function sumLimit(values: Array<number | null | undefined>): number | null {
  if (values.length === 0 || values.some((value) => value == null)) return null;
  return values.reduce<number>((total, value) => total + value!, 0);
}

/**
 * Loads the public projection of a testing event so feed embeds can render the
 * same detail as session cards (description, schedule, capacity). The event id
 * is validated before it reaches the API request path.
 */
export async function hydratePublicTestingEvent(
  eventId: string,
): Promise<TestingEventHydration | null> {
  if (!EVENT_ID_PATTERN.test(eventId)) return null;
  try {
    const event = await getPublicTestingEvent(eventId);
    if (!event) return null;
    const slots = event.slots ?? [];
    return {
      name: event.name?.trim() || "Testing event",
      description: event.description?.trim() || null,
      startsAt:
        slots
          .map((slot) => slot.startsAt)
          .filter((value): value is string => Boolean(value))
          .sort()[0] ?? event.startsAt ?? null,
      endsAt:
        slots
          .map((slot) => slot.endsAt)
          .filter((value): value is string => Boolean(value))
          .sort()
          .at(-1) ?? event.endsAt ?? null,
      mode: event.mode ?? null,
      status: event.status ?? null,
      registeredTesterCount: slots.reduce(
        (total, slot) => total + (slot.registeredTesterCount ?? 0),
        0,
      ),
      maxTesters: sumLimit(slots.map((slot) => slot.maxTesters)),
      availableTesterCount: slots.some(
        (slot) => slot.availableTesterCount == null,
      )
        ? null
        : slots.reduce(
            (total, slot) => total + slot.availableTesterCount!,
            0,
          ),
    };
  } catch {
    // A failed hydration leaves the embed on its parsed-from-text fallback.
    return null;
  }
}
