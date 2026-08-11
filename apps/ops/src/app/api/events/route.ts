import { NextResponse } from "next/server";
import { k8sCore } from "../../../lib/k8s";

export const dynamic = "force-dynamic";

interface EventRow {
  name: string;
  namespace: string;
  reason: string;
  message: string;
  lastTimestamp: string;
  count: number;
}

const MAX_EVENTS = 50;

export async function GET() {
  try {
    const eventList = await k8sCore.listEventForAllNamespaces({
      fieldSelector: "type=Warning",
    });

    const rows: EventRow[] = (eventList.items ?? [])
      .map((event) => {
        const ts = event.lastTimestamp;
        return {
          name: event.metadata?.name ?? "",
          namespace: event.metadata?.namespace ?? "",
          reason: event.reason ?? "",
          message: event.message ?? "",
          lastTimestamp: ts ? ts.toISOString() : "",
          count: event.count ?? 0,
        };
      })
      .filter((row) => row.lastTimestamp)
      .sort((a, b) => b.lastTimestamp.localeCompare(a.lastTimestamp))
      .slice(0, MAX_EVENTS);

    return NextResponse.json(rows);
  } catch (err) {
    const message = err instanceof Error ? err.message : "k8s request failed";
    return NextResponse.json({ error: message }, { status: 500 });
  }
}
