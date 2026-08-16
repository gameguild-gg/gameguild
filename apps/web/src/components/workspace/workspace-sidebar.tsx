'use client';

import {
  FolderKanban,
  Home,
  MailCheck,
  Settings,
  Users,
  Video,
} from 'lucide-react';
import { usePathname } from 'next/navigation';

import { Link } from '@/i18n/navigation';
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@game-guild/ui/components/sidebar';

import { WorkspaceTeamSwitcher } from './workspace-team-switcher';

export interface WorkspaceTeamSummary {
  id: string;
  slug: string;
  name: string;
  isPersonal: boolean;
}

export const workspaceNav = [
  { title: 'Home', url: '/workspace', icon: Home },
  { title: 'Projects', url: '/workspace/projects', icon: FolderKanban },
  { title: 'Teams', url: '/workspace/teams', icon: Users },
  { title: 'Learning', url: '/workspace/learning', icon: Video },
  { title: 'Invitations', url: '/workspace/invitations', icon: MailCheck },
  { title: 'Settings', url: '/workspace/settings/account', icon: Settings },
] as const;

function WorkspaceNav() {
  const pathname = usePathname() ?? '';

  return (
    <SidebarGroup>
      <SidebarGroupContent>
        <SidebarMenu>
          {workspaceNav.map((item) => {
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
  );
}

export function WorkspaceSidebar({ teams }: { teams: readonly WorkspaceTeamSummary[] }) {
  return (
    <Sidebar collapsible="icon">
      <SidebarHeader>
        <WorkspaceTeamSwitcher teams={teams} />
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <WorkspaceNav />
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
