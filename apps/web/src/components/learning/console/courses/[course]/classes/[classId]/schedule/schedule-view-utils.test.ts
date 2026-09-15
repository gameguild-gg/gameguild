import { afterEach, describe, expect, it, vi } from "vitest";

import {
  calendarDayKey,
  formatCalendarDay,
  formatScheduleDate,
  itemPrimaryDate,
  itemTypeLabel,
  scheduleItems,
} from "./schedule-view-utils";

describe("schedule view utilities", () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it("returns an empty list for missing items and sorts by week then order", () => {
    expect(scheduleItems({ items: undefined } as never)).toEqual([]);

    const unordered = [
      { id: "week-2", instructionalWeek: 2, sortOrder: 0 },
      { id: "week-1-second", instructionalWeek: 1, sortOrder: 2 },
      { id: "week-1-first", instructionalWeek: 1, sortOrder: 1 },
      { id: "unscheduled" },
    ];
    expect(
      scheduleItems({ items: unordered } as never).map((item) => item.id),
    ).toEqual(["unscheduled", "week-1-first", "week-1-second", "week-2"]);
    expect(unordered.map((item) => item.id)).toEqual([
      "week-2",
      "week-1-second",
      "week-1-first",
      "unscheduled",
    ]);

    expect(
      scheduleItems({
        items: [
          { id: "missing-week" },
          { id: "defined-week", instructionalWeek: 1 },
        ],
      } as never).map((item) => item.id),
    ).toEqual(["missing-week", "defined-week"]);
    expect(
      scheduleItems({
        items: [
          { id: "defined-week", instructionalWeek: 1 },
          { id: "missing-week" },
        ],
      } as never).map((item) => item.id),
    ).toEqual(["missing-week", "defined-week"]);
    expect(
      scheduleItems({
        items: [
          { id: "missing-order", instructionalWeek: 1 },
          { id: "defined-order", instructionalWeek: 1, sortOrder: 1 },
        ],
      } as never).map((item) => item.id),
    ).toEqual(["missing-order", "defined-order"]);
    expect(
      scheduleItems({
        items: [
          { id: "defined-order", instructionalWeek: 1, sortOrder: 1 },
          { id: "missing-order", instructionalWeek: 1 },
        ],
      } as never).map((item) => item.id),
    ).toEqual(["missing-order", "defined-order"]);
  });

  it("chooses each supported primary date in priority order", () => {
    expect(
      itemPrimaryDate({
        availableFrom: "available",
        startsAt: "starts",
        dueAt: "due",
        availableUntil: "until",
      } as never),
    ).toBe("available");
    expect(itemPrimaryDate({ startsAt: "starts", dueAt: "due" } as never)).toBe(
      "starts",
    );
    expect(
      itemPrimaryDate({ dueAt: "due", availableUntil: "until" } as never),
    ).toBe("due");
    expect(itemPrimaryDate({ availableUntil: "until" } as never)).toBe("until");
    expect(itemPrimaryDate({} as never)).toBeNull();
  });

  it("formats valid dates and rejects absent or invalid values", () => {
    expect(formatScheduleDate(undefined, "UTC")).toBeNull();
    expect(formatScheduleDate("not-a-date", "UTC")).toBeNull();
    expect(
      formatScheduleDate("2026-09-15T18:30:00.000Z", "UTC", {
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
        hour: undefined,
        minute: undefined,
      }),
    ).toContain("2026");
    expect(formatScheduleDate("2026-09-15T18:30:00.000Z", null)).toBeTypeOf(
      "string",
    );
    expect(formatCalendarDay("2026-09-15T18:30:00.000Z", "UTC")).toContain(
      "2026",
    );
    expect(formatCalendarDay("2026-09-15T18:30:00.000Z", undefined)).toContain(
      "2026",
    );
  });

  it("builds timezone-aware calendar keys and safely handles absent parts", () => {
    expect(calendarDayKey("2026-09-15T23:30:00.000Z", "UTC")).toBe(
      "2026-09-15",
    );
    expect(calendarDayKey("2026-09-15T23:30:00.000Z", null)).toBe("2026-09-15");

    vi.spyOn(Intl, "DateTimeFormat").mockImplementationOnce(
      function DateTimeFormatStub() {
        return { formatToParts: () => [] } as never;
      } as never,
    );
    expect(calendarDayKey("2026-09-15T23:30:00.000Z", "UTC")).toBe("--");
  });

  it("labels every schedule item type", () => {
    expect(itemTypeLabel("ContentRelease")).toBe("Content release");
    expect(itemTypeLabel("LiveSession")).toBe("Live class");
    expect(itemTypeLabel("AssessmentWindow")).toBe("Assessment");
    expect(itemTypeLabel(undefined)).toBe("Milestone");
  });
});
