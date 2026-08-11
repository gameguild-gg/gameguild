import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("../../../lib/k8s", () => ({
  k8sCustom: {
    listNamespacedCustomObject: vi.fn(),
  },
}));

import { GET } from "./route";
import { k8sCustom } from "../../../lib/k8s";

describe("longhorn route", () => {
  beforeEach(() => vi.resetAllMocks());

  it("returns mapped volumes and nodes on success", async () => {
    const volumes = {
      items: [
        {
          metadata: { name: "vol-a", namespace: "longhorn-system" },
          spec: { numberOfReplicas: 3, size: "10737418240" },
          status: {
            state: "attached",
            robustness: "healthy",
            scheduled: true,
            nodeID: "mario",
          },
        },
        {
          metadata: { name: "vol-b", namespace: "longhorn-system" },
          spec: { numberOfReplicas: 3, size: "21474836480" },
          status: { state: "detached", robustness: "unknown" },
        },
      ],
    };
    const nodes = {
      items: [
        {
          metadata: { name: "mario" },
          status: {
            diskStatus: {
              "default-disk": {
                storageAvailable: 1000,
                storageMaximum: 2000,
              },
            },
          },
        },
        { metadata: { name: "luigi" }, status: { diskStatus: {} } },
      ],
    };
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockImplementation(
      (param: { plural: string }) =>
        Promise.resolve(param.plural === "volumes" ? volumes : nodes),
    );

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body.volumes).toHaveLength(2);
    expect(body.volumes[0]).toMatchObject({
      name: "vol-a",
      state: "attached",
      robustness: "healthy",
      nodeID: "mario",
    });
    expect(body.nodes).toHaveLength(2);
    expect(body.nodes[0].diskUsage[0]).toMatchObject({
      disk: "default-disk",
      storageAvailable: 1000,
      storageMaximum: 2000,
    });
  });

  it("returns 500 with error message on failure", async () => {
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error("boom"),
    );

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toContain("boom");
  });
});
