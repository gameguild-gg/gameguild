'use client';

import React from 'react';
import {
  AudioWaveform,
  BarChart3,
  BookOpen,
  Building2,
  Command,
  FileText,
  GalleryVerticalEnd,
  LayoutDashboard,
  Package,
  Send,
  Settings,
  TestTube,
  Users,
} from 'lucide-react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
} from '@/components/ui/sidebar';
import { TenantSwitcher } from '@/components/dashboard/tenant-switcher';
import { TenantResponse } from '@/lib/tenants/types';

const navigationData = {
  tenants: [
    {
      name: 'Acme Inc',
      logo: GalleryVerticalEnd,
      plan: 'Enterprise',
    },
    {
      name: 'Acme Corp.',
      logo: AudioWaveform,
      plan: 'Startup',
    },
    {
      name: 'Evil Corp.',
      logo: Command,
      plan: 'Free',
    },
  ],
  primary: [
    {
      title: 'Overview',
      url: '/dashboard/overview',
      icon: LayoutDashboard,
    },
    {
      title: 'Users',
      url: '/dashboard/users',
      icon: Users,
    },
    {
      title: 'Courses',
      url: '/dashboard/courses',
      icon: BookOpen,
    },
    {
      title: 'Analytics',
      url: '/dashboard/analytics',
      icon: BarChart3,
    },
    {
      title: 'Reports',
      url: '/dashboard/reports',
      icon: FileText,
    },
    {
      title: 'Projects',
      url: '/dashboard/projects',
      icon: Package,
    },
    {
      title: 'Testing Lab',
      url: '/dashboard/testing-lab',
      icon: TestTube,
    },
    {
      title: 'Tenant Management',
      url: '/dashboard/tenant',
      icon: Building2,
    },
  ],
  secondary: [
    {
      title: 'Settings',
      url: '/dashboard/settings',
      icon: Settings,
    },
    {
      title: 'Feedback',
      url: '#',
      icon: Send,
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
        <SidebarGroup>
          <SidebarGroupLabel>Platform</SidebarGroupLabel>
          <SidebarMenu>
            {navigationData.primary.map((item) => (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton
                  size="default"
                  tooltip={item.title}
                  className="text-slate-300 hover:text-white hover:bg-gradient-to-r hover:from-slate-800/60 hover:to-slate-700/40 data-[active=true]:bg-gradient-to-r data-[active=true]:from-blue-600/20 data-[active=true]:to-purple-600/20 data-[active=true]:text-blue-400 data-[active=true]:border-l-2 data-[active=true]:border-blue-400 data-[active=true]:shadow-lg data-[active=true]:shadow-blue-500/10 rounded-lg mx-1 transition-all duration-200 backdrop-blur-sm"
                >
                  <a href={item.url}>
                    <item.icon className="h-4 w-4" />
                    <span className="font-medium">{item.title}</span>
                  </a>
                </SidebarMenuButton>
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <SidebarMenu>
          {navigationData.secondary.map((item) => (
            <SidebarMenuItem key={item.title}>
              <SidebarMenuButton className="data-[state=open]:bg-sidebar-accent data-[state=open]:text-sidebar-accent-foreground">
                <a href={item.url}>
                  <item.icon />
                  <span>{item.title}</span>
                </a>
              </SidebarMenuButton>
            </SidebarMenuItem>
          ))}
        </SidebarMenu>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
};
