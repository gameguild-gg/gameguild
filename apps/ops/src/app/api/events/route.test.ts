import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  listEventForAllNamespaces: vi.fn(),
}));

vi.mock("../../../lib/k8s", () => ({
  k8sCore: { listEventForAllNamespaces: mocks.listEventForAllNamespaces },
  k8sApps: {},
  k8sCustom: {},
}));

import { GET } from "./route";

beforeEach(() => mocks.listEventForAllNamespaces.mockReset());

describe("GET /api/events", () => {
  it("filters by type=Warning, sorts desc by lastTimestamp, caps 50", async () => {
    mocks.listEventForAllNamespaces.mockResolvedValueOnce({
      items: [
        {
          metadata: { name: "e2", namespace: "ns" },
          reason: "BackOff",
          message: "older",
          lastTimestamp: new Date("2024-01-01T00:00:00Z"),
          count: 3,
        },
        {
          metadata: { name: "e1", namespace: "ns" },
          reason: "FailedScheduling",
          message: "newer",
          lastTimestamp: new Date("2024-06-01T00:00:00Z"),
          count: 1,
        },
        {
          metadata: { name: "e-blank", namespace: "ns" },
          reason: "Ignored",
          message: "no timestamp",
          lastTimestamp: undefined,
          count: 0,
        },
      ],
    });

    const res = await GET();
    expect(res.status).toBe(200);

    // The route must forward the type=Warning filter to k8s.
    expect(mocks.listEventForAllNamespaces).toHaveBeenCalledWith({
      fieldSelector: "type=Warning",
    });

    const body = await res.json();
    // Filtered out the no-timestamp event; newest first.
    expect(body).toEqual([
      {
        name: "e1",
        namespace: "ns",
        reason: "FailedScheduling",
        message: "newer",
        lastTimestamp: "2024-06-01T00:00:00.000Z",
        count: 1,
      },
      {
        name: "e2",
        namespace: "ns",
        reason: "BackOff",
        message: "older",
        lastTimestamp: "2024-01-01T00:00:00.000Z",
        count: 3,
      },
    ]);
  });

  it("returns 500 with { error } when listEventForAllNamespaces throws", async () => {
    mocks.listEventForAllNamespaces.mockRejectedValueOnce(new Error("boom"));

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toBe("boom");
  });
});
