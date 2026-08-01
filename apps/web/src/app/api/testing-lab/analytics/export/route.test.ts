import { NextRequest } from "next/server";
import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  getTestingLabAnalyticsCsv: vi.fn(),
}));

vi.mock("@/lib/testing-lab", () => ({
  getTestingLabAnalyticsCsv: mocks.getTestingLabAnalyticsCsv,
}));

import { GET } from "./route";

describe("Testing Lab analytics CSV route", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getTestingLabAnalyticsCsv.mockResolvedValue({
      data: "event,applications\nJuly lab,4",
    });
  });

  it("returns an authenticated tenant CSV for the inclusive UI period", async () => {
    const response = await GET(
      new NextRequest(
        "http://localhost/api/testing-lab/analytics/export?from=2026-07-01&to=2026-07-31",
      ),
    );

    expect(mocks.getTestingLabAnalyticsCsv).toHaveBeenCalledWith({
      fromDate: "2026-07-01T00:00:00.000Z",
      toDate: "2026-08-01T00:00:00.000Z",
    });
    expect(response.status).toBe(200);
    expect(response.headers.get("content-type")).toContain("text/csv");
    expect(response.headers.get("content-disposition")).toContain(
      "testing-lab-2026-07-01-to-2026-07-31.csv",
    );
    await expect(response.text()).resolves.toBe(
      "event,applications\nJuly lab,4",
    );
  });

  it("rejects malformed or reversed periods", async () => {
    const response = await GET(
      new NextRequest(
        "http://localhost/api/testing-lab/analytics/export?from=2026-07-31&to=2026-07-01",
      ),
    );

    expect(response.status).toBe(400);
    expect(mocks.getTestingLabAnalyticsCsv).not.toHaveBeenCalled();
  });

  it("returns a recoverable gateway error when the API export fails", async () => {
    mocks.getTestingLabAnalyticsCsv.mockResolvedValue({
      data: null,
      issue: "Testing Lab analytics export returned 503",
    });

    const response = await GET(
      new NextRequest(
        "http://localhost/api/testing-lab/analytics/export?from=2026-07-01&to=2026-07-31",
      ),
    );

    expect(response.status).toBe(502);
    await expect(response.json()).resolves.toEqual({
      error: "Testing Lab analytics export returned 503",
    });
  });
});
