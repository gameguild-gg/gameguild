'use client';

import {
  FolderKanban,
  Home,
  MailCheck,
  Settings,
  SquareCheck,
  Users,
} from 'lucide-react';
import { usePathname } from 'next/navigation';

import { Link } from '@/i18n/navigation';
import { Badge } from '@game-guild/ui/components/badge';
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@game-guild/ui/components/sidebar';
import { cn } from '@game-guild/ui/lib/utils';

import { WorkspaceTeamSwitcher } from './workspace-team-switcher';

export interface WorkspaceTeamSummary {
  id: string;
  slug: string;
  name: string;
  isPersonal: boolean;
}

const navGroups = [
  {
    label: 'Workspace',
    items: [
      { title: 'Hub', url: '/workspace', icon: Home },
      { title: 'Projects', url: '/workspace/projects', icon: FolderKanban },
      { title: 'Teams', url: '/workspace/teams', icon: Users },
      { title: 'Work', url: '/workspace/work', icon: SquareCheck },
      { title: 'Invitations', url: '/workspace/invitations', icon: MailCheck },
    ],
  },
  {
    label: 'Account',
    items: [{ title: 'Settings', url: '/workspace/settings/account', icon: Settings }],
  },
] as const;

function NavGroups() {
  const pathname = usePathname() ?? '';

  return (
    <>
      {navGroups.map((group) => (
        <SidebarGroup key={group.label}>
          <SidebarGroupLabel>{group.label}</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {group.items.map((item) => {
                const active =
                  item.url === '/workspace'
                    ? pathname === '/workspace'
                    : pathname.startsWith(item.url);
                return (
                  <SidebarMenuItem key={item.url}>
                    <SidebarMenuButton asChild isActive={active} tooltip={item.title}>
                      <Link href={item.url}>
                        <item.icon />
                        <span>{item.title}</span>
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      ))}
    </>
  );
}

export function WorkspaceSidebar({ teams }: { teams: readonly WorkspaceTeamSummary[] }) {
  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <WorkspaceTeamSwitcher teams={teams} />
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <NavGroups />
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
