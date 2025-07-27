'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { ChevronRight, TestTube, type LucideIcon } from 'lucide-react';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  useSidebar,
} from '@/components/ui/sidebar';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuTrigger } from '@/components/ui/dropdown-menu';

export function ContentManagementSidebarContent({
  items,
  testingLabItems,
}: {
  items: {
    name: string;
    url: string;
    icon: LucideIcon;
  }[];
  testingLabItems: {
    name: string;
    url: string;
    icon: LucideIcon;
  }[];
}) {
  const { isMobile, state } = useSidebar();
  const pathname = usePathname();

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Content Management</SidebarGroupLabel>
      <SidebarMenu>
        {items.map((item) => {
          const isActive = pathname === item.url || pathname.startsWith(item.url + '/');

          return (
            <SidebarMenuItem key={item.name}>
              <SidebarMenuButton tooltip={item.name} isActive={isActive} size="lg" asChild>
                <Link href={item.url}>
                  <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                    <item.icon className="size-4" />
                  </div>
                  <div className="grid flex-1 text-left text-sm leading-tight">
                    <span className="truncate font-semibold">{item.name}</span>
                    {/* <span className="truncate text-xs">Content</span> */}
                  </div>
                </Link>
              </SidebarMenuButton>
            </SidebarMenuItem>
          );
        })}

        {/* Testing Lab - Show dropdown when collapsed, collapsible when expanded */}
        {state === 'collapsed' ? (
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton
                  tooltip="Testing Lab"
                  isActive={testingLabItems.some((item) => pathname === item.url || pathname.startsWith(item.url + '/'))}
                  size="lg"
                >
                  <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                    <TestTube className="size-4" />
                  </div>
                  <div className="grid flex-1 text-left text-sm leading-tight">
                    <span className="truncate font-semibold">Testing Lab</span>
                    {/* <span className="truncate text-xs">Development</span> */}
                  </div>
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-56 rounded-lg" align="start" side={isMobile ? 'bottom' : 'right'} sideOffset={4}>
                <DropdownMenuLabel className="text-muted-foreground text-xs">Testing Lab</DropdownMenuLabel>
                {testingLabItems.map((subItem) => {
                  const isSubItemActive = pathname === subItem.url || pathname.startsWith(subItem.url + '/');

                  return (
                    <DropdownMenuItem key={subItem.name} asChild>
                      <Link href={subItem.url} className={`gap-2 p-2 ${isSubItemActive ? 'bg-sidebar-accent' : ''}`}>
                        <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                          <subItem.icon className="size-4" />
                        </div>
                        <div className="grid flex-1 text-left text-sm leading-tight">
                          <span className="truncate font-semibold">{subItem.name}</span>
                          {/* <span className="truncate text-xs">Testing Lab</span> */}
                        </div>
                      </Link>
                    </DropdownMenuItem>
                  );
                })}
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        ) : (
          <Collapsible asChild className="group/collapsible">
            <SidebarMenuItem>
              <CollapsibleTrigger asChild>
                <SidebarMenuButton
                  tooltip="Testing Lab"
                  isActive={testingLabItems.some((item) => pathname === item.url || pathname.startsWith(item.url + '/'))}
                  size="lg"
                >
                  <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                    <TestTube className="size-4" />
                  </div>
                  <div className="grid flex-1 text-left text-sm leading-tight">
                    <span className="truncate font-semibold">Testing Lab</span>
                    {/* <span className="truncate text-xs">Development</span> */}
                  </div>
                  <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                </SidebarMenuButton>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub>
                  {testingLabItems.map((subItem) => {
                    const isSubItemActive = pathname === subItem.url || pathname.startsWith(subItem.url + '/');

                    return (
                      <SidebarMenuSubItem key={subItem.name}>
                        <SidebarMenuSubButton isActive={isSubItemActive} asChild>
                          <Link href={subItem.url}>
                            <div className="flex aspect-square size-8 items-center justify-center rounded-lg bg-sidebar-accent text-sidebar-accent-foreground">
                              <subItem.icon className="size-4" />
                            </div>
                            <div className="grid flex-1 text-left text-sm leading-tight">
                              <span className="truncate font-semibold">{subItem.name}</span>
                              {/* <span className="truncate text-xs">Testing Lab</span> */}
                            </div>
                          </Link>
                        </SidebarMenuSubButton>
                      </SidebarMenuSubItem>
                    );
                  })}
                </SidebarMenuSub>
              </CollapsibleContent>
            </SidebarMenuItem>
          </Collapsible>
        )}
      </SidebarMenu>
    </SidebarGroup>
  );
}
