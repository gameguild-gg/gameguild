import { NextResponse } from "next/server";

const ALERTMANAGER_URL =
  "http://kube-prometheus-stack-alertmanager.monitoring:9093/api/v2/alerts";

type AlertmanagerAlert = {
  labels?: { alertname?: string; severity?: string; summary?: string };
  annotations?: { summary?: string };
  status?: { state?: string };
  activeAt?: string;
};

export async function GET(): Promise<Response> {
  try {
    const res = await fetch(ALERTMANAGER_URL, {
      cache: "no-store",
      signal: AbortSignal.timeout(5000),
    });
    const alerts = (await res.json()) as AlertmanagerAlert[];
    const mapped = alerts.map((a) => ({
      name: a.labels?.alertname ?? "",
      severity: a.labels?.severity ?? "",
      status: a.status?.state ?? "",
      activeAt: a.activeAt ?? "",
      summary: a.annotations?.summary ?? a.labels?.summary ?? "",
    }));
    return NextResponse.json(mapped);
  } catch (err) {
    return NextResponse.json(
      { error: err instanceof Error ? err.message : "alerts fetch failed" },
      { status: 500 },
    );
  }
}
