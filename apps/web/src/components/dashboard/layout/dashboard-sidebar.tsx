'use client';

import React from 'react';
import { BarChart3, BookOpen, Building2, FileText, LayoutDashboard, Package, Send, Settings, TestTube, Users } from 'lucide-react';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail,
  SidebarSeparator,
  SidebarTrigger,
} from '@/components/ui/sidebar';

const navigationData = {
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

export function DashboardSidebar(props: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar
      collapsible="icon"
      className="border-r border-slate-700/30 bg-gradient-to-b from-slate-900 via-slate-800 to-slate-900 relative overflow-hidden"
      {...props}
    >
      {/* Background glow effects similar to contributors page */}
      <div className="absolute top-0 left-0 w-full h-px bg-gradient-to-r from-transparent via-blue-400/30 to-transparent"></div>
      <div className="absolute -top-20 -left-20 w-40 h-40 rounded-full bg-gradient-to-r from-blue-500/10 via-purple-500/5 to-transparent blur-3xl"></div>
      <div className="absolute -bottom-20 -right-20 w-32 h-32 rounded-full bg-gradient-to-r from-purple-500/8 via-blue-500/5 to-transparent blur-3xl"></div>
      
      <SidebarHeader className="p-4 relative z-10">
        <div className="flex items-center gap-3">
          <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-blue-500 via-blue-600 to-purple-600 flex items-center justify-center shadow-lg shadow-blue-500/25 relative">
            <div className="absolute inset-0 rounded-xl bg-gradient-to-br from-blue-400/20 to-purple-400/20 blur-sm"></div>
            <span className="text-white font-bold text-sm relative z-10">GG</span>
          </div>
          <div className="group-data-[collapsible=icon]:hidden">
            <h1 className="font-bold text-lg bg-gradient-to-r from-blue-400 to-purple-400 bg-clip-text text-transparent">Game Guild</h1>
            <p className="text-xs text-slate-400 font-medium">Admin Dashboard</p>
          </div>
        </div>
      </SidebarHeader>

      <SidebarContent className="px-2 relative z-10">
        <SidebarGroup>
          <SidebarMenu className="space-y-1">
            {navigationData.primary.map((item) => (
              <SidebarMenuItem key={item.title}>
                <SidebarMenuButton
                  asChild
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

      <SidebarFooter className="relative z-10">
        <SidebarSeparator className="bg-gradient-to-r from-transparent via-slate-700/50 to-transparent" />
        <SidebarMenu>
          {navigationData.secondary.map((item) => (
            <SidebarMenuItem key={item.title}>
              <SidebarMenuButton
                asChild
                size="default"
                tooltip={item.title}
                className="text-slate-300 hover:text-white hover:bg-gradient-to-r hover:from-slate-800/50 hover:to-slate-700/30 backdrop-blur-sm rounded-lg mx-1 transition-all duration-200"
              >
                <a href={item.url}>
                  <item.icon />
                  <span>{item.title}</span>
                </a>
              </SidebarMenuButton>
            </SidebarMenuItem>
          ))}
        </SidebarMenu>
        <SidebarSeparator className="bg-gradient-to-r from-transparent via-slate-700/50 to-transparent" />
        <div className="flex items-center justify-center">
          <SidebarTrigger className="text-slate-400 hover:text-white hover:bg-slate-800/50 rounded-lg transition-all duration-200 backdrop-blur-sm" />
        </div>
      </SidebarFooter>

      <SidebarRail />
    </Sidebar>
  );
}
