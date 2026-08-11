// Thin Prometheus helper. Used by the nodes route's flannel check and reused
// by T7's metrics route. Stdlib fetch only — no axios/got.
export const PROMETHEUS_URL =
  process.env.PROMETHEUS_URL ??
  "http://kube-prometheus-stack-prometheus.monitoring:9090";

export interface PrometheusQueryResult {
  status: string;
  data: {
    resultType: string;
    result: Array<{
      metric: Record<string, string>;
      value: [number, string];
    }>;
  };
}

export async function prometheusQuery(
  query: string,
  signal?: AbortSignal,
): Promise<PrometheusQueryResult> {
  const url = `${PROMETHEUS_URL}/api/v1/query?query=${encodeURIComponent(query)}`;
  const res = await fetch(url, { cache: "no-store", signal });
  if (!res.ok) {
    throw new Error(`Prometheus ${res.status}: ${await res.text()}`);
  }
  return (await res.json()) as PrometheusQueryResult;
}
