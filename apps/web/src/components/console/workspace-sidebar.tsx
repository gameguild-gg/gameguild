'use client';

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
import {
  BarChart3,
  BookOpen,
  CalendarDays,
  ChevronRight,
  ClipboardList,
  CircleDollarSign,
  FileText,
  FlaskConical,
  FolderOpen,
  HeadphonesIcon,
  Home,
  LayoutDashboard,
  List,
  FolderKanban,
  Rocket,
  Settings,
  ShieldCheck,
  UserCog,
  Users,
  type LucideIcon,
} from 'lucide-react';
import * as React from 'react';
import { GraduationCap } from 'lucide-react';
import { TenantSwitcher, type Tenant } from './tenant-switcher';

// Types for navigation structure
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
}

export interface WorkspaceNavGroup {
  label: string;
  items: WorkspaceNavGroupItem[];
}

// Game Guild Dashboard navigation structure
// Routes map to: /[locale]/(dashboard)/dashboard/...
export const workspaceNavigationData: WorkspaceNavGroup[] = [
  {
    label: 'Workspace',
    items: [
      {
        title: 'Home',
        url: '/workspace',
        icon: Home,
      },
      {
        title: 'Projects',
        url: '/workspace/projects',
        icon: FolderKanban,
      },
      {
        title: 'Teams',
        url: '/workspace/teams',
        icon: Users,
      },
      {
        title: 'Learning',
        icon: BookOpen,
        subGroups: [
          { title: 'Overview', url: '/workspace/learning', icon: LayoutDashboard, items: [] },
          { title: 'Courses', url: '/workspace/learning/courses', icon: BookOpen, items: [] },
          { title: 'Tutorials', url: '/workspace/learning/tutorials', icon: FileText, items: [] },
          { title: 'Resources', url: '/workspace/learning/resources', icon: FolderOpen, items: [] },
        ],
      },
    ],
  },
  {
    label: 'Community Management',
    items: [
      {
        title: 'Overview',
        url: '/console/community',
        icon: LayoutDashboard,
        requiredCapabilities: ['Community.Manage'],
      },
      {
        title: 'Members',
        icon: Users,
        requiredCapabilities: ['Community.ManageMembers'],
        subGroups: [
          {
            title: 'Overview',
            url: '/console/community/members',
            icon: LayoutDashboard,
            items: [],
            requiredCapabilities: ['Community.ManageMembers'],
          },
          {
            title: 'Users',
            url: '/console/community/members/users',
            icon: UserCog,
            items: [],
            requiredCapabilities: ['Community.ManageMembers'],
          },
          {
            title: 'Groups',
            url: '/console/community/members/groups',
            icon: Users,
            items: [],
            requiredCapabilities: ['Community.ManageMembers'],
          },
          {
            title: 'Support',
            url: '/console/community/members/support',
            icon: HeadphonesIcon,
            items: [],
            requiredCapabilities: ['Community.ManageSupport'],
          },
        ],
      },
      {
        title: 'Teams',
        url: '/console/community/teams',
        icon: Users,
        requiredCapabilities: ['Community.ManageTeams'],
      },
      {
        title: 'Projects',
        url: '/console/community/projects',
        icon: FolderKanban,
        requiredCapabilities: ['Community.ManageProjects'],
      },
      {
        title: 'Testing Lab',
        icon: FlaskConical,
        requiredCapabilities: [
          'TestingLab.ManageEvents',
          'TestingLab.ReviewApplications',
          'TestingLab.ManageParticipants',
          'TestingLab.ManageFeedback',
          'TestingLab.ViewAnalytics',
          'TestingLab.ManageSettings',
        ],
        subGroups: [
          {
            title: 'Calendar',
            url: '/workspace/testing-lab',
            icon: CalendarDays,
            items: [],
            requiredCapabilities: [
              'TestingLab.ManageEvents',
              'TestingLab.ReviewApplications',
              'TestingLab.ManageParticipants',
              'TestingLab.ManageFeedback',
              'TestingLab.ViewAnalytics',
              'TestingLab.ManageSettings',
            ],
          },
          {
            title: 'Sessions',
            url: '/workspace/testing-lab/events',
            icon: List,
            items: [],
            requiredCapabilities: ['TestingLab.ManageEvents'],
          },
          {
            title: 'Settings',
            url: '/workspace/testing-lab/settings',
            icon: Settings,
            items: [],
            requiredCapabilities: [
              'TestingLab.ManageSettings',
              'TestingLab.ViewAnalytics',
            ],
          },
        ],
      },
      {
        title: 'Launch Pad',
        url: '/console/community/launch-pad',
        icon: Rocket,
        requiredCapabilities: [
          'LaunchPad.ManageEvents',
          'LaunchPad.ReviewApplications',
          'LaunchPad.ManageParticipants',
          'LaunchPad.ViewAnalytics',
          'LaunchPad.ManageSettings',
        ],
        subGroups: [
          {
            title: 'Overview',
            url: '/console/community/launch-pad',
            icon: LayoutDashboard,
            items: [],
            requiredCapabilities: [
              'LaunchPad.ManageEvents',
              'LaunchPad.ReviewApplications',
              'LaunchPad.ManageParticipants',
              'LaunchPad.ViewAnalytics',
              'LaunchPad.ManageSettings',
            ],
          },
          {
            title: 'Events',
            url: '/console/community/launch-pad/events',
            icon: Rocket,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageEvents'],
          },
          {
            title: 'Applications',
            url: '/console/community/launch-pad/applications',
            icon: ClipboardList,
            items: [],
            requiredCapabilities: ['LaunchPad.ReviewApplications'],
          },
          {
            title: 'Participants',
            url: '/console/community/launch-pad/participants',
            icon: Users,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageParticipants'],
          },
          {
            title: 'Analytics',
            url: '/console/community/launch-pad/analytics',
            icon: BarChart3,
            items: [],
            requiredCapabilities: ['LaunchPad.ViewAnalytics'],
          },
          {
            title: 'Settings',
            url: '/console/community/launch-pad/settings',
            icon: Settings,
            items: [],
            requiredCapabilities: ['LaunchPad.ManageSettings'],
          },
        ],
      },
      {
        title: 'Learning',
        icon: BookOpen,
        requiredCapabilities: ['Learning.Manage'],
        subGroups: [
          {
            title: 'Overview',
            url: '/console/learning',
            icon: LayoutDashboard,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Courses',
            url: '/console/learning/courses',
            icon: BookOpen,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Tutorials',
            url: '/console/learning/tutorials',
            icon: FileText,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
          {
            title: 'Resources',
            url: '/console/learning/resources',
            icon: FolderOpen,
            items: [],
            requiredCapabilities: ['Learning.Manage'],
          },
        ],
      },
    ],
  },
  {
    label: 'Platform Management',
    items: [
      {
        title: 'Economy',
        icon: CircleDollarSign,
        requiredCapabilities: ['Economy.ManagePayouts'],
        subGroups: [
          {
            title: 'Payout review',
            url: '/console/economy/payout-reviews',
            icon: ClipboardList,
            items: [],
            requiredCapabilities: ['Economy.ManagePayouts'],
          },
        ],
      },
      {
        title: 'Roles',
        url: '/console/platform/roles',
        icon: ShieldCheck,
        requiredCapabilities: ['Platform.ManageRoles'],
      },
    ],
  },
];

function hasAnyCapability(
  requiredCapabilities: readonly string[] | undefined,
  capabilities: ReadonlySet<string>,
): boolean {
  return (
    !requiredCapabilities?.length ||
    requiredCapabilities.some((capability) => capabilities.has(capability))
  );
}

export function filterWorkspaceNavigation(
  groups: WorkspaceNavGroup[],
  actorCapabilities: readonly string[],
): WorkspaceNavGroup[] {
  const capabilities = new Set(actorCapabilities);

  return groups.flatMap((group) => {
    const items = group.items.flatMap((item) => {
      if (!hasAnyCapability(item.requiredCapabilities, capabilities)) return [];

      const nestedItems = item.items?.filter((nested) =>
        hasAnyCapability(nested.requiredCapabilities, capabilities),
      );
      const subGroups = item.subGroups?.filter((nested) =>
        hasAnyCapability(nested.requiredCapabilities, capabilities),
      );

      if (item.items?.length && !nestedItems?.length) return [];
      if (item.subGroups?.length && !subGroups?.length) return [];

      return [{ ...item, items: nestedItems, subGroups }];
    });

    return items.length ? [{ ...group, items }] : [];
  });
}

export function flattenWorkspaceNavigationItems(groups: WorkspaceNavGroup[] = workspaceNavigationData): WorkspaceNavSubItem[] {
  const items: WorkspaceNavSubItem[] = [];

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
            <SidebarMenu className="gap-2">
              {group.items.map((item) => {
                const Icon = item.icon;
                const hasItems = item.items && item.items.length > 0;
                const hasSubGroups = item.subGroups && item.subGroups.length > 0;
                const isOpen = openItems.has(item.title);

                // Simple link item (no children)
                if (!hasItems && !hasSubGroups && item.url) {
                  const isActive = pathname === item.url || pathname?.endsWith(item.url);
                  return (
                    <SidebarMenuItem key={item.title} className="group-data-[collapsible=icon]:mx-auto group-data-[collapsible=icon]:w-8">
                      <SidebarMenuButton isActive={isActive} tooltip={item.title} className="[&_svg]:size-5" render={<Link href={item.url} />}>
                        {Icon && <Icon className="size-5" />}
                        <span>{item.title}</span>
                        {notificationCounts?.[item.url] ? (
                          <NotificationChip count={notificationCounts[item.url]} />
                        ) : null}
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  );
                }

                // Collapsible item with sub-items
                if (hasItems) {
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem>
                        <CollapsibleTrigger render={<SidebarMenuButton tooltip={item.title} className="[&_svg]:size-5" />}>
                          {Icon && <Icon className="size-5" />}
                          <span>{item.title}</span>
                          <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90" />
                        </CollapsibleTrigger>
                        <CollapsibleContent>
                          <SidebarMenuSub>
                            {item.items!.map((subItem) => {
                              const isActive = pathname === subItem.url || pathname?.endsWith(subItem.url);
                              return (
                                <SidebarMenuSubItem key={subItem.title}>
                                  <SidebarMenuSubButton isActive={isActive} className="[&_svg]:size-5" render={<Link href={subItem.url} />}>
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
                  return (
                    <Collapsible key={item.title} open={isOpen} onOpenChange={() => toggleItem(item.title)} className="group/collapsible">
                      <SidebarMenuItem>
                        <CollapsibleTrigger render={<SidebarMenuButton tooltip={item.title} className="[&_svg]:size-5" />}>
                          {Icon && <Icon className="size-5" />}
                          <span>{item.title}</span>
                          <ChevronRight className="ml-auto size-4 transition-transform group-data-[state=open]/collapsible:rotate-90" />
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
                                  <SidebarMenuSubButton isActive={isActive} className="[&_svg]:size-5" render={<Link href={subGroup.url || '#'} />}>
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

interface WorkspaceSidebarProps extends React.ComponentProps<typeof Sidebar> {
  navigation?: WorkspaceNavGroup[];
  notifications?: DashboardNotificationSummary;
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

export function WorkspaceSidebar({
  navigation = filterWorkspaceNavigation(workspaceNavigationData, []),
  notifications,
  ...props
}: WorkspaceSidebarProps) {
  const notificationCounts = countNotificationsByUrl(notifications, navigation);
  return (
    <Sidebar collapsible="icon" {...props}>
      <SidebarHeader className="h-16 p-2">
        <div className="flex h-full items-center justify-between gap-2">
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
