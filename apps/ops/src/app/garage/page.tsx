"use client";

import { useGarage } from "@/lib/polling";
import {
  Alert,
  AlertDescription,
  AlertTitle,
} from "@game-guild/ui/components/alert";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

interface GarageNode {
  nodeId?: string;
  hostname?: string;
  address?: string;
  port?: number;
  zone?: string;
}

export default function GaragePage() {
  const { data, isLoading, error } = useGarage();

  if (isLoading) return <p>Loading…</p>;
  if (error)
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Failed to load Garage data. Will retry shortly.
        </AlertDescription>
      </Alert>
    );

  const nodes: GarageNode[] = Array.isArray(data) ? data : [];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Garage</h1>

      <Alert>
        <AlertTitle>Note</AlertTitle>
        <AlertDescription>
          Connectivity status requires Garage admin API (not configured). CRD
          lists registered nodes only.
        </AlertDescription>
      </Alert>

      <h2 className="text-lg font-medium">Registered Nodes: {nodes.length}</h2>

      {nodes.length === 0 ? (
        <Alert>
          <AlertDescription>No registered nodes</AlertDescription>
        </Alert>
      ) : (
        <Table className="garage-node-table">
          <TableHeader>
            <TableRow>
              <TableHead>Node ID</TableHead>
              <TableHead>Hostname</TableHead>
              <TableHead>Address</TableHead>
              <TableHead>Port</TableHead>
              <TableHead>Zone</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {nodes.map((n) => (
              <TableRow key={n.nodeId}>
                <TableCell>{n.nodeId}</TableCell>
                <TableCell>{n.hostname}</TableCell>
                <TableCell>{n.address}</TableCell>
                <TableCell>{n.port}</TableCell>
                <TableCell>{n.zone}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </div>
  );
}
