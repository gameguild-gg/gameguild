"use client";

import { Loader2 } from "lucide-react";
import { useMemo, useState } from "react";

import { Alert, AlertDescription, AlertTitle } from "@game-guild/ui/components/alert";
import { Badge } from "@game-guild/ui/components/badge";
import { Input } from "@game-guild/ui/components/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@game-guild/ui/components/select";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

import { usePods } from "@/lib/polling";

type PodRow = {
  name: string;
  namespace: string;
  status: string;
  ready: string;
  restarts: number;
  age: number;
};

const ALL_NS = "__all__";

function statusTint(s: string): string {
  if (s === "Running") return "border-green-500 text-green-500";
  if (s === "Pending" || s === "ContainerCreating") {
    return "border-yellow-500 text-yellow-500";
  }
  // Failed / Error / CrashLoopBackOff / Unknown
  return "border-red-500 text-red-500";
}

// age from /api/pods is a millisecond delta from Date.now(). Format to a
// compact human string; K8s conventions: s / m / h / d.
function formatAge(ms: number): string {
  if (!Number.isFinite(ms) || ms <= 0) return "—";
  const s = Math.floor(ms / 1000);
  if (s < 60) return `${s}s`;
  const m = Math.floor(s / 60);
  if (m < 60) return `${m}m`;
  const h = Math.floor(m / 60);
  if (h < 24) return `${h}h`;
  return `${Math.floor(h / 24)}d`;
}

export default function PodsPage() {
  const pods = usePods();
  const [namespace, setNamespace] = useState<string>(ALL_NS);
  const [search, setSearch] = useState("");

  // Flatten { [node]: PodRow[] } into one list tagged with the host node.
  const flattened = useMemo<Array<PodRow & { node: string }>>(() => {
    const nodes =
      pods.data && typeof pods.data === "object" && pods.data.nodes && typeof pods.data.nodes === "object"
        ? (pods.data.nodes as Record<string, PodRow[]>)
        : undefined;
    if (!nodes) return [];
    return Object.entries(nodes).flatMap(([node, list]) =>
      (list ?? []).map((p) => ({ ...p, node })),
    );
  }, [pods.data]);

  const namespaces = useMemo(() => {
    const set = new Set<string>();
    for (const p of flattened) set.add(p.namespace);
    return Array.from(set).sort();
  }, [flattened]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    return flattened.filter((p) => {
      if (namespace !== ALL_NS && p.namespace !== namespace) return false;
      if (q && !p.name.toLowerCase().includes(q)) return false;
      return true;
    });
  }, [flattened, namespace, search]);

  if (pods.isLoading) {
    return (
      <div className="flex items-center gap-2 text-muted-foreground">
        <Loader2 className="size-4 animate-spin" />
        Loading pods…
      </div>
    );
  }

  if (pods.error) {
    return (
      <Alert variant="destructive">
        <AlertTitle>Failed to load pods</AlertTitle>
        <AlertDescription>
          Retrying automatically.{" "}
          {pods.error instanceof Error ? pods.error.message : String(pods.error)}
        </AlertDescription>
      </Alert>
    );
  }

  return (
    <div className="pod-table space-y-4">
      <h1 className="text-xl font-semibold">Pods</h1>
      <div className="flex flex-wrap items-center gap-2">
        <Select value={namespace} onValueChange={setNamespace}>
          <SelectTrigger className="w-56">
            <SelectValue placeholder="Namespace" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value={ALL_NS}>All namespaces</SelectItem>
            {namespaces.map((ns) => (
              <SelectItem key={ns} value={ns}>
                {ns}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        <Input
          type="search"
          placeholder="Search pod name…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="max-w-xs"
        />
        <span className="text-xs text-muted-foreground">
          {filtered.length} / {flattened.length} pods
        </span>
      </div>
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Node</TableHead>
            <TableHead>Namespace</TableHead>
            <TableHead>Pod</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Ready</TableHead>
            <TableHead>Restarts</TableHead>
            <TableHead>Age</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {filtered.map((p) => (
            <TableRow key={`${p.node}/${p.namespace}/${p.name}`}>
              <TableCell className="font-mono text-xs">{p.node}</TableCell>
              <TableCell className="text-xs">{p.namespace}</TableCell>
              <TableCell className="font-mono text-xs">{p.name}</TableCell>
              <TableCell>
                <Badge variant="outline" className={statusTint(p.status)}>
                  {p.status}
                </Badge>
              </TableCell>
              <TableCell className="text-xs">{p.ready}</TableCell>
              <TableCell className="text-xs">{p.restarts}</TableCell>
              <TableCell className="text-xs">{formatAge(p.age)}</TableCell>
            </TableRow>
          ))}
          {filtered.length === 0 && (
            <TableRow>
              <TableCell colSpan={7} className="text-muted-foreground">
                No pods match the current filters.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
