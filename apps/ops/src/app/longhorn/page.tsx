"use client";

import { useLonghorn } from "@/lib/polling";
import { Alert, AlertDescription } from "@game-guild/ui/components/alert";
import { Badge } from "@game-guild/ui/components/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

// ponytail: inline color map beats a StatusBadge component for 4 known values.
const ROBUSTNESS_CLASS: Record<string, string> = {
  healthy: "bg-green-500 text-white",
  degraded: "bg-yellow-500 text-black",
  faulted: "bg-red-500 text-white",
  unknown: "bg-gray-500 text-white",
};

function formatBytes(s?: string | number): string {
  const n = typeof s === "number" ? s : Number(s);
  if (!Number.isFinite(n) || n <= 0) return "—";
  return `${(n / 1e9).toFixed(1)} GB`;
}

interface LonghornVolume {
  name?: string;
  namespace?: string;
  state?: string;
  robustness?: string;
  size?: string;
  numberOfReplicas?: number;
  scheduled?: boolean;
  nodeID?: string;
}
interface DiskUsage {
  disk: string;
  storageAvailable?: number;
  storageMaximum?: number;
}
interface LonghornNode {
  name?: string;
  diskUsage?: DiskUsage[];
}

export default function LonghornPage() {
  const { data, isLoading, error } = useLonghorn();

  if (isLoading) return <p>Loading…</p>;
  if (error)
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Failed to load Longhorn data. Will retry shortly.
        </AlertDescription>
      </Alert>
    );

  const volumes: LonghornVolume[] = Array.isArray(data?.volumes) ? data.volumes : [];
  const nodes: LonghornNode[] = Array.isArray(data?.nodes) ? data.nodes : [];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Longhorn</h1>

      <section>
        <h2 className="mb-2 text-lg font-medium">Volumes</h2>
        {volumes.length === 0 ? (
          <Alert>
            <AlertDescription>No volumes</AlertDescription>
          </Alert>
        ) : (
          <Table className="volume-table">
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Namespace</TableHead>
                <TableHead>State</TableHead>
                <TableHead>Robustness</TableHead>
                <TableHead>Size</TableHead>
                <TableHead>Replicas</TableHead>
                <TableHead>Node</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {volumes.map((v) => (
                <TableRow key={`${v.namespace}/${v.name}`}>
                  <TableCell>{v.name}</TableCell>
                  <TableCell>{v.namespace}</TableCell>
                  <TableCell>{v.state}</TableCell>
                  <TableCell>
                    <Badge
                      className={ROBUSTNESS_CLASS[v.robustness ?? "unknown"]}
                    >
                      {v.robustness ?? "unknown"}
                    </Badge>
                  </TableCell>
                  <TableCell>{formatBytes(v.size)}</TableCell>
                  <TableCell>
                    {v.scheduled ? "✓" : "✗"}/{v.numberOfReplicas ?? 0}
                  </TableCell>
                  <TableCell>{v.nodeID}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>

      <section>
        <h2 className="mb-2 text-lg font-medium">Node Disk Usage</h2>
        {nodes.length === 0 ? (
          <Alert>
            <AlertDescription>No node data</AlertDescription>
          </Alert>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Node</TableHead>
                <TableHead>Disk Path</TableHead>
                <TableHead>Storage Available</TableHead>
                <TableHead>Storage Maximum</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {nodes.flatMap((n) =>
                (n.diskUsage ?? []).map((d) => (
                  <TableRow key={`${n.name}/${d.disk}`}>
                    <TableCell>{n.name}</TableCell>
                    <TableCell>{d.disk}</TableCell>
                    <TableCell>{formatBytes(d.storageAvailable)}</TableCell>
                    <TableCell>{formatBytes(d.storageMaximum)}</TableCell>
                  </TableRow>
                )),
              )}
            </TableBody>
          </Table>
        )}
      </section>
    </div>
  );
}
