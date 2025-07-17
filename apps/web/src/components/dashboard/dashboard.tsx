import React, { PropsWithChildren } from 'react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarRail,
  SidebarSeparator,
  SidebarTrigger,
} from '@game-guild/ui/components/sidebar';
import { BarChart3, BookOpen, Building2, FileText, LayoutDashboard, Package, Send, Settings, Users } from 'lucide-react';
import { cookies } from 'next/headers';
import { DashboardHeader } from './layout/dashboard-header';

const data = {
  navigation: {
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
  },
};

export const Dashboard = async ({ children }: PropsWithChildren) => {
  const cookieStore = await cookies();
  const defaultOpen = cookieStore.get('sidebar_state')?.value === 'true';

  return (
    <SidebarProvider defaultOpen={defaultOpen}>
      <DashboardSidebar />
      <SidebarInset className="flex flex-col flex-1">
        <DashboardHeader />
        <main className="flex-1 overflow-auto">
          <div className="container mx-auto p-6">{children}</div>
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
};

export const DashboardSidebar = ({ ...props }: React.ComponentProps<typeof Sidebar>) => {
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <div className="flex items-center gap-2 px-4 py-2">
          <div className="h-8 w-8 rounded-lg bg-primary flex items-center justify-center">
            <span className="text-primary-foreground font-bold text-sm">GG</span>
          </div>
          <div className="group-data-[collapsible=icon]:hidden">
            <h1 className="font-semibold">Game Guild</h1>
            <p className="text-xs text-muted-foreground">Dashboard</p>
          </div>
        </div>
      </SidebarHeader>
      <SidebarContent>
        <SidebarGroup>
          <SidebarMenu>
            {data.navigation.primary.map((item) => (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton asChild size="default" tooltip={item.title}>
                  <a href={item.url}>
                    <item.icon />
                    <span>{item.title}</span>
                  </a>
                </SidebarMenuButton>
              </SidebarMenuItem>
            ))}
          </SidebarMenu>
        </SidebarGroup>
      </SidebarContent>
      <SidebarFooter>
        <SidebarSeparator />
        <SidebarMenu>
          {data.navigation.secondary.map((item) => (
            <SidebarMenuItem key={item.title}>
              <SidebarMenuButton asChild size="default" tooltip={item.title}>
                <a href={item.url}>
                  <item.icon />
                  <span>{item.title}</span>
                </a>
              </SidebarMenuButton>
            </SidebarMenuItem>
          ))}
        </SidebarMenu>
        <SidebarSeparator />
        <div className="flex items-center justify-center">
          <SidebarTrigger />
        </div>
      </SidebarFooter>
      <SidebarRail />
    </Sidebar>
  );
};
