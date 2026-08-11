import { NextResponse } from "next/server";
import { k8sCustom } from "../../../lib/k8s";

export const dynamic = "force-dynamic";

interface GarageNode {
  metadata?: { name?: string };
  spec?: { zone?: string };
  hostname?: string;
  address?: string;
  port?: number;
}
interface CustomResourceList<T> {
  items?: T[];
}

export async function GET() {
  try {
    const resp = (await k8sCustom.listNamespacedCustomObject({
      group: "deuxfleurs.fr",
      version: "v1",
      namespace: "garage",
      plural: "garagenodes",
    })) as CustomResourceList<GarageNode>;

    const nodes = (resp.items ?? []).map((n) => ({
      nodeId: n.metadata?.name,
      hostname: n.hostname,
      address: n.address,
      port: n.port,
      zone: n.spec?.zone,
    }));

    return NextResponse.json(nodes);
  } catch (e) {
    return NextResponse.json({ error: String(e) }, { status: 500 });
  }
}
