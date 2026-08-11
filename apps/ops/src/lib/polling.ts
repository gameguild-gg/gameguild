"use client";

import { useQuery } from "@tanstack/react-query";

export const POLL_INTERVAL = 15000;

// ponytail: 10 explicit hooks, no factory. Each is tree-shakeable and the
// queryKey is self-documenting at the call site.

export function useNodes() {
  return useQuery({
    queryKey: ["nodes"],
    queryFn: () => fetch("/api/nodes").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function usePods() {
  return useQuery({
    queryKey: ["pods"],
    queryFn: () => fetch("/api/pods").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useEvents() {
  return useQuery({
    queryKey: ["events"],
    queryFn: () => fetch("/api/events").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useLonghorn() {
  return useQuery({
    queryKey: ["longhorn"],
    queryFn: () => fetch("/api/longhorn").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useCnpg() {
  return useQuery({
    queryKey: ["cnpg"],
    queryFn: () => fetch("/api/cnpg").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useGarage() {
  return useQuery({
    queryKey: ["garage"],
    queryFn: () => fetch("/api/garage").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useAlerts() {
  return useQuery({
    queryKey: ["alerts"],
    queryFn: () => fetch("/api/alerts").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useServices() {
  return useQuery({
    queryKey: ["services"],
    queryFn: () => fetch("/api/services").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function useVelero() {
  return useQuery({
    queryKey: ["velero"],
    queryFn: () => fetch("/api/velero").then((r) => r.json()),
    refetchInterval: POLL_INTERVAL,
  });
}

export function usePrometheus(query: string, options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: ["prometheus", query],
    queryFn: async () => {
      const res = await fetch("/api/prometheus", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ query }),
      });
      return res.json();
    },
    enabled: options?.enabled,
    refetchInterval: POLL_INTERVAL,
  });
}
