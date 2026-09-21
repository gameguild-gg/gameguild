'use client';

import * as React from 'react';
import { Link, usePathname } from '@/i18n/navigation';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@game-guild/ui/components/collapsible';
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
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  SidebarRail,
  SidebarTrigger,
} from '@game-guild/ui/components/sidebar';
import { TenantSwitcher, type Tenant } from '@/components/console/tenant-switcher';
import { flattenWorkspaceNavigationItems } from '@/components/console/workspace-sidebar';
import { ChevronRight, GraduationCap, type LucideIcon } from 'lucide-react';
import { useSearchParams } from 'next/navigation';

// Types for the shared navigation structure rendered by `AppShellSidebar`.
export interface WorkspaceNavSubItem {
  title: string;
  url: string;
  icon: LucideIcon;
  isActive?: boolean;
  badge?: string;
  requiredCapabilities?: readonly string[];
}

export interface WorkspaceNavItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items: WorkspaceNavSubItem[];
  requiredCapabilities?: readonly string[];
}

export interface WorkspaceNavGroupItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items?: WorkspaceNavSubItem[];
  subGroups?: WorkspaceNavItem[];
  requiredCapabilities?: readonly string[];
  activeOnPath?: string;
  activeOnTab?: string;
}

export interface WorkspaceNavGroup {
  label: string;
  items: WorkspaceNavGroupItem[];
}

function NotificationChip({ count }: { count: number }) {
  const label = count > 99 ? '99+' : String(count);
  return (
    <>
      <span className="ml-auto hidden size-2 shrink-0 rounded-full bg-primary group-data-[collapsible=icon]:block" aria-hidden="true" />
      <span className="ml-auto shrink-0 rounded-full bg-primary px-1.5 py-0.5 text-xs font-medium text-primary-foreground group-data-[collapsible=icon]:hidden">
        {label}
      </span>
    </>
  );
}

function NavGroups({ groups, notificationCounts }: { groups: WorkspaceNavGroup[]; notificationCounts?: Record<string, number> }) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [openItems, setOpenItems] = React.useState<Set<string>>(new Set());

  const toggleItem = (key: string) => {
    setOpenItems((prev) => {
      const next = new Set(prev);
      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }
      return next;
    });
  };

  return (
    <>
      {groups.map((group) => (
        <SidebarGroup key={group.label || 'sidebar'}>
          {group.label ? <SidebarGroupLabel>{group.label}</SidebarGroupLabel> : null}
          <SidebarGroupContent>
            <SidebarMenu className="gap-2">
              {group.items.map((item) => {
                const Icon = item.icon;
                const hasItems = item.items && item.items.length > 0;
                const hasSubGroups = item.subGroups && item.subGroups.length > 0;
                const isOpen = openItems.has(item.title);

                // Simple link item (no children)
                if (!hasItems && !hasSubGroups && item.url) {
                  const isActive = item.activeOnPath
                    ? pathname?.startsWith(item.activeOnPath) ?? false
                    : item.activeOnTab !== undefined
                      ? pathname === item.url &&
                        (item.activeOnTab === ''
                          ? !searchParams.has('tab')
                          : searchParams.get('tab') === item.activeOnTab)
                      : pathname === item.url || pathname?.endsWith(item.url);
                  return (
                    <SidebarMenuItem key={item.title} className="group-data-[collapsible=icon]:mx-auto group-data-[collapsible=icon]:w-8">
                      <SidebarMenuButton isActive={isActive} tooltip={item.title} className="[&_svg]:size-5 group-data-[collapsible=icon]:justify-center group-data-[collapsible=icon]:p-0 data-active:bg-sidebar-primary/15 data-active:text-sidebar-primary data-active:font-medium" render={<Link href={item.url} />}>
                        {Icon && <Icon className="size-5" />}
                        <span className="group-data-[collapsible=icon]:hidden">{item.title}</span>
                        {notificationCounts?.[item.url] ? (
                          <NotificationChip count={notificationCounts[item.url]} />
                        ) : null}
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  );
                }

                // Collapsible item with sub-items
                if (hasItems) {
                  const childActive = item.items!.some(
                    (subItem) => pathname === subItem.url || pathname?.endsWith(subItem.url),
                  );
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem className="group-data-[collapsible=icon]:mx-auto group-data-[collapsible=icon]:w-8">
                        <CollapsibleTrigger render={<SidebarMenuButton isActive={childActive} tooltip={item.title} className="[&_svg]:size-5 group-data-[collapsible=icon]:justify-center data-active:bg-sidebar-primary/15 data-active:text-sidebar-primary data-active:font-medium" />}>
                          {Icon && <Icon className="size-5" />}
                          <span className="group-data-[collapsible=icon]:hidden">{item.title}</span>
                          <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90 group-data-[collapsible=icon]:hidden" />
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.items!.map((subItem) => {
                              const isActive = pathname === subItem.url || pathname?.endsWith(subItem.url);
                              return (
                                <SidebarMenuSubItem key={subItem.title}>
                                  <SidebarMenuSubButton isActive={isActive} className="[&_svg]:size-5 data-active:bg-sidebar-primary/15 data-active:text-sidebar-primary data-active:font-medium" render={<Link href={subItem.url} />}>
                                    <subItem.icon className="size-5" />
                                    <span>{subItem.title}</span>
                                    {subItem.badge && (
                                      <span className="ml-auto rounded-full bg-primary px-2 py-0.5 text-xs text-primary-foreground">{subItem.badge}</span>
                                    )}
                                  </SidebarMenuSubButton>
                                </SidebarMenuSubItem>
                              );
                            })}
                          </SidebarMenuSub>
                        </CollapsibleContent>
                      </SidebarMenuItem>
                    </Collapsible>
                  );
                }

                // Collapsible item with sub-groups (nested)
                if (hasSubGroups) {
                  const childActive = item.subGroups!.some(
                    (subGroup) =>
                      Boolean(
                        subGroup.url &&
                          (pathname === subGroup.url ||
                            (subGroup.url !== '/workspace/testing-lab' &&
                              pathname?.startsWith(`${subGroup.url}/`))),
                      ),
                  );
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem className="group-data-[collapsible=icon]:mx-auto group-data-[collapsible=icon]:w-8">
                        <CollapsibleTrigger render={<SidebarMenuButton isActive={childActive} tooltip={item.title} className="[&_svg]:size-5 group-data-[collapsible=icon]:justify-center data-active:bg-sidebar-primary/15 data-active:text-sidebar-primary data-active:font-medium" />}>
                          {Icon && <Icon className="size-5" />}
                          <span className="group-data-[collapsible=icon]:hidden">{item.title}</span>
                          <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90 group-data-[collapsible=icon]:hidden" />
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.subGroups!.map((subGroup) => {
                              const isActive = Boolean(
                                subGroup.url &&
                                  (pathname === subGroup.url ||
                                    (subGroup.url !== '/workspace/testing-lab' &&
                                      pathname?.startsWith(`${subGroup.url}/`))),
                              );
                              const SubIcon = subGroup.icon;
                              return (
                                <SidebarMenuSubItem key={subGroup.title}>
                                  <SidebarMenuSubButton isActive={isActive} className="[&_svg]:size-5 data-active:bg-sidebar-primary/15 data-active:text-sidebar-primary data-active:font-medium" render={<Link href={subGroup.url || '#'} />}>
                                    {SubIcon && <SubIcon className="size-5" />}
                                    <span>{subGroup.title}</span>
                                  </SidebarMenuSubButton>
                                </SidebarMenuSubItem>
                              );
                            })}
                          </SidebarMenuSub>
                        </CollapsibleContent>
                      </SidebarMenuItem>
                    </Collapsible>
                  );
                }

                return null;
              })}
            </SidebarMenu>
          </SidebarGroupContent>
        </SidebarGroup>
      ))}
    </>
  );
}

/** Default console tenant — GameGuild platform until multi-tenant switching ships. */
const tenants: Tenant[] = [
  { id: 'gameguild', name: 'GameGuild', logo: GraduationCap, plan: 'Platform' },
];

function countNotificationsByUrl(
  notifications: DashboardNotificationSummary | undefined,
  navigation: WorkspaceNavGroup[],
): Record<string, number> {
  const navUrls = flattenWorkspaceNavigationItems(navigation)
    .map((item) => item.url)
    .filter((url): url is string => Boolean(url));
  const counts: Record<string, number> = {};
  for (const item of notifications?.items ?? []) {
    const url = item.actionUrl;
    if (!url) continue;
    const navUrl = navUrls.find((candidate) => url === candidate || url.startsWith(`${candidate}/`));
    if (navUrl) counts[navUrl] = (counts[navUrl] ?? 0) + 1;
  }
  return counts;
}

interface AppShellSidebarProps {
  navigation: WorkspaceNavGroup[];
  notifications?: DashboardNotificationSummary;
}

/**
 * The shared app sidebar. Composed inside `AppShell`; callers supply the
 * navigation groups (workspace or social) and optional notification summary.
 */
export function AppShellSidebar({ navigation, notifications }: AppShellSidebarProps) {
  const notificationCounts = countNotificationsByUrl(notifications, navigation);
  return (
    <Sidebar collapsible="icon">
      <SidebarHeader className="h-16 p-2">
        <div className="flex h-full items-center justify-between gap-2 group-data-[collapsible=icon]:justify-center">
          <TenantSwitcher tenants={tenants} />
          <SidebarTrigger className="shrink-0 group-data-[collapsible=icon]:hidden [&_svg]:size-5" />
        </div>
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <NavGroups groups={navigation} notificationCounts={notificationCounts} />
      </SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
