'use client';

import React, { ComponentProps } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { MessageSquare, Settings } from 'lucide-react';
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem, SidebarRail } from '@/components/ui/sidebar';

import { TenantSwitcher } from '@/components/tenant/common/ui/tenant-switcher';
import { navigationData } from '@/components/dashboard/common/ui/navigation-data';
import { PlatformManagementSidebarContent } from '@/components/dashboard/common/ui/platform-management-sidebar-content';
import { ContentManagementSidebarContent } from '@/components/dashboard/common/ui/content-management-sidebar-content';

export const DashboardSidebar = ({ ...props }: ComponentProps<typeof Sidebar>): React.JSX.Element => {
  const pathname = usePathname();

  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <TenantSwitcher />
        {/* TODO: Add an user profile like YouTube Creator's page. */}
      </SidebarHeader>
      <SidebarContent>
        <PlatformManagementSidebarContent items={navigationData.primary} />
        <ContentManagementSidebarContent items={navigationData.content} testingLabItems={navigationData.testingLab} />
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" isActive={pathname === '/dashboard/feedback' || pathname.startsWith('/dashboard/feedback/')} asChild>
              <Link href="/dashboard/feedback">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                  <MessageSquare className="size-4" />
                </div>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-semibold">Feedback</span>
                  <span className="truncate text-xs">Send feedback</span>
                </div>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" isActive={pathname === '/dashboard/settings' || pathname.startsWith('/dashboard/settings/')} asChild>
              <Link href="/dashboard/settings">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                  <Settings className="size-4" />
                </div>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-semibold">Settings</span>
                  <span className="truncate text-xs">Manage preferences</span>
                </div>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
};
