"use client";

import {
  Activity,
  Bell,
  Box,
  Database,
  HardDrive,
  Loader2,
  Server,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";

import { Badge } from "@game-guild/ui/components/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@game-guild/ui/components/card";

import {
  useAlerts,
  useCnpg,
  useLonghorn,
  useNodes,
  usePods,
  useServices,
} from "@/lib/polling";

type Status = "green" | "yellow" | "red";

const DOT_CLASS: Record<Status, string> = {
  green: "bg-green-500",
  yellow: "bg-yellow-500",
  red: "bg-red-500",
};

const STATUS_LABEL: Record<Status, string> = {
  green: "Healthy",
  yellow: "Degraded",
  red: "Critical",
};

function StatusBadge({ status }: { status: Status }) {
  return (
    <Badge variant="outline" className="gap-1.5">
      <span className={`size-2 rounded-full ${DOT_CLASS[status]}`} />
      {STATUS_LABEL[status]}
    </Badge>
  );
}

type CardProps = {
  title: string;
  icon: LucideIcon;
  href: string;
  primary?: string;
  status?: Status;
  loading?: boolean;
  error?: unknown;
};

// ponytail: one generic card, 6 callers pass pre-computed values. Keeps the
// 6 distinct count/colour rules in one render scope each, no inheritance.
function SummaryCard({
  title,
  icon: Icon,
  href,
  primary,
  status,
  loading,
  error,
}: CardProps) {
  return (
    <Card className="summary-card">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-sm">
          <Icon className="size-4 text-muted-foreground" />
          {title}
        </CardTitle>
      </CardHeader>
      <CardContent>
        {loading ? (
          <div className="flex items-center gap-2 text-muted-foreground">
            <Loader2 className="size-4 animate-spin" />
            <span className="text-sm">Loading…</span>
          </div>
        ) : error ? (
          <div className="flex items-center gap-2">
            <Badge variant="destructive">Error</Badge>
            <span className="text-xs text-muted-foreground">will retry</span>
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between gap-2">
              <div className="text-2xl font-semibold">{primary ?? "—"}</div>
              {status ? <StatusBadge status={status} /> : null}
            </div>
            <Link
              href={href}
              className="mt-2 inline-block text-xs text-muted-foreground hover:text-foreground"
            >
              View details →
            </Link>
          </>
        )}
      </CardContent>
    </Card>
  );
}

export default function OverviewPage() {
  const nodes = useNodes();
  const pods = usePods();
  const alerts = useAlerts();
  const longhorn = useLonghorn();
  const cnpg = useCnpg();
  const services = useServices();

  // `useQuery` queryFn returns `Promise<any>` (fetch().then(r=>r.json())),
  // so .data is `any`. Treat as opaque; only read fields we know exist.
  const nodeList: { ready?: boolean }[] = Array.isArray(nodes.data)
    ? nodes.data
    : [];
  const nodesReady = nodeList.filter((n) => n?.ready === true).length;
  const nodesTotal = nodeList.length;
  const nodesStatus: Status =
    nodesTotal === 0
      ? "green"
      : nodesReady === nodesTotal
        ? "green"
        : nodesReady >= 8
          ? "yellow"
          : "red";

  const podByNode =
    pods.data && typeof pods.data === "object" && pods.data.nodes && typeof pods.data.nodes === "object"
      ? (pods.data.nodes as Record<string, { status?: string }[]>)
      : {};
  const allPods = Object.values(podByNode).flat();
  const runningPods = allPods.filter((p) => p?.status === "Running").length;
  const failedPods = allPods.filter((p) =>
    ["Failed", "CrashLoopBackOff", "Error"].includes(p?.status ?? ""),
  ).length;
  const podsStatus: Status =
    failedPods === 0 ? "green" : failedPods <= 5 ? "yellow" : "red";

  const alertList: { severity?: string }[] = Array.isArray(alerts.data)
    ? alerts.data
    : [];
  const critical = alertList.filter((a) => a?.severity === "critical").length;
  const warning = alertList.filter((a) => a?.severity === "warning").length;
  const alertsStatus: Status =
    critical > 0 ? "red" : warning > 0 ? "yellow" : "green";

  const lhVolumes: { robustness?: string }[] = Array.isArray(longhorn.data?.volumes)
    ? longhorn.data.volumes
    : [];
  const healthy = lhVolumes.filter((v) => v?.robustness === "healthy").length;
  const degraded = lhVolumes.filter((v) => v?.robustness === "degraded").length;
  const faulted = lhVolumes.filter((v) => v?.robustness === "faulted").length;
  const lhStatus: Status =
    faulted > 0 ? "red" : degraded > 0 ? "yellow" : "green";

  const clusters: { instances?: number; readyInstances?: number }[] = Array.isArray(
    cnpg.data?.clusters,
  )
    ? cnpg.data.clusters
    : [];
  const totalInstances = clusters.reduce((s, c) => s + (c?.instances ?? 0), 0);
  const readyInstances = clusters.reduce(
    (s, c) => s + (c?.readyInstances ?? 0),
    0,
  );
  const pgStatus: Status =
    totalInstances === 0
      ? "green"
      : readyInstances === totalInstances
        ? "green"
        : "red";

  const serviceList: { status?: string }[] = Array.isArray(services.data)
    ? services.data
    : [];
  const passing = serviceList.filter((s) => s?.status === "pass").length;
  const serviceTotal = serviceList.length;
  const servicesStatus: Status =
    serviceTotal === 0
      ? "green"
      : passing === serviceTotal
        ? "green"
        : passing >= 4
          ? "yellow"
          : "red";

  return (
    <div className="space-y-4">
      <h1 className="text-xl font-semibold">Overview</h1>
      <div className="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        <SummaryCard
          title="Nodes"
          icon={Server}
          href="/nodes"
          loading={nodes.isLoading}
          error={nodes.error}
          primary={nodesTotal ? `${nodesReady}/${nodesTotal} ready` : undefined}
          status={nodesStatus}
        />
        <SummaryCard
          title="Pods"
          icon={Box}
          href="/pods"
          loading={pods.isLoading}
          error={pods.error}
          primary={allPods.length ? `${runningPods} running` : undefined}
          status={podsStatus}
        />
        <SummaryCard
          title="Active Alerts"
          icon={Bell}
          href="/alerts"
          loading={alerts.isLoading}
          error={alerts.error}
          primary={`${critical} critical · ${warning} warning`}
          status={alertsStatus}
        />
        <SummaryCard
          title="Storage (Longhorn)"
          icon={HardDrive}
          href="/longhorn"
          loading={longhorn.isLoading}
          error={longhorn.error}
          primary={
            lhVolumes.length
              ? `${healthy} healthy · ${degraded} degraded · ${faulted} faulted`
              : undefined
          }
          status={lhStatus}
        />
        <SummaryCard
          title="Postgres"
          icon={Database}
          href="/postgres"
          loading={cnpg.isLoading}
          error={cnpg.error}
          primary={
            totalInstances
              ? `${readyInstances}/${totalInstances} instances`
              : undefined
          }
          status={pgStatus}
        />
        <SummaryCard
          title="Services"
          icon={Activity}
          href="/services"
          loading={services.isLoading}
          error={services.error}
          primary={
            serviceTotal ? `${passing}/${serviceTotal} healthy` : undefined
          }
          status={servicesStatus}
        />
      </div>
    </div>
  );
}
