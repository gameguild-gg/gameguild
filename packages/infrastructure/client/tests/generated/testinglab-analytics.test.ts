import { describe, expect, it, vi } from "vitest";
import type { ApiClient } from "../../src/runtime/client.js";
import { TestinglabAnalyticsModule } from "../../src/generated/modules/testinglab-analytics.gen.js";

describe("TestinglabAnalyticsModule", () => {
  it("requests the tenant analytics report with period and comparison parameters", async () => {
    const request = vi
      .fn()
      .mockResolvedValue({ ok: true, data: { events: [] } });
    const module = new TestinglabAnalyticsModule({
      request,
      getBaseUrl: () => "https://api.example.com",
    } as ApiClient);

    await module.getTestingAnalytics({
      fromDate: "2026-07-01T00:00:00.000Z",
      toDate: "2026-07-08T00:00:00.000Z",
      includeComparison: true,
    });

    expect(request).toHaveBeenCalledWith({
      method: "GET",
      path: "/v1/testing/analytics",
      params: {
        fromDate: "2026-07-01T00:00:00.000Z",
        toDate: "2026-07-08T00:00:00.000Z",
        includeComparison: true,
      },
      requiresAuth: true,
    });
  });

  it("requests an authenticated CSV export", async () => {
    const request = vi
      .fn()
      .mockResolvedValue({ ok: true, data: "event,applications" });
    const module = new TestinglabAnalyticsModule({
      request,
      getBaseUrl: () => "https://api.example.com",
    } as ApiClient);

    await module.getTestingAnalyticsExport({
      fromDate: "2026-07-01T00:00:00.000Z",
      toDate: "2026-07-08T00:00:00.000Z",
    });

    expect(request).toHaveBeenCalledWith({
      method: "GET",
      path: "/v1/testing/analytics/export",
      params: {
        fromDate: "2026-07-01T00:00:00.000Z",
        toDate: "2026-07-08T00:00:00.000Z",
      },
      requiresAuth: true,
    });
  });
});
