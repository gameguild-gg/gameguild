import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("../../../lib/k8s", () => ({
  k8sCustom: {
    listNamespacedCustomObject: vi.fn(),
  },
  k8sCore: {
    listNamespacedPod: vi.fn(),
  },
}));

import { GET } from "./route";
import { k8sCustom, k8sCore } from "../../../lib/k8s";

describe("cnpg route", () => {
  beforeEach(() => vi.resetAllMocks());

  it("returns mapped clusters and pod instances on success", async () => {
    const clusters = {
      items: [
        {
          metadata: { name: "gameguild-pg" },
          status: {
            instances: 3,
            readyInstances: 3,
            phase: "Cluster in healthy state",
            currentPrimary: "gameguild-pg-6",
          },
        },
      ],
    };
    const pods = {
      items: [
        {
          metadata: {
            name: "gameguild-pg-6",
            labels: { "cnpg.io/cluster": "gameguild-pg", role: "primary" },
          },
          spec: { nodeName: "mario" },
          status: { conditions: [{ type: "Ready", status: "True" }] },
        },
        {
          metadata: { name: "gameguild-pg-7", labels: { role: "replica" } },
          spec: { nodeName: "luigi" },
          status: { conditions: [{ type: "Ready", status: "True" }] },
        },
        {
          metadata: { name: "gameguild-pg-8", labels: { role: "replica" } },
          spec: { nodeName: "peach" },
          status: { conditions: [{ type: "Ready", status: "False" }] },
        },
      ],
    };
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockResolvedValue(
      clusters,
    );
    (k8sCore.listNamespacedPod as ReturnType<typeof vi.fn>).mockResolvedValue(pods);

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body.clusters).toHaveLength(1);
    expect(body.clusters[0]).toMatchObject({
      name: "gameguild-pg",
      instances: 3,
      readyInstances: 3,
      currentPrimary: "gameguild-pg-6",
    });
    expect(body.instances).toHaveLength(3);
    expect(body.instances[0]).toMatchObject({
      name: "gameguild-pg-6",
      ready: true,
      node: "mario",
    });
    expect(body.instances[2].ready).toBe(false);
  });

  it("returns 500 with error message on failure", async () => {
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error("api down"),
    );

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toContain("api down");
  });
});
