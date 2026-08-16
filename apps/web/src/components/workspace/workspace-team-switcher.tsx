'use client';

import { ChevronsUpDown, UserRound, UsersRound } from 'lucide-react';

import { Link, usePathname } from '@/i18n/navigation';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import {
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSkeleton,
  useSidebar,
} from '@game-guild/ui/components/sidebar';

import type { WorkspaceTeamSummary } from './workspace-sidebar';

/**
 * Team switcher for the workspace sidebar — mirrors the console context
 * switcher. Personal team always exists; other teams navigate to their
 * workspace. No team is required to use the workspace.
 */
export function WorkspaceTeamSwitcher({ teams }: { teams: readonly WorkspaceTeamSummary[] }) {
  const { isMobile } = useSidebar();
  const pathname = usePathname();
  const personal = teams.find((team) => team.isPersonal);
  const active =
    teams.find((team) => pathname?.startsWith(`/workspace/teams/${team.slug}`)) ?? personal;
  const activeName = active?.name ?? 'Personal';
  const ActiveIcon = active?.isPersonal ? UserRound : UsersRound;

  return (
    <SidebarMenu>
      <SidebarMenuItem>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <SidebarMenuButton
              size="lg"
              className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground"
            >
              <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                <ActiveIcon className="size-4" />
              </span>
              <span className="grid min-w-0 flex-1 text-left">
                <span className="truncate text-xs font-medium text-muted-foreground">
                  {active?.isPersonal ? 'Personal team' : 'Team'}
                </span>
                <span className="truncate text-sm font-semibold">{activeName}</span>
              </span>
              <ChevronsUpDown className="ml-auto size-4" />
            </SidebarMenuButton>
          </DropdownMenuTrigger>
          <DropdownMenuContent className="w-(--radix-dropdown-menu-trigger-width) min-w-56 rounded-lg" side={isMobile ? 'bottom' : 'right'} align="start" sideOffset={4}>
            <DropdownMenuLabel className="text-xs text-muted-foreground">Teams</DropdownMenuLabel>
            {teams.length === 0 && <SidebarMenuSkeleton className="mx-2 my-1" />}
            {teams.map((team) => (
              <DropdownMenuItem key={team.id} asChild>
                <Link href={`/workspace/teams/${team.slug}`}>
                  {team.isPersonal ? <UserRound className="size-4" /> : <UsersRound className="size-4" />}
                  <span className="truncate">{team.name}</span>
                  {team.isPersonal && <span className="ml-auto text-xs text-muted-foreground">Personal</span>}
                </Link>
              </DropdownMenuItem>
            ))}
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <Link href="/workspace/teams/new">
                <UsersRound className="size-4" />
                Create team
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </SidebarMenuItem>
    </SidebarMenu>
  );
}
