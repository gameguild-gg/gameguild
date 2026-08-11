import { NextResponse } from "next/server";
import { k8sCore, k8sCustom } from "../../../lib/k8s";
import { hostnameToZone } from "../../../lib/zones";

export const dynamic = "force-dynamic";

interface GarageNode {
  metadata?: { name?: string };
  spec?: {
    hostname?: string;
    address?: string;
    port?: number;
  };
}
interface CustomResourceList<T> {
  items?: T[];
}
interface Pod {
  metadata?: { name?: string };
  spec?: { nodeName?: string };
}
interface PodList {
  items?: Pod[];
}

export async function GET() {
  try {
    const [resp, podList] = await Promise.all([
      k8sCustom.listNamespacedCustomObject({
        group: "deuxfleurs.fr",
        version: "v1",
        namespace: "garage",
        plural: "garagenodes",
      }) as Promise<CustomResourceList<GarageNode>>,
      k8sCore.listNamespacedPod({
        namespace: "garage",
        labelSelector: "app.kubernetes.io/name=garage",
      }) as Promise<PodList>,
    ]);

    // CRD has no zone field; derive it from pod → node → node hostname.
    const podZone = new Map<string, string>();
    for (const pod of podList.items ?? []) {
      const nodeName = pod.spec?.nodeName;
      const podName = pod.metadata?.name;
      if (nodeName && podName) {
        podZone.set(podName, hostnameToZone(nodeName));
      }
    }

    const nodes = (resp.items ?? []).map((n) => {
      const hostname = n.spec?.hostname;
      return {
        nodeId: n.metadata?.name,
        hostname,
        address: n.spec?.address,
        port: n.spec?.port,
        zone: hostname ? (podZone.get(hostname) ?? "unknown") : "unknown",
      };
    });

    return NextResponse.json(nodes);
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
}
