'use client';

import Link from 'next/link';
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

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Content Management</SidebarGroupLabel>
      <SidebarMenu>
        {items.map((item) => (
          <SidebarMenuItem key={item.name}>
            <SidebarMenuButton tooltip={item.name} asChild>
              <Link href={item.url}>
                <item.icon />
                <span>{item.name}</span>
              </Link>
            </SidebarMenuButton>
          </SidebarMenuItem>
        ))}
        
        {/* Testing Lab - Show dropdown when collapsed, collapsible when expanded */}
        {state === 'collapsed' ? (
          <SidebarMenuItem>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <SidebarMenuButton tooltip="Testing Lab">
                  <TestTube />
                  <span>Testing Lab</span>
                </SidebarMenuButton>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-56 rounded-lg" align="start" side={isMobile ? 'bottom' : 'right'} sideOffset={4}>
                <DropdownMenuLabel className="text-muted-foreground text-xs">Testing Lab</DropdownMenuLabel>
                {testingLabItems.map((subItem) => (
                  <DropdownMenuItem key={subItem.name} asChild>
                    <Link href={subItem.url} className="gap-2 p-2">
                      <subItem.icon className="size-4" />
                      {subItem.name}
                    </Link>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </SidebarMenuItem>
        ) : (
          <Collapsible asChild className="group/collapsible">
            <SidebarMenuItem>
              <CollapsibleTrigger asChild>
                <SidebarMenuButton tooltip="Testing Lab">
                  <TestTube />
                  <span>Testing Lab</span>
                  <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                </SidebarMenuButton>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub>
                  {testingLabItems.map((subItem) => (
                    <SidebarMenuSubItem key={subItem.name}>
                      <SidebarMenuSubButton asChild>
                        <Link href={subItem.url}>
                          <subItem.icon />
                          <span>{subItem.name}</span>
                        </Link>
                      </SidebarMenuSubButton>
                    </SidebarMenuSubItem>
                  ))}
                </SidebarMenuSub>
              </CollapsibleContent>
            </SidebarMenuItem>
          </Collapsible>
        )}
      </SidebarMenu>
    </SidebarGroup>
  );
}
