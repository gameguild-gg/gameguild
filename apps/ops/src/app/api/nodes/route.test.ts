import { beforeEach, describe, expect, it, vi } from "vitest";

const mocks = vi.hoisted(() => {
  return {
    listNode: vi.fn(),
    fetch: vi.fn(),
  };
});

vi.mock("../../../lib/k8s", () => ({
  k8sCore: { listNode: mocks.listNode },
  k8sApps: {},
  k8sCustom: {},
}));

vi.mock("../../../lib/prometheus", () => ({
  PROMETHEUS_URL: "http://stub:9090",
  prometheusQuery: mocks.fetch,
}));

import { GET } from "./route";

function promOk(result: unknown) {
  mocks.fetch.mockResolvedValueOnce({ status: "success", data: { resultType: "vector", result } });
}

function flannelResult(nodeNames: string[]) {
  return nodeNames.map((node) => ({
    metric: { node, device: "flannel.1" },
    value: [Date.now() / 1000, "1"],
  }));
}

beforeEach(() => {
  mocks.listNode.mockReset();
  mocks.fetch.mockReset();
});

describe("GET /api/nodes", () => {
  it("maps nodes with zone, role, ready, flannel", async () => {
    mocks.listNode.mockResolvedValueOnce({
      items: [
        {
          metadata: {
            name: "bowser",
            labels: {
              "kubernetes.io/hostname": "bowser",
              "node-role.kubernetes.io/control-plane": "",
            },
          },
          status: {
            conditions: [{ type: "Ready", status: "True" }],
          },
        },
        {
          metadata: {
            name: "luigi",
            labels: {
              "topology.kubernetes.io/zone": "champlain-label",
            },
          },
          status: {
            conditions: [{ type: "Ready", status: "False" }],
          },
        },
      ],
    });
    promOk(flannelResult(["bowser"]));

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body).toEqual([
      {
        name: "bowser",
        zone: "home",
        role: "control-plane",
        ready: true,
        flannelHealthy: true,
      },
      {
        name: "luigi",
        zone: "champlain-label",
        role: "agent",
        ready: false,
        flannelHealthy: false,
      },
    ]);
  });

  it("falls back hostname → zone when no topology label", async () => {
    mocks.listNode.mockResolvedValueOnce({
      items: [
        {
          metadata: { name: "oracle", labels: {} },
          status: { conditions: [{ type: "Ready", status: "True" }] },
        },
      ],
    });
    promOk([]);

    const res = await GET();
    const body = await res.json();
    expect(body[0].zone).toBe("cloud");
    expect(body[0].flannelHealthy).toBe(false);
  });

  it("returns 500 with { error } when listNode throws", async () => {
    mocks.listNode.mockRejectedValueOnce(new Error("boom"));
    promOk([]);

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toBe("boom");
  });

  it("marks flannelHealthy false for all when Prometheus errors", async () => {
    mocks.listNode.mockResolvedValueOnce({
      items: [
        {
          metadata: { name: "bowser", labels: {} },
          status: { conditions: [{ type: "Ready", status: "True" }] },
        },
      ],
    });
    mocks.fetch.mockRejectedValueOnce(new Error("prom down"));

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body[0].flannelHealthy).toBe(false);
  });
});
