import { NextResponse } from "next/server";
import { k8sCore, k8sCustom } from "../../../lib/k8s";

export const dynamic = "force-dynamic";

interface CnpgCluster {
  metadata?: { name?: string };
  status?: {
    instances?: number;
    readyInstances?: number;
    phase?: string;
    currentPrimary?: string;
  };
}
interface CustomResourceList<T> {
  items?: T[];
}
interface V1PodList {
  items?: {
    metadata?: { name?: string; labels?: Record<string, string> };
    spec?: { nodeName?: string };
    status?: {
      conditions?: { type?: string; status?: string }[];
    };
  }[];
}

export async function GET() {
  try {
    const [clustersResp, podsResp] = await Promise.all([
      k8sCustom.listNamespacedCustomObject({
        group: "postgresql.cnpg.io",
        version: "v1",
        namespace: "gameguild",
        plural: "clusters",
      }),
      k8sCore.listNamespacedPod({
        namespace: "gameguild",
        labelSelector: "cnpg.io/cluster=gameguild-pg",
      }),
    ]);

    const clusters = (
      (clustersResp as CustomResourceList<CnpgCluster>).items ?? []
    ).map((c) => ({
      name: c.metadata?.name,
      instances: c.status?.instances,
      readyInstances: c.status?.readyInstances,
      phase: c.status?.phase,
      currentPrimary: c.status?.currentPrimary,
    }));

    const podList = podsResp as V1PodList;
    const instances = (podList.items ?? []).map((p) => {
      const readyCond = (p.status?.conditions ?? []).find(
        (cond) => cond.type === "Ready",
      );
      return {
        name: p.metadata?.name,
        role: p.metadata?.labels?.["cnpg.io/instanceRole"] ?? "unknown",
        ready: readyCond?.status === "True",
        node: p.spec?.nodeName,
      };
    });

    return NextResponse.json({ clusters, instances });
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
}
