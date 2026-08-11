import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("../../../lib/k8s", () => ({
  k8sCustom: {
    listNamespacedCustomObject: vi.fn(),
  },
}));

import { GET } from "./route";
import { k8sCustom } from "../../../lib/k8s";

const listFn = k8sCustom.listNamespacedCustomObject as ReturnType<typeof vi.fn>;

const makeSchedule = (
  name: string,
  schedule: string,
  phase = "Enabled",
  lastBackup = "2024-01-01T00:00:00Z",
) => ({
  metadata: { name },
  spec: { schedule },
  status: {
    phase,
    lastBackup,
    lastBackupTimestamp: lastBackup,
  },
});

const makeBackup = (
  name: string,
  ts: string,
  phase = "Completed",
) => ({
  metadata: { name, namespace: "velero", creationTimestamp: ts },
  spec: { storageLocation: "default" },
  status: {
    phase,
    expiration: "2024-02-01T00:00:00Z",
    completionTimestamp: ts,
    warnings: 0,
    errors: 0,
  },
});

describe("velero/route GET", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("lists schedules and sorts backups desc by creationTimestamp", async () => {
    listFn
      .mockResolvedValueOnce({
        items: [makeSchedule("daily", "0 1 * * *"), makeSchedule("hourly", "0 * * * *")],
      })
      .mockResolvedValueOnce({
        items: [
          makeBackup("b1", "2024-01-01T00:00:00Z"),
          makeBackup("b3", "2024-01-03T00:00:00Z"),
          makeBackup("b2", "2024-01-02T00:00:00Z"),
        ],
      });

    const res = await GET();
    expect(res.status).toBe(200);
    const body = await res.json();
    expect(body.schedules).toEqual([
      {
        name: "daily",
        schedule: "0 1 * * *",
        lastBackup: "2024-01-01T00:00:00Z",
        lastBackupTimestamp: "2024-01-01T00:00:00Z",
        phase: "Enabled",
      },
      {
        name: "hourly",
        schedule: "0 * * * *",
        lastBackup: "2024-01-01T00:00:00Z",
        lastBackupTimestamp: "2024-01-01T00:00:00Z",
        phase: "Enabled",
      },
    ]);
    expect(body.lastBackups.map((b: { name: string }) => b.name)).toEqual([
      "b3",
      "b2",
      "b1",
    ]);
  });

  it("slices to the 10 most recent backups", async () => {
    const items = Array.from({ length: 15 }, (_, i) => {
      const day = String(i + 1).padStart(2, "0");
      return makeBackup(`b${day}`, `2024-01-${day}T00:00:00Z`);
    });
    listFn.mockResolvedValueOnce({ items: [] }).mockResolvedValueOnce({ items });

    const body = await (await GET()).json();
    expect(body.lastBackups).toHaveLength(10);
    expect(body.lastBackups[0].name).toBe("b15");
    expect(body.lastBackups[9].name).toBe("b06");
  });

  it("returns 500 when k8s api throws", async () => {
    listFn.mockRejectedValue(new Error("forbidden"));
    const res = await GET();
    expect(res.status).toBe(500);
    expect((await res.json()).error).toContain("forbidden");
  });

  it("calls listNamespacedCustomObject with the velero CRD coordinates", async () => {
    listFn.mockResolvedValue({ items: [] });
    await GET();
    expect(listFn).toHaveBeenCalledWith({
      group: "velero.io",
      version: "v1",
      namespace: "velero",
      plural: "schedules",
    });
    expect(listFn).toHaveBeenCalledWith({
      group: "velero.io",
      version: "v1",
      namespace: "velero",
      plural: "backups",
    });
  });
});
