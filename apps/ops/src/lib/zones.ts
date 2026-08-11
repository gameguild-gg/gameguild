// Cluster hostname → zone fallback. Used by nodes/route when a node lacks the
// `topology.kubernetes.io/zone` label. Keep in sync with the actual hostnames
// assigned in the cluster bootstrap.
const ZONE_MAP: Record<string, string> = {
  bowser: "home",
  bowsette: "home",
  rosalina: "home",
  luigi: "champlain",
  yoshi: "champlain",
  mario: "champlain",
  toad: "champlain",
  wario: "champlain",
  waluigi: "champlain",
  oracle: "cloud",
};

export function hostnameToZone(hostname: string): string {
  return ZONE_MAP[hostname.toLowerCase()] ?? "unknown";
}
