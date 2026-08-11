"use client";

import { useQuery } from "@tanstack/react-query";

export const POLL_INTERVAL = 15000;

// ponytail: 10 explicit hooks, no factory. Each is tree-shakeable and the
// queryKey is self-documenting at the call site.
//
// Every queryFn checks res.ok before parsing JSON. Without this, a 500 with
// body {error:"..."} resolves successfully and TanStack Query stores that
// object as `data` — pages then call .map() on it and crash. Throwing on
// non-2xx routes the response into `error` so the page's error branch renders.

export function useNodes() {
  return useQuery({
    queryKey: ["nodes"],
    queryFn: async () => {
      const res = await fetch("/api/nodes");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function usePods() {
  return useQuery({
    queryKey: ["pods"],
    queryFn: async () => {
      const res = await fetch("/api/pods");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useEvents() {
  return useQuery({
    queryKey: ["events"],
    queryFn: async () => {
      const res = await fetch("/api/events");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useLonghorn() {
  return useQuery({
    queryKey: ["longhorn"],
    queryFn: async () => {
      const res = await fetch("/api/longhorn");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useCnpg() {
  return useQuery({
    queryKey: ["cnpg"],
    queryFn: async () => {
      const res = await fetch("/api/cnpg");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useGarage() {
  return useQuery({
    queryKey: ["garage"],
    queryFn: async () => {
      const res = await fetch("/api/garage");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useAlerts() {
  return useQuery({
    queryKey: ["alerts"],
    queryFn: async () => {
      const res = await fetch("/api/alerts");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useServices() {
  return useQuery({
    queryKey: ["services"],
    queryFn: async () => {
      const res = await fetch("/api/services");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    refetchInterval: POLL_INTERVAL,
  });
}

export function useVelero() {
  return useQuery({
    queryKey: ["velero"],
    queryFn: async () => {
      const res = await fetch("/api/velero");
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
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
      if (!res.ok) throw new Error(`API ${res.status}`);
      return res.json();
    },
    enabled: options?.enabled,
    refetchInterval: POLL_INTERVAL,
  });
}
