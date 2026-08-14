'use client';

import * as React from 'react';
import { BriefcaseBusiness, ChevronsUpDown, FolderKanban, Gamepad2, Plus, Settings2, Users } from 'lucide-react';
import { Link, usePathname } from '@/i18n/navigation';
import type { DashboardContextSummary, DashboardContextType } from '@/lib/dashboard-contexts';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { SidebarMenu, SidebarMenuButton, SidebarMenuItem, useSidebar } from '@game-guild/ui/components/sidebar';

const contextMeta: Record<DashboardContextType, { label: string; icon: React.ElementType }> = {
  Workspace: { label: 'Workspace', icon: Gamepad2 },
  Team: { label: 'Team', icon: Users },
  Project: { label: 'Project', icon: FolderKanban },
  Operations: { label: 'Operations', icon: Settings2 },
};

function isOperationsPath(pathname: string | null): boolean {
  return Boolean(pathname?.startsWith('/dashboard/testing-lab') || pathname?.startsWith('/dashboard/launch-pad'));
}

export function ContextSwitcher({ contexts }: { contexts: readonly DashboardContextSummary[] }) {
  const { isMobile } = useSidebar();
  const pathname = usePathname();
  const available = contexts.length > 0
    ? contexts
    : [{ type: 'Workspace' as const, id: null, name: 'Workspace', route: '/dashboard' }];
  const active = available.find((context) =>
    context.type === 'Operations'
      ? isOperationsPath(pathname)
      : context.route !== '/dashboard' && (pathname === context.route || pathname?.startsWith(`${context.route}/`)),
  ) ?? available.find((context) => context.type === 'Workspace') ?? available[0]!;
  const ActiveIcon = contextMeta[active.type].icon;

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton size="lg" className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
              <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                <ActiveIcon className="size-4" />
              </div>
              <div className="grid flex-1 text-left text-sm leading-tight">
                <span className="truncate font-medium">{active.name}</span>
                <span className="truncate text-xs text-sidebar-foreground/70">{contextMeta[active.type].label}</span>
              </div>
              <ChevronsUpDown className="ml-auto size-4" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent
            className="w-[--radix-dropdown-menu-trigger-width] min-w-64 rounded-lg"
            align="start"
            side={isMobile ? 'bottom' : 'right'}
            sideOffset={4}
          >
            <DropdownMenuLabel className="text-xs text-muted-foreground">Work contexts</DropdownMenuLabel>
            {available.map((context) => {
              const Icon = contextMeta[context.type].icon;
              return (
                <DropdownMenuItem key={`${context.type}:${context.id ?? 'root'}`} asChild>
                  <Link href={context.route} className="gap-2 p-2">
                    <div className="flex size-7 items-center justify-center rounded-md border">
                      <Icon className="size-3.5" />
                    </div>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{context.name}</p>
                      <p className="text-xs text-muted-foreground">{contextMeta[context.type].label}</p>
                    </div>
                  </Link>
                </DropdownMenuItem>
              );
            })}
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <Link href="/dashboard/teams/new" className="gap-2 p-2">
                <div className="flex size-7 items-center justify-center rounded-md border bg-transparent">
                  <Plus className="size-4" />
                </div>
                <span className="font-medium">Create team</span>
              </Link>
            </DropdownMenuItem>
            <DropdownMenuItem asChild>
              <Link href="/dashboard/projects/new" className="gap-2 p-2">
                <div className="flex size-7 items-center justify-center rounded-md border bg-transparent">
                  <BriefcaseBusiness className="size-4" />
                </div>
                <span className="font-medium">Create project</span>
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}

export const TeamSwitcher = ContextSwitcher;
