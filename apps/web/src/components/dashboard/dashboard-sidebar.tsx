'use client';

import * as React from 'react';
import { BarChart3, BookOpen, Building2, FileText, LayoutDashboard, Package, Settings, TestTube, Users } from 'lucide-react';
import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, SidebarRail } from '@/components/ui/sidebar';
import { NavMain } from '@/components/dashboard/nav-main';
import { NavProjects } from '@/components/dashboard/nav-projects';
import { NavUser } from '@/components/dashboard/nav-user';
import { TenantSwitcher } from '@/components/dashboard/tenant-switcher';
import { TenantResponse } from '@/lib/tenants/types';

// This is sample data structured for the collapsible sidebar
const navigationData = {
  user: {
    name: 'User',
    email: 'user@example.com',
    avatar: '/avatars/user.jpg',
  },
  navMain: [
    {
      title: 'Dashboard',
      url: '/dashboard/overview',
      icon: LayoutDashboard,
      isActive: true,
      items: [
        {
          title: 'Overview',
          url: '/dashboard/overview',
        },
        {
          title: 'Analytics',
          url: '/dashboard/analytics',
        },
      ],
    },
    {
      title: 'User Management',
      url: '/dashboard/users',
      icon: Users,
      items: [
        {
          title: 'All Users',
          url: '/dashboard/users',
        },
        {
          title: 'User Roles',
          url: '/dashboard/users/roles',
        },
      ],
    },
    {
      title: 'Content',
      url: '/dashboard/courses',
      icon: BookOpen,
      items: [
        {
          title: 'Courses',
          url: '/dashboard/courses',
        },
        {
          title: 'Testing Lab',
          url: '/dashboard/testing-lab',
        },
      ],
    },
    {
      title: 'Reports',
      url: '/dashboard/reports',
      icon: FileText,
      items: [
        {
          title: 'Analytics',
          url: '/dashboard/analytics',
        },
        {
          title: 'Reports',
          url: '/dashboard/reports',
        },
      ],
    },
    {
      title: 'Settings',
      url: '/dashboard/settings',
      icon: Settings,
      items: [
        {
          title: 'General',
          url: '/dashboard/settings',
        },
        {
          title: 'Tenant Management',
          url: '/dashboard/tenant',
        },
      ],
    },
  ],
  projects: [
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
      name: 'Tenant Management',
      url: '/dashboard/tenant',
      icon: Building2,
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
        <NavMain items={navigationData.navMain} />
        <NavProjects projects={navigationData.projects} />
      </SidebarContent>
      <SidebarFooter>
        <NavUser user={navigationData.user} />
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
};
