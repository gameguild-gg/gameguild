import type { TestingLabTestingEventProjection } from "@game-guild/client";
import { describe, expect, it } from "vitest";
import {
  countLabel,
  formatCapacity,
  formatEventDateTime,
  isTestingEventReadOnly,
} from "./event-workspace";

describe("testing event workspace formatters", () => {
  it.each([
    ["Completed", true],
    ["Cancelled", true],
    ["Draft", false],
    [undefined, false],
  ])("classifies %s events as read-only: %s", (status, expected) => {
    expect(
      isTestingEventReadOnly({ status } as TestingLabTestingEventProjection),
    ).toBe(expected);
  });

  it("formats valid event dates in UTC", () => {
    expect(formatEventDateTime("2026-09-15T18:30:00.000Z")).toBe(
      "Sep 15, 2026, 6:30 PM UTC",
    );
  });

  it.each([undefined, null, "not-a-date"])(
    "treats an absent or invalid date as unscheduled",
    (value) => {
      expect(formatEventDateTime(value)).toBe("Not scheduled");
    },
  );

  it.each([
    [3, 8, "3/8"],
    [undefined, 8, "0/8"],
    [3, null, "3/unlimited"],
    [undefined, undefined, "0/unlimited"],
  ])("formats capacity %s/%s", (current, maximum, expected) => {
    expect(formatCapacity(current, maximum)).toBe(expected);
  });

  it("pluralizes counts and accepts an irregular plural", () => {
    expect(countLabel(1, "session")).toBe("1 session");
    expect(countLabel(2, "session")).toBe("2 sessions");
    expect(countLabel(2, "person", "people")).toBe("2 people");
  });
});
