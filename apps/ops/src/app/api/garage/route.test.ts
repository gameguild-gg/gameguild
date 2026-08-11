import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("../../../lib/k8s", () => ({
  k8sCustom: {
    listNamespacedCustomObject: vi.fn(),
  },
}));

import { GET } from "./route";
import { k8sCustom } from "../../../lib/k8s";

describe("garage route", () => {
  beforeEach(() => vi.resetAllMocks());

  it("returns mapped garage nodes on success", async () => {
    const resp = {
      items: [
        {
          metadata: { name: "node-1" },
          spec: { zone: "dc-1" },
          hostname: "storage-1",
          address: "10.0.0.1",
          port: 3901,
        },
        {
          metadata: { name: "node-2" },
          spec: { zone: "dc-2" },
          hostname: "storage-2",
          address: "10.0.0.2",
          port: 3901,
        },
      ],
    };
    (k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>).mockResolvedValue(
      resp,
    );

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body).toHaveLength(2);
    expect(body[0]).toMatchObject({
      nodeId: "node-1",
      hostname: "storage-1",
      address: "10.0.0.1",
      port: 3901,
      zone: "dc-1",
    });
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
