"use client";

import { AlertCircle, CheckCircle } from "lucide-react";

import { Badge } from "@game-guild/ui/components/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

import { useAlerts } from "@/lib/polling";

type Severity = "critical" | "warning" | "info";

type Alert = {
  name: string;
  severity: Severity;
  status: "firing" | "pending";
  activeAt: string;
  summary: string;
};

// ponytail: inline severity rank — no enum, no map. critical < warning < info.
const SEVERITY_RANK: Record<Severity, number> = {
  critical: 0,
  warning: 1,
  info: 2,
};

const SEVERITY_BADGE: Record<Severity, string> = {
  critical: "bg-red-500 text-white",
  warning: "bg-yellow-500 text-black",
  info: "bg-blue-500 text-white",
};

function timeAgo(iso: string): string {
  const mins = Math.floor((Date.now() - new Date(iso).getTime()) / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.floor(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export default function AlertsPage() {
  const { data, isLoading, error } = useAlerts();
  const alerts = (data as Alert[] | undefined) ?? [];

  if (isLoading) {
    return <div className="text-muted-foreground">Loading alerts…</div>;
  }
  if (error) {
    return (
      <div className="flex items-center gap-2 text-destructive">
        <AlertCircle className="size-4" />
        Failed to load alerts.
      </div>
    );
  }

  const sorted = [...alerts].sort((a, b) => {
    const rank = SEVERITY_RANK[a.severity] - SEVERITY_RANK[b.severity];
    if (rank !== 0) return rank;
    return new Date(a.activeAt).getTime() - new Date(b.activeAt).getTime();
  });

  if (sorted.length === 0) {
    return (
      <div className="no-alerts flex flex-col items-center gap-3 py-20 text-muted-foreground">
        <CheckCircle className="size-10 text-green-500" />
        <p className="text-lg">No active alerts</p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-semibold">Alerts</h1>
      <Table className="alerts-table">
        <TableHeader>
          <TableRow>
            <TableHead>Alert Name</TableHead>
            <TableHead>Severity</TableHead>
            <TableHead>Status</TableHead>
            <TableHead>Active Since</TableHead>
            <TableHead>Summary</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {sorted.map((alert) => (
            <TableRow key={`${alert.name}-${alert.activeAt}`}>
              <TableCell className="font-medium">{alert.name}</TableCell>
              <TableCell>
                <Badge className={SEVERITY_BADGE[alert.severity]}>
                  {alert.severity}
                </Badge>
              </TableCell>
              <TableCell>
                <span className="inline-flex items-center gap-2">
                  <span
                    className={`inline-block size-2 rounded-full ${
                      alert.status === "firing"
                        ? "bg-red-500"
                        : "bg-yellow-500"
                    }`}
                  />
                  {alert.status}
                </span>
              </TableCell>
              <TableCell className="text-muted-foreground">
                {timeAgo(alert.activeAt)}
              </TableCell>
              <TableCell className="max-w-md truncate text-muted-foreground">
                {alert.summary}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
