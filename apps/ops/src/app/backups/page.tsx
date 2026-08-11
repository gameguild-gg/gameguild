"use client";

import { AlertCircle } from "lucide-react";

import { Badge } from "@game-guild/ui/components/badge";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@game-guild/ui/components/table";

import { useVelero } from "@/lib/polling";

type Schedule = {
  name: string;
  schedule: string;
  lastBackup: string;
  lastBackupTimestamp: string;
  phase: string;
};

type Backup = {
  name: string;
  namespace: string;
  status: {
    phase: string;
    expiration: string;
    completionTimestamp: string;
    warnings: number;
    errors: number;
  };
  storageLocation: string;
};

type Velero = {
  schedules: Schedule[];
  lastBackups: Backup[];
};

// ponytail: lowercase-compare handles "Completed" / "completed" drift from API.
function phaseTone(phase: string): string {
  const p = phase.toLowerCase();
  if (p === "completed") return "bg-green-500 text-white";
  if (p === "partiallyfailed") return "bg-yellow-500 text-black";
  if (p === "failed") return "bg-red-500 text-white";
  return "bg-secondary text-secondary-foreground";
}

export default function BackupsPage() {
  const { data, isLoading, error } = useVelero();
  const velero = data as Velero | undefined;
  const schedules = velero?.schedules ?? [];
  const lastBackups = velero?.lastBackups ?? [];

  if (isLoading) {
    return <div className="text-muted-foreground">Loading backups…</div>;
  }
  if (error) {
    return (
      <div className="flex items-center gap-2 text-destructive">
        <AlertCircle className="size-4" />
        Failed to load Velero status.
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Backups</h1>

      <section className="space-y-2">
        <h2 className="text-lg font-medium">Schedules</h2>
        {schedules.length === 0 ? (
          <p className="text-muted-foreground">No backup schedules configured.</p>
        ) : (
          <Table className="backup-table">
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Schedule</TableHead>
                <TableHead>Last Backup</TableHead>
                <TableHead>Phase</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {schedules.map((s) => (
                <TableRow key={s.name}>
                  <TableCell className="font-medium">{s.name}</TableCell>
                  <TableCell className="font-mono text-xs text-muted-foreground">
                    {s.schedule}
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {s.lastBackupTimestamp || s.lastBackup || "—"}
                  </TableCell>
                  <TableCell>
                    <Badge className={phaseTone(s.phase)}>{s.phase}</Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>

      <section className="space-y-2">
        <h2 className="text-lg font-medium">Recent Backups</h2>
        {lastBackups.length === 0 ? (
          <p className="text-muted-foreground">No backup history available.</p>
        ) : (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Phase</TableHead>
                <TableHead>Completion</TableHead>
                <TableHead>Warnings</TableHead>
                <TableHead>Errors</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {lastBackups.map((b) => (
                <TableRow key={`${b.namespace}/${b.name}`}>
                  <TableCell className="font-medium">{b.name}</TableCell>
                  <TableCell>
                    <Badge className={phaseTone(b.status.phase)}>
                      {b.status.phase}
                    </Badge>
                  </TableCell>
                  <TableCell className="text-muted-foreground">
                    {b.status.completionTimestamp || "—"}
                  </TableCell>
                  <TableCell
                    className={
                      b.status.warnings > 0
                        ? "font-medium text-yellow-500"
                        : "text-muted-foreground"
                    }
                  >
                    {b.status.warnings}
                  </TableCell>
                  <TableCell
                    className={
                      b.status.errors > 0
                        ? "font-medium text-red-500"
                        : "text-muted-foreground"
                    }
                  >
                    {b.status.errors}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        )}
      </section>
    </div>
  );
}
