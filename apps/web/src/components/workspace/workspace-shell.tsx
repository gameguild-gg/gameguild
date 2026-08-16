'use client';

import { Bell, LayoutDashboard, LogOut, Settings } from 'lucide-react';
import * as React from 'react';
import { usePathname, useRouter } from 'next/navigation';

import { Toaster } from '@/components/ui/sonner';
import { WorkspaceSearch } from '@/components/workspace/workspace-search';
import { WorkspaceSidebar, type WorkspaceTeamSummary } from '@/components/workspace/workspace-sidebar';
import { Link } from '@/i18n/navigation';
import { useAuth } from '@game-guild/client/react';
import { Avatar, AvatarFallback } from '@game-guild/ui/components/avatar';
import { Badge } from '@game-guild/ui/components/badge';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@game-guild/ui/components/breadcrumb';
import { Button } from '@game-guild/ui/components/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { Separator } from '@game-guild/ui/components/separator';
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@game-guild/ui/components/sidebar';
import type { DashboardNotificationItem, DashboardNotificationSummary } from '@/lib/dashboard-notifications';

export interface WorkspaceUser {
  name: string;
  email: string;
  initials: string;
  canManage: boolean;
}

function NotificationMenuItem({ item }: { item: DashboardNotificationItem }) {
  const content = (
    <div className="flex w-full flex-col gap-1">
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium">{item.title}</p>
        {!item.isRead && <span className="mt-1 size-2 rounded-full bg-primary" aria-label="Unread" />}
      </div>
      <p className="line-clamp-2 text-xs text-muted-foreground">{item.message}</p>
      <p className="text-xs text-muted-foreground">{item.createdLabel}</p>
      {item.actionText && <p className="text-xs font-medium text-primary">{item.actionText}</p>}
    </div>
  );

  if (item.actionUrl?.startsWith('/')) {
    return (
      <DropdownMenuItem asChild>
        <Link href={item.actionUrl}>{content}</Link>
      </DropdownMenuItem>
    );
  }

  return <DropdownMenuItem>{content}</DropdownMenuItem>;
}

function WorkspaceBreadcrumbs() {
  const pathname = usePathname() ?? '';
  const crumbs = pathname
    .split('/')
    .filter((p) => p && !/^[a-z]{2}(-[A-Z]{2})?$/.test(p))
    .slice(1);
  const label = (value: string) =>
    value.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());

  return (
    <>
      <Separator orientation="vertical" className="mr-2 hidden h-4 sm:block" />
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            {crumbs.length === 0 ? (
              <BreadcrumbPage>Home</BreadcrumbPage>
            ) : (
              <BreadcrumbLink asChild>
                <Link href="/workspace">Home</Link>
              </BreadcrumbLink>
            )}
          </BreadcrumbItem>
          {crumbs.map((crumb, index) => (
            <React.Fragment key={`${crumb}:${index}`}>
              <BreadcrumbSeparator />
              <BreadcrumbItem>
                {index === crumbs.length - 1 ? (
                  <BreadcrumbPage>{label(crumb)}</BreadcrumbPage>
                ) : (
                  <BreadcrumbLink asChild>
                    <Link href={`/workspace/${crumbs.slice(0, index + 1).join('/')}`}>
                      {label(crumb)}
                    </Link>
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </React.Fragment>
          ))}
        </BreadcrumbList>
      </Breadcrumb>
    </>
  );
}

function WorkspaceUserMenu({ user }: { user: WorkspaceUser }) {
  const router = useRouter();
  const { signOut } = useAuth();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className="h-auto min-w-0 gap-2 rounded-full p-1 pr-3"
          aria-label={`Open ${user.name} account menu`}
        >
          <Avatar size="sm">
            <AvatarFallback className="bg-primary text-xs font-bold text-primary-foreground">
              {user.initials}
            </AvatarFallback>
          </Avatar>
          <span className="hidden max-w-36 truncate text-sm font-medium lg:inline">{user.name}</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <p className="truncate text-sm font-medium">{user.name}</p>
          {user.email && <p className="truncate text-xs text-muted-foreground">{user.email}</p>}
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        {user.canManage && (
          <DropdownMenuItem asChild>
            <Link href="/dashboard">
              <LayoutDashboard className="size-4" />
              Console
            </Link>
          </DropdownMenuItem>
        )}
        <DropdownMenuItem asChild>
          <Link href="/workspace/settings/account">
            <Settings className="size-4" />
            Account settings
          </Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={(event) => {
            event.preventDefault();
            void signOut({ redirect: false }).then(() => router.push('/sign-in'));
          }}
        >
          <LogOut className="size-4" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * Member application shell for /workspace — visual twin of the console:
 * collapsible sidebar (team switcher + flat nav), header with breadcrumbs,
 * search, notifications and the user menu.
 */
export function WorkspaceShell({
  children,
  user,
  teams,
  notifications,
}: {
  children: React.ReactNode;
  user: WorkspaceUser;
  teams: readonly WorkspaceTeamSummary[];
  notifications?: DashboardNotificationSummary;
}): React.JSX.Element {
  const notificationSummary = notifications ?? { items: [], unreadCount: 0 };
  const unreadLabel =
    notificationSummary.unreadCount > 99 ? '99+' : String(notificationSummary.unreadCount);

  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <SidebarProvider>
        <WorkspaceSidebar teams={teams} />
        <SidebarInset className="min-w-0 overflow-hidden">
          <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
            <header className="sticky top-0 z-40 grid h-16 min-w-0 shrink-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-2 border-b px-3 sm:px-4">
              <div className="flex min-w-0 items-center gap-2">
                <SidebarTrigger />
                <WorkspaceBreadcrumbs />
              </div>
              <div className="flex items-center gap-1">
                <WorkspaceSearch />

                <DropdownMenu>
                  <DropdownMenuTrigger asChild>
                    <Button variant="ghost" size="icon" className="relative" aria-label="Notifications">
                      <Bell className="size-5" />
                      {notificationSummary.unreadCount > 0 && (
                        <Badge variant="destructive" className="absolute -right-1 -top-1 h-5 min-w-5 rounded-full px-1 text-xs">
                          {unreadLabel}
                        </Badge>
                      )}
                    </Button>
                  </DropdownMenuTrigger>
                  <DropdownMenuContent align="end" className="w-80">
                    <DropdownMenuLabel>Notifications</DropdownMenuLabel>
                    <DropdownMenuSeparator />
                    <div className="max-h-[300px] overflow-y-auto">
                      {notificationSummary.items.length > 0 ? (
                        notificationSummary.items.map((item) => (
                          <NotificationMenuItem key={item.id} item={item} />
                        ))
                      ) : (
                        <div className="px-3 py-6 text-center">
                          <p className="text-sm font-medium">No notifications</p>
                          <p className="mt-1 text-xs text-muted-foreground">
                            New account updates will appear here.
                          </p>
                        </div>
                      )}
                    </div>
                    <DropdownMenuSeparator />
                    <DropdownMenuLabel className="text-xs font-normal text-muted-foreground">
                      Showing latest account notifications
                    </DropdownMenuLabel>
                  </DropdownMenuContent>
                </DropdownMenu>

                <WorkspaceUserMenu user={user} />
              </div>
            </header>

            <div className="min-w-0 flex-1 overflow-y-auto overflow-x-hidden bg-muted/30 p-4 transition-all duration-300 sm:p-6">
              {children}
            </div>
          </div>
        </SidebarInset>
      </SidebarProvider>
      <Toaster closeButton richColors position="top-right" />
    </div>
  );
}
