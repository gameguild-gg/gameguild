'use client';

import * as React from 'react';
import { usePathname } from 'next/navigation';
import Link from 'next/link';
import {
  ChevronRight,
  LayoutDashboard,
  type LucideIcon,
  Users,
  Layers,
  CreditCard,
  Building2,
  UserCog,
  HeadphonesIcon,
  BadgeDollarSign,
  BookOpen,
  FileText,
  FolderOpen,
  Gamepad2,
  GraduationCap,
} from 'lucide-react';
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
import { TenantSwitcher, type Tenant } from './tenant-switcher';

// Sample tenants data
const sampleTenants: Tenant[] = [
  {
    id: '1',
    name: 'Game Guild',
    logo: Gamepad2,
    plan: 'Enterprise',
  },
  {
    id: '2',
    name: 'Indie Studio',
    logo: Layers,
    plan: 'Startup',
  },
  {
    id: '3',
    name: 'Learning Hub',
    logo: GraduationCap,
    plan: 'Free',
  },
];

// Types for navigation structure
interface NavSubItem {
  title: string;
  url: string;
  icon: LucideIcon;
  isActive?: boolean;
  badge?: string;
}

interface NavItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items: NavSubItem[];
}

interface NavGroupItem {
  title: string;
  url?: string;
  icon?: LucideIcon;
  items?: NavSubItem[];
  subGroups?: NavItem[];
}

interface NavGroup {
  label: string;
  items: NavGroupItem[];
}

// Game Guild Dashboard navigation structure
// Routes map to: /[locale]/(dashboard)/dashboard/...
const navigationData: NavGroup[] = [
  {
    label: 'Overview',
    items: [
      {
        title: 'Dashboard',
        url: '/dashboard',
        icon: LayoutDashboard,
      },
    ],
  },
  {
    label: 'Platform Management',
    items: [
      {
        title: 'Overview',
        url: '/dashboard/platform',
        icon: LayoutDashboard,
      },
      {
        title: 'Subscription Plans',
        url: '/dashboard/platform/subscriptions',
        icon: CreditCard,
      },
      {
        title: 'Customers',
        icon: Building2,
        subGroups: [
          {
            title: 'Overview',
            url: '/dashboard/platform/customers',
            icon: LayoutDashboard,
            items: [],
          },
          {
            title: 'Accounts',
            url: '/dashboard/platform/customers/accounts',
            icon: UserCog,
            items: [],
          },
          {
            title: 'Support',
            url: '/dashboard/platform/customers/support',
            icon: HeadphonesIcon,
            items: [],
          },
        ],
      },
      {
        title: 'Billing & Revenue',
        url: '/dashboard/platform/billing',
        icon: BadgeDollarSign,
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
      {
        title: 'Learning',
        icon: BookOpen,
        items: [
          {
            title: 'Overview',
            url: '/dashboard/learning',
            icon: LayoutDashboard,
          },
          {
            title: 'Courses',
            url: '/dashboard/learning/courses',
            icon: BookOpen,
          },
          {
            title: 'Tutorials',
            url: '/dashboard/learning/tutorials',
            icon: FileText,
          },
          {
            title: 'Resources',
            url: '/dashboard/learning/resources',
            icon: FolderOpen,
          },
        ],
      },
    ],
  },
];

function NavGroups({ groups }: { groups: NavGroup[] }) {
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
                    <Collapsible
                      key={item.title}
                      open={isOpen}
                      onOpenChange={() => toggleItem(item.title)}
                      className="group/collapsible"
                    >
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
                                        <span className="ml-auto rounded-full bg-primary px-2 py-0.5 text-xs text-primary-foreground">
                                          {subItem.badge}
                                        </span>
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
                    <Collapsible
                      key={item.title}
                      open={isOpen}
                      onOpenChange={() => toggleItem(item.title)}
                      className="group/collapsible"
                    >
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
        <TenantSwitcher tenants={sampleTenants} />
      </SidebarHeader>
      <SidebarContent className="gap-0">
        <NavGroups groups={navigationData} />
      </SidebarContent>
      <SidebarFooter />
      <SidebarRail />
    </Sidebar>
  );
}
