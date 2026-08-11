"use client";

import {
  Activity,
  Archive,
  Bell,
  Box,
  Database,
  HardDrive,
  LayoutDashboard,
  Server,
  Warehouse,
  type LucideIcon,
} from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { cn } from "@game-guild/ui/lib/utils";

type NavItem = {
  href: string;
  label: string;
  icon: LucideIcon;
  match: (pathname: string) => boolean;
};

const NAV_ITEMS: NavItem[] = [
  { href: "/", label: "Overview", icon: LayoutDashboard, match: (p) => p === "/" },
  { href: "/nodes", label: "Nodes", icon: Server, match: (p) => p.startsWith("/nodes") },
  { href: "/pods", label: "Pods", icon: Box, match: (p) => p.startsWith("/pods") },
  { href: "/longhorn", label: "Longhorn", icon: HardDrive, match: (p) => p.startsWith("/longhorn") },
  { href: "/garage", label: "Garage", icon: Warehouse, match: (p) => p.startsWith("/garage") },
  { href: "/postgres", label: "Postgres", icon: Database, match: (p) => p.startsWith("/postgres") },
  { href: "/alerts", label: "Alerts", icon: Bell, match: (p) => p.startsWith("/alerts") },
  { href: "/services", label: "Services", icon: Activity, match: (p) => p.startsWith("/services") },
  { href: "/backups", label: "Backups", icon: Archive, match: (p) => p.startsWith("/backups") },
];

const LINK_BASE =
  "flex items-center gap-3 rounded-md px-3 py-2 text-sm transition-colors hover:bg-sidebar-accent hover:text-sidebar-accent-foreground";
const LINK_ACTIVE = "bg-sidebar-accent text-sidebar-accent-foreground font-medium";

export function Sidebar() {
  const pathname = usePathname();
  return (
    <nav
      aria-label="Main"
      className="flex h-svh w-60 shrink-0 flex-col gap-1 border-r border-sidebar-border bg-sidebar p-3 text-sidebar-foreground"
    >
      <div className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-sidebar-foreground/60">
        Ops
      </div>
      {NAV_ITEMS.map((item) => {
        const active = item.match(pathname);
        const Icon = item.icon;
        return (
          <Link
            key={item.href}
            href={item.href}
            className={cn(LINK_BASE, active && LINK_ACTIVE)}
            data-active={active ? "true" : undefined}
          >
            <Icon className="size-4 shrink-0" />
            <span className="truncate">{item.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
