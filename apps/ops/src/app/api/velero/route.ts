import { NextResponse } from "next/server";
import { k8sCustom } from "../../../lib/k8s";

type VeleroSchedule = {
  metadata: { name: string };
  spec: { schedule: string };
  status?: {
    lastBackup?: string;
    lastBackupTimestamp?: string;
    phase?: string;
  };
};

type VeleroBackup = {
  metadata: { name: string; namespace: string; creationTimestamp: string };
  spec: { storageLocation?: string };
  status?: {
    phase?: string;
    expiration?: string;
    completionTimestamp?: string;
    warnings?: number;
    errors?: number;
  };
};

type VeleroList<T> = { items: T[] };

export async function GET(): Promise<Response> {
  try {
    const [schedulesRes, backupsRes] = await Promise.all([
      k8sCustom.listNamespacedCustomObject({
        group: "velero.io",
        version: "v1",
        namespace: "velero",
        plural: "schedules",
      }),
      k8sCustom.listNamespacedCustomObject({
        group: "velero.io",
        version: "v1",
        namespace: "velero",
        plural: "backups",
      }),
    ]);

    const schedulesList = (schedulesRes as VeleroList<VeleroSchedule>).items ?? [];
    const backupsList = (backupsRes as VeleroList<VeleroBackup>).items ?? [];

    const schedules = schedulesList.map((s) => ({
      name: s.metadata.name,
      schedule: s.spec.schedule,
      lastBackup: s.status?.lastBackup,
      lastBackupTimestamp: s.status?.lastBackupTimestamp,
      phase: s.status?.phase,
    }));

    const sortedBackups = [...backupsList].sort(
      (a, b) =>
        new Date(b.metadata.creationTimestamp).getTime() -
        new Date(a.metadata.creationTimestamp).getTime(),
    );
    const lastBackups = sortedBackups.slice(0, 10).map((b) => ({
      name: b.metadata.name,
      namespace: b.metadata.namespace,
      status: {
        phase: b.status?.phase,
        expiration: b.status?.expiration,
        completionTimestamp: b.status?.completionTimestamp,
        warnings: b.status?.warnings,
        errors: b.status?.errors,
      },
      storageLocation: b.spec.storageLocation,
    }));

    return NextResponse.json({ schedules, lastBackups });
  } catch (err) {
    return NextResponse.json(
      { error: err instanceof Error ? err.message : "velero list failed" },
      { status: 500 },
    );
  }
}
