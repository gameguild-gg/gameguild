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

describe("garage route", () => {
  beforeEach(() => vi.resetAllMocks());

  it("derives zone from garage pod's node hostname", async () => {
    const crdResp = {
      items: [
        {
          metadata: { name: "node-1" },
          spec: {
            hostname: "garage-qg5sc",
            address: "10.0.0.1",
            port: 3901,
          },
        },
        {
          metadata: { name: "node-2" },
          spec: {
            hostname: "garage-abcde",
            address: "10.0.0.2",
            port: 3901,
          },
        },
      ],
    };
    const podList = {
      items: [
        { metadata: { name: "garage-qg5sc" }, spec: { nodeName: "mario" } },
        { metadata: { name: "garage-abcde" }, spec: { nodeName: "oracle" } },
      ],
    };
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockResolvedValue(
      crdResp,
    );
    (k8sCore.listNamespacedPod as ReturnType<typeof vi.fn>).mockResolvedValue(
      podList,
    );

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body).toHaveLength(2);
    expect(body[0]).toMatchObject({
      nodeId: "node-1",
      hostname: "garage-qg5sc",
      address: "10.0.0.1",
      port: 3901,
      zone: "champlain",
    });
    expect(body[1]).toMatchObject({
      nodeId: "node-2",
      hostname: "garage-abcde",
      address: "10.0.0.2",
      port: 3901,
      zone: "cloud",
    });
  });

  it("returns zone 'unknown' when hostname has no matching pod", async () => {
    const crdResp = {
      items: [
        {
          metadata: { name: "node-1" },
          spec: {
            hostname: "garage-missing",
            address: "10.0.0.1",
            port: 3901,
          },
        },
      ],
    };
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockResolvedValue(
      crdResp,
    );
    (k8sCore.listNamespacedPod as ReturnType<typeof vi.fn>).mockResolvedValue({
      items: [],
    });

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body[0].zone).toBe("unknown");
  });

  it("returns 500 with error message on failure", async () => {
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockRejectedValue(
      new Error("forbidden"),
    );

    const res = await GET();
    expect(res.status).toBe(500);
    const body = await res.json();
    expect(body.error).toContain("forbidden");
  });
});
