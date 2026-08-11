import { NextResponse } from "next/server";
import { k8sCore } from "../../../lib/k8s";
import { prometheusQuery } from "../../../lib/prometheus";
import { hostnameToZone } from "../../../lib/zones";

export const dynamic = "force-dynamic";

interface NodeRow {
  name: string;
  zone: string;
  role: "control-plane" | "agent";
  ready: boolean;
  flannelHealthy: boolean;
  podCount?: number;
}

// 3s ceiling on the flannel lookup so a slow/unreachable Prometheus does not
// stall the whole nodes response.
const FLANNEL_TIMEOUT_MS = 3_000;

async function fetchFlannelNodes(): Promise<Set<string>> {
  // node_network_info{device="flannel.1"} is present iff flannel.1 exists on
  // that host. metric.node is the k8s node name when present; otherwise fall
  // back to the hostname portion of metric.instance (host:port).
  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), FLANNEL_TIMEOUT_MS);
  try {
    const result = await prometheusQuery(
      'node_network_info{device="flannel.1"}',
      controller.signal,
    );
    const names = new Set<string>();
    for (const point of result.data?.result ?? []) {
      const node = point.metric.node;
      if (node) {
        names.add(node);
        continue;
      }
      const instance = point.metric.instance;
      if (instance) {
        const host = instance.split(":")[0];
        if (host) names.add(host);
      }
    }
    return names;
  } finally {
    clearTimeout(timer);
  }
}

export async function GET() {
  try {
    const [nodeList, flannelNodes] = await Promise.all([
      k8sCore.listNode(),
      fetchFlannelNodes().catch(() => new Set<string>()),
    ]);

    const rows: NodeRow[] = (nodeList.items ?? []).map((node) => {
      const labels = node.metadata?.labels ?? {};
      const name = node.metadata?.name ?? "";
      const conditions = node.status?.conditions ?? [];

      const zone =
        labels["topology.kubernetes.io/zone"] ??
        hostnameToZone(labels["kubernetes.io/hostname"] ?? name);
      const role: NodeRow["role"] =
        labels["node-role.kubernetes.io/control-plane"] !== undefined
          ? "control-plane"
          : "agent";
      const ready =
        conditions.find((c) => c.type === "Ready")?.status === "True";
      const flannelHealthy =
        flannelNodes.size === 0
          ? false
          : (node.status?.addresses ?? []).some((a) =>
              flannelNodes.has(a.address),
            );

      return { name, zone, role, ready, flannelHealthy };
    });

    return NextResponse.json(rows);
  } catch (err) {
    const message = err instanceof Error ? err.message : "k8s request failed";
    return NextResponse.json({ error: message }, { status: 500 });
  }
}
