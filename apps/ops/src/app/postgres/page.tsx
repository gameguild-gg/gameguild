"use client";

import { useCnpg } from "@/lib/polling";
import { Alert, AlertDescription } from "@game-guild/ui/components/alert";
import { Badge } from "@game-guild/ui/components/badge";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@game-guild/ui/components/card";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

interface CnpgCluster {
  name?: string;
  instances?: number;
  readyInstances?: number;
  phase?: string;
  currentPrimary?: string;
}
interface CnpgInstance {
  name?: string;
  role?: string;
  ready?: boolean;
  node?: string;
}

export default function PostgresPage() {
  const { data, isLoading, error } = useCnpg();

  if (isLoading) return <p>Loading…</p>;
  if (error)
    return (
      <Alert variant="destructive">
        <AlertDescription>
          Failed to load CNPG data. Will retry shortly.
        </AlertDescription>
      </Alert>
    );

  const clusters: CnpgCluster[] = Array.isArray(data?.clusters) ? data.clusters : [];
  const instances: CnpgInstance[] = Array.isArray(data?.instances) ? data.instances : [];
  const cluster = clusters[0];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Postgres (CNPG)</h1>

      {cluster ? (
        <Card className="cnpg-cluster-card">
          <CardHeader>
            <CardTitle>{cluster.name}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <div className="flex items-center gap-3">
              <Badge>{cluster.phase ?? "unknown"}</Badge>
              <span className="text-2xl font-semibold">
                {cluster.readyInstances ?? 0}/{cluster.instances ?? 0}
              </span>
              <span className="text-sm text-muted-foreground">
                instances ready
              </span>
            </div>
            <div className="text-sm">
              Primary:{" "}
              <span className="font-medium">
                {cluster.currentPrimary ?? "—"}
              </span>
            </div>
          </CardContent>
        </Card>
      ) : (
        <Alert>
          <AlertDescription>No CNPG clusters</AlertDescription>
        </Alert>
      )}

      <section>
        <h2 className="mb-2 text-lg font-medium">Instances</h2>
        {instances.length === 0 ? (
          <Alert>
            <AlertDescription>No instances</AlertDescription>
          </Alert>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Pod</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Node</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {instances.map((p) => (
                <TableRow key={p.name}>
                  <TableCell>{p.name}</TableCell>
                  <TableCell>
                    <Badge
                      variant={p.role === "primary" ? "default" : "secondary"}
                    >
                      {p.role ?? "unknown"}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Badge
                      className={
                        p.ready
                          ? "bg-green-500 text-white"
                          : "bg-red-500 text-white"
                      }
                    >
                      {p.ready ? "Ready" : "NotReady"}
                    </Badge>
                  </TableCell>
                  <TableCell>{p.node}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>
    </div>
  );
}
