import type { TestingLabTestingEventProjection } from "@game-guild/client";
import { describe, expect, it } from "vitest";

import {
  calendarEventSegments,
  calendarRange,
  calendarRangeLabel,
  parseCalendarView,
  shiftCalendarAnchor,
} from "./calendar";

describe("Testing Lab calendar helpers", () => {
  it("uses Month as the safe default calendar view", () => {
    expect(parseCalendarView(null)).toBe("month");
    expect(parseCalendarView("week")).toBe("week");
    expect(parseCalendarView("unexpected")).toBe("month");
  });

  it.each(["day", "week", "month", "year", "schedule", "3days"] as const)(
    "accepts the supported %s view",
    (view) => expect(parseCalendarView(view)).toBe(view),
  );

  it("renders only the Monday-first weeks needed by a month", () => {
    const range = calendarRange(new Date(2026, 8, 10), "month", true);
    const weekdaysOnly = calendarRange(new Date(2026, 8, 10), "month", false);

    expect(range.days).toHaveLength(35);
    expect(range.days[0]).toEqual(new Date(2026, 7, 31));
    expect(range.days.at(-1)).toEqual(new Date(2026, 9, 4));
    expect(range.days[0]?.getDay()).toBe(1);
    expect(weekdaysOnly.days).toHaveLength(25);
    expect(
      weekdaysOnly.days.every(
        (day) => day.getDay() !== 0 && day.getDay() !== 6,
      ),
    ).toBe(true);
  });

  it("moves the anchor by the visible calendar view", () => {
    const anchor = new Date(2026, 7, 10);

    expect(shiftCalendarAnchor(anchor, "month", 1)).toEqual(
      new Date(2026, 8, 10),
    );
    expect(shiftCalendarAnchor(anchor, "week", -1)).toEqual(
      new Date(2026, 7, 3),
    );
    expect(shiftCalendarAnchor(anchor, "3days", 1)).toEqual(
      new Date(2026, 7, 13),
    );
    expect(shiftCalendarAnchor(anchor, "day", -1)).toEqual(
      new Date(2026, 7, 9),
    );
    expect(shiftCalendarAnchor(anchor, "year", 1)).toEqual(
      new Date(2027, 7, 10),
    );
    expect(shiftCalendarAnchor(anchor, "schedule", -1)).toEqual(
      new Date(2026, 6, 11),
    );
  });

  it("builds day, three-day, week, schedule, and year ranges", () => {
    const anchor = new Date(2026, 8, 16, 15, 30);
    expect(calendarRange(anchor, "day", true).days).toHaveLength(1);
    expect(calendarRange(anchor, "3days", true).days).toHaveLength(3);
    expect(calendarRange(anchor, "week", false).days).toHaveLength(5);
    expect(calendarRange(anchor, "schedule", true).days).toHaveLength(90);
    expect(calendarRange(anchor, "year", true).days).toHaveLength(365);
  });

  it("formats labels for every calendar scale and cross-year range", () => {
    const anchor = new Date(2026, 8, 16);
    expect(calendarRangeLabel(anchor, "month", calendarRange(anchor, "month", true))).toBe(
      "September 2026",
    );
    expect(calendarRangeLabel(anchor, "year", calendarRange(anchor, "year", true))).toBe("2026");
    expect(calendarRangeLabel(anchor, "day", calendarRange(anchor, "day", true))).toBe(
      "Wednesday, September 16, 2026",
    );
    expect(calendarRangeLabel(anchor, "week", calendarRange(anchor, "week", true))).toBe(
      "Sep 14 – Sep 20, 2026",
    );
    const crossYear = calendarRange(new Date(2026, 11, 31), "3days", true);
    expect(calendarRangeLabel(anchor, "3days", crossYear)).toBe(
      "Dec 31, 2026 – Jan 2, 2027",
    );
  });

  it("places multi-day Testing Lab events on every visible calendar day they occupy", () => {
    const event = {
      id: "event-1",
      name: "Campus playtest",
      startsAt: "2026-08-10T18:00:00.000Z",
      endsAt: "2026-08-12T20:00:00.000Z",
      status: "Scheduled",
    } as TestingLabTestingEventProjection;
    const range = calendarRange(new Date(2026, 7, 10), "week", true);

    const segments = calendarEventSegments([event], range);

    expect(segments).toHaveLength(3);
    expect(segments.map((segment) => segment.dayKey)).toEqual([
      "2026-08-10",
      "2026-08-11",
      "2026-08-12",
    ]);
    expect(segments.map(({ startsOnDay, endsOnDay }) => [startsOnDay, endsOnDay])).toEqual([
      [true, false],
      [false, false],
      [false, true],
    ]);
  });

  it("ignores unscheduled events and normalizes missing, invalid, or backwards ends", () => {
    const range = calendarRange(new Date(2026, 7, 10), "day", true);
    const segments = calendarEventSegments(
      [
        { id: "missing" },
        { id: "invalid", startsAt: "invalid" },
        { id: "no-end", startsAt: "2026-08-10T20:00:00Z" },
        { id: "invalid-end", startsAt: "2026-08-10T19:00:00Z", endsAt: "invalid" },
        { id: "backwards", startsAt: "2026-08-10T18:00:00Z", endsAt: "2026-08-09T18:00:00Z" },
        { id: "outside", startsAt: "2026-08-11T18:00:00Z" },
      ] as TestingLabTestingEventProjection[],
      range,
    );

    expect(segments.map((segment) => segment.event.id)).toEqual([
      "backwards",
      "invalid-end",
      "no-end",
    ]);
    expect(segments.every((segment) => segment.startsOnDay && segment.endsOnDay)).toBe(true);
  });
});
