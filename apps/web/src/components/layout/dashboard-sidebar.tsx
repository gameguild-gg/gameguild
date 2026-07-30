'use client';

import { Link, usePathname } from '@/i18n/navigation';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@game-guild/ui/components/collapsible';
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
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
} from '@game-guild/ui/components/sidebar';
import {
  BarChart3,
  BookOpen,
  ChevronRight,
  FileText,
  FlaskConical,
  FolderOpen,
  Gamepad2,
  HeadphonesIcon,
  LayoutDashboard,
  MailCheck,
  FolderKanban,
  Rocket,
  Settings,
  ShieldCheck,
  UserCog,
  Users,
  type LucideIcon,
} from 'lucide-react';
import * as React from 'react';

// Types for navigation structure
export interface DashboardNavSubItem {
  title: string;
  url: string;
  icon: LucideIcon;
  isActive?: boolean;
  badge?: string;
}

export interface DashboardNavItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items: DashboardNavSubItem[];
}

export interface DashboardNavGroupItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items?: DashboardNavSubItem[];
  subGroups?: DashboardNavItem[];
}

export interface DashboardNavGroup {
  label: string;
  items: DashboardNavGroupItem[];
}

// Game Guild Dashboard navigation structure
// Routes map to: /[locale]/(dashboard)/dashboard/...
export const dashboardNavigationData: DashboardNavGroup[] = [
  {
    label: 'Overview',
    items: [
      {
        title: 'Dashboard',
        url: '/dashboard',
        icon: LayoutDashboard,
      },
      {
        title: 'Invitations',
        url: '/dashboard/invitations',
        icon: MailCheck,
      },
    ],
  },
  {
    label: 'Community Management',
    items: [
      {
        title: 'Overview',
        url: '/dashboard/community',
        icon: LayoutDashboard,
      },
      {
        title: 'Members',
        icon: Users,
        subGroups: [
          {
            title: 'Overview',
            url: '/dashboard/community/members',
            icon: LayoutDashboard,
            items: [],
          },
          {
            title: 'Users',
            url: '/dashboard/community/members/users',
            icon: UserCog,
            items: [],
          },
          {
            title: 'Groups',
            url: '/dashboard/community/members/groups',
            icon: Users,
            items: [],
          },
          {
            title: 'Support',
            url: '/dashboard/community/members/support',
            icon: HeadphonesIcon,
            items: [],
          },
        ],
      },
    ],
  },
  {
    label: 'Platform Management',
    items: [
      {
        title: 'Roles',
        url: '/dashboard/platform/roles',
        icon: ShieldCheck,
      },
      {
        title: 'Learning',
        icon: BookOpen,
        subGroups: [
          {
            title: 'Overview',
            url: '/dashboard/learning',
            icon: LayoutDashboard,
            items: [],
          },
          {
            title: 'Courses',
            url: '/dashboard/learning/courses',
            icon: BookOpen,
            items: [],
          },
          {
            title: 'Tutorials',
            url: '/dashboard/learning/tutorials',
            icon: FileText,
            items: [],
          },
          {
            title: 'Resources',
            url: '/dashboard/learning/resources',
            icon: FolderOpen,
            items: [],
          },
        ],
      },
      {
        title: 'Testing Lab',
        icon: FlaskConical,
        subGroups: [
          { title: 'Overview', url: '/dashboard/testing-lab', icon: LayoutDashboard, items: [] },
          { title: 'Events', url: '/dashboard/testing-lab/events', icon: FlaskConical, items: [] },
          { title: 'Projects', url: '/dashboard/testing-lab/projects', icon: FolderKanban, items: [] },
          { title: 'Participants', url: '/dashboard/testing-lab/participants', icon: Users, items: [] },
          { title: 'Analytics', url: '/dashboard/testing-lab/analytics', icon: BarChart3, items: [] },
          { title: 'Settings', url: '/dashboard/testing-lab/settings', icon: Settings, items: [] },
        ],
      },
      {
        title: 'Launch Pad',
        url: '/dashboard/launch-pad',
        icon: Rocket,
      },
    ],
  },
];

export function flattenDashboardNavigationItems(groups: DashboardNavGroup[] = dashboardNavigationData): DashboardNavSubItem[] {
  const items: DashboardNavSubItem[] = [];

  for (const group of groups) {
    for (const item of group.items) {
      if (item.url && item.icon) {
        items.push({ title: item.title, url: item.url, icon: item.icon });
      }

      if (item.items?.length) {
        items.push(...item.items);
      }

      if (item.subGroups?.length) {
        for (const subGroup of item.subGroups) {
          if (subGroup.url && subGroup.icon) {
            items.push({ title: subGroup.title, url: subGroup.url, icon: subGroup.icon });
          }

          items.push(...subGroup.items);
        }
      }
    }
  }

  return items;
}

function NavGroups({ groups }: { groups: DashboardNavGroup[] }) {
  const pathname = usePathname();
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
        <SidebarGroup key={group.label}>
          <SidebarGroupLabel>{group.label}</SidebarGroupLabel>
          <SidebarGroupContent>
            <SidebarMenu>
              {group.items.map((item) => {
                const Icon = item.icon;
                const hasItems = item.items && item.items.length > 0;
                const hasSubGroups = item.subGroups && item.subGroups.length > 0;
                const isOpen = openItems.has(item.title);

                // Simple link item (no children)
                if (!hasItems && !hasSubGroups && item.url) {
                  const isActive = pathname === item.url || pathname?.endsWith(item.url);
                  return (
                    <SidebarMenuItem key={item.title}>
                      <SidebarMenuButton asChild isActive={isActive}>
                        <Link href={item.url}>
                          {Icon && <Icon className="size-4" />}
                          <span>{item.title}</span>
                        </Link>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  );
                }

                // Collapsible item with sub-items
                if (hasItems) {
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem>
                        <CollapsibleTrigger asChild>
                          <SidebarMenuButton>
                            {Icon && <Icon className="size-4" />}
                            <span>{item.title}</span>
                            <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90" />
                          </SidebarMenuButton>
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.items!.map((subItem) => {
                              const isActive = pathname === subItem.url || pathname?.endsWith(subItem.url);
                              return (
                                <SidebarMenuSubItem key={subItem.title}>
                                  <SidebarMenuSubButton asChild isActive={isActive}>
                                    <Link href={subItem.url}>
                                      <subItem.icon className="size-4" />
                                      <span>{subItem.title}</span>
                                      {subItem.badge && (
                                        <span className="ml-auto rounded-full bg-primary px-2 py-0.5 text-xs text-primary-foreground">{subItem.badge}</span>
                                      )}
                                    </Link>
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
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem>
                        <CollapsibleTrigger asChild>
                          <SidebarMenuButton>
                            {Icon && <Icon className="size-4" />}
                            <span>{item.title}</span>
                            <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90" />
                          </SidebarMenuButton>
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.subGroups!.map((subGroup) => {
                              const isActive = pathname === subGroup.url || pathname?.endsWith(subGroup.url ?? '');
                              const SubIcon = subGroup.icon;
                              return (
                                <SidebarMenuSubItem key={subGroup.title}>
                                  <SidebarMenuSubButton asChild isActive={isActive}>
                                    <Link href={subGroup.url || '#'}>
                                      {SubIcon && <SubIcon className="size-4" />}
                                      <span>{subGroup.title}</span>
                                    </Link>
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

export function DashboardSidebar(props: React.ComponentProps<typeof Sidebar>) {
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton asChild size="lg">
              <Link href="/dashboard">
                <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-primary text-sidebar-primary-foreground">
                  <Gamepad2 className="size-4" />
                </div>
                <div className="grid flex-1 text-left text-sm leading-tight">
                  <span className="truncate font-medium">GameGuild</span>
                  <span className="truncate text-xs text-sidebar-foreground/70">Creator dashboard</span>
                </div>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <NavGroups groups={dashboardNavigationData} />
      </SidebarContent>
      <SidebarFooter />
      <SidebarRail />
    </Sidebar>
  );
}
