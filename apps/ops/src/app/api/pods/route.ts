import { NextResponse } from "next/server";
import { k8sCore } from "../../../lib/k8s";

interface PodRow {
  name: string;
  namespace: string;
  status: string;
  ready: string;
  restarts: number;
  age: number;
}

export async function GET() {
  try {
    const podList = await k8sCore.listPodForAllNamespaces();
    const now = Date.now();
    const byNode: Record<string, PodRow[]> = {};

    for (const pod of podList.items ?? []) {
      const nodeName = pod.spec?.nodeName;
      if (!nodeName) continue;

      const statuses = pod.status?.containerStatuses ?? [];
      const readyCount = statuses.filter((s) => s.ready).length;
      const restarts = statuses.reduce((sum, s) => sum + (s.restartCount ?? 0), 0);
      const created = pod.metadata?.creationTimestamp;
      const age = created ? now - created.getTime() : 0;

      (byNode[nodeName] ??= []).push({
        name: pod.metadata?.name ?? "",
        namespace: pod.metadata?.namespace ?? "",
        status: pod.status?.phase ?? "Unknown",
        ready: statuses.length > 0 ? `${readyCount}/${statuses.length}` : "0/0",
        restarts,
        age,
      });
    }

    return NextResponse.json({ nodes: byNode });
  } catch (err) {
    const message = err instanceof Error ? err.message : "k8s request failed";
    return NextResponse.json({ error: message }, { status: 500 });
  }
}
