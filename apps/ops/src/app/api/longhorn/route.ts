import { NextResponse } from "next/server";
import { k8sCustom } from "../../../lib/k8s";

export const dynamic = "force-dynamic";

interface LonghornVolume {
  metadata?: { name?: string; namespace?: string };
  spec?: { numberOfReplicas?: number; size?: string };
  status?: {
    state?: string;
    robustness?: string;
    scheduled?: boolean;
    nodeID?: string;
  };
}
interface LonghornNode {
  metadata?: { name?: string };
  status?: {
    diskStatus?: Record<
      string,
      { storageAvailable?: number; storageMaximum?: number }
    >;
  };
}
interface CustomResourceList<T> {
  items?: T[];
}

export async function GET() {
  try {
    const [volumesResp, nodesResp] = await Promise.all([
      k8sCustom.listNamespacedCustomObject({
        group: "longhorn.io",
        version: "v1beta2",
        namespace: "longhorn-system",
        plural: "volumes",
      }),
      k8sCustom.listNamespacedCustomObject({
        group: "longhorn.io",
        version: "v1beta2",
        namespace: "longhorn-system",
        plural: "nodes",
      }),
    ]);

    const volumes = (
      (volumesResp as CustomResourceList<LonghornVolume>).items ?? []
    ).map((v) => ({
      name: v.metadata?.name,
      namespace: v.metadata?.namespace,
      state: v.status?.state,
      robustness: v.status?.robustness,
      size: v.spec?.size,
      numberOfReplicas: v.spec?.numberOfReplicas,
      scheduled: v.status?.scheduled,
      nodeID: v.status?.nodeID,
    }));

    const nodes = (
      (nodesResp as CustomResourceList<LonghornNode>).items ?? []
    ).map((n) => {
      const diskStatus = n.status?.diskStatus ?? {};
      const diskUsage = Object.entries(diskStatus).map(([disk, s]) => ({
        disk,
        storageAvailable: s.storageAvailable,
        storageMaximum: s.storageMaximum,
      }));
      return { name: n.metadata?.name, diskUsage };
    });

    return NextResponse.json({ volumes, nodes });
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
}
