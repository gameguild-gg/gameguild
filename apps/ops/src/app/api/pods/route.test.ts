import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => ({
  listPodForAllNamespaces: vi.fn(),
}));

vi.mock("../../../lib/k8s", () => ({
  k8sCore: { listPodForAllNamespaces: mocks.listPodForAllNamespaces },
  k8sApps: {},
  k8sCustom: {},
}));

import { GET } from "./route";

beforeEach(() => mocks.listPodForAllNamespaces.mockReset());

describe("GET /api/pods", () => {
  it("groups pods by nodeName and summarizes status", async () => {
    const created = new Date("2024-01-01T00:00:00Z");
    mocks.listPodForAllNamespaces.mockResolvedValueOnce({
      items: [
        {
          metadata: { name: "pod-a", namespace: "default", creationTimestamp: created },
          spec: { nodeName: "bowser" },
          status: {
            phase: "Running",
            containerStatuses: [
              { ready: true, restartCount: 0 },
              { ready: false, restartCount: 2 },
            ],
          },
        },
        {
          metadata: { name: "pod-b", namespace: "kube-system", creationTimestamp: created },
          spec: { nodeName: "bowser" },
          status: {
            phase: "Pending",
            containerStatuses: [{ ready: false, restartCount: 5 }],
          },
        },
        {
          metadata: { name: "orphan", namespace: "default", creationTimestamp: created },
          spec: { nodeName: undefined },
          status: { phase: "Running", containerStatuses: [] },
        },
      ],
    });

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(Object.keys(body.nodes)).toEqual(["bowser"]);
    expect(body.nodes.bowser).toHaveLength(2);
    expect(body.nodes.bowser[0]).toEqual({
      name: "pod-a",
      namespace: "default",
      status: "Running",
      ready: "1/2",
      restarts: 2,
      age: expect.any(Number),
    });
    expect(body.nodes.bowser[1].restarts).toBe(5);
    expect(body.nodes.bowser[1].ready).toBe("0/1");
  });

  it("returns 500 with { error } when listPodForAllNamespaces throws", async () => {
    mocks.listPodForAllNamespaces.mockRejectedValueOnce(new Error("boom"));

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toBe("boom");
  });
});
