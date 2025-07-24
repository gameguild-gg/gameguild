'use client';

import * as React from 'react';
import Link from 'next/link';
import { BookOpen, Building2, FileText, LayoutDashboard, MessageSquare, Package, Settings, TestTube, Users } from 'lucide-react';
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarMenu, SidebarMenuButton, SidebarMenuItem, SidebarRail } from '@/components/ui/sidebar';
import { PlatformManagementSidebarContent } from '@/components/dashboard/platform-management-sidebar-content';
import { ContentManagementSidebarContent } from '@/components/dashboard/content-management-sidebar-content';
import { TenantSwitcher } from '@/components/dashboard/tenant-switcher';
import { TenantResponse } from '@/lib/tenants/types';

// This is sample data structured for the sidebar
const navigationData = {
  navMain: [
    {
      title: 'Overview',
      url: '/dashboard/overview',
      icon: LayoutDashboard,
      isActive: true,
    },
    {
      title: 'Tenants',
      url: '/dashboard/tenant',
      icon: Building2,
    },
    {
      title: 'Users',
      url: '/dashboard/users',
      icon: Users,
    },
  ],
  content: [
    {
      name: 'Projects',
      url: '/dashboard/projects',
      icon: Package,
    },
    {
      name: 'Testing Lab',
      url: '/dashboard/testing-lab',
      icon: TestTube,
    },
    {
      name: 'Courses',
      url: '/dashboard/courses',
      icon: BookOpen,
    },
    {
      name: 'Reports',
      url: '/dashboard/reports',
      icon: FileText,
    },
  ],
};

export const DashboardSidebar = ({ tenants = [], ...props }: React.ComponentProps<typeof Sidebar> & { tenants?: TenantResponse[] }) => {
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <TenantSwitcher initialTenants={tenants} />
      </SidebarHeader>
      <SidebarContent>
        <PlatformManagementSidebarContent items={navigationData.navMain} />
        <ContentManagementSidebarContent items={navigationData.content} />
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton size="lg" asChild>
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
            <SidebarMenuButton size="lg" asChild>
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
