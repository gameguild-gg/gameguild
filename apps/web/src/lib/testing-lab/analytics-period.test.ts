import { describe, expect, it } from "vitest";
import { resolveTestingLabAnalyticsPeriod } from "./analytics-period";

describe("resolveTestingLabAnalyticsPeriod", () => {
  const now = new Date("2026-09-15T18:30:00.000Z");

  it.each([
    ["7", "2026-09-09", "2026-09-15"],
    ["30", "2026-08-17", "2026-09-15"],
    ["90", "2026-06-18", "2026-09-15"],
  ])("resolves the %s-day preset as an inclusive UI range", (range, from, to) => {
    expect(resolveTestingLabAnalyticsPeriod({ range }, now)).toEqual({
      range,
      fromInput: from,
      toInput: to,
      fromDate: `${from}T00:00:00.000Z`,
      toDate: "2026-09-16T00:00:00.000Z",
    });
  });

  it("uses an exclusive API end date for a valid custom range", () => {
    expect(
      resolveTestingLabAnalyticsPeriod(
        { from: "2026-02-27", to: "2026-03-01" },
        now,
      ),
    ).toEqual({
      range: "custom",
      fromInput: "2026-02-27",
      toInput: "2026-03-01",
      fromDate: "2026-02-27T00:00:00.000Z",
      toDate: "2026-03-02T00:00:00.000Z",
    });
  });

  it.each([
    { range: "365" },
    { range: "invalid" },
    { from: "2026-02-30", to: "2026-03-01" },
    { from: "2026/02/27", to: "2026-03-01" },
    { from: "2026-03-02", to: "2026-03-01" },
    { from: "2026-03-01" },
  ])("falls back to 30 days for invalid input %#", (params) => {
    expect(resolveTestingLabAnalyticsPeriod(params, now)).toMatchObject({
      range: "30",
      fromInput: "2026-08-17",
      toInput: "2026-09-15",
    });
  });
});
