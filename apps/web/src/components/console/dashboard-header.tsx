'use client';

import { ThemeToggle } from '@/components/ui/theme-toggle';
import { Link, usePathname } from '@/i18n/navigation';
import { DashboardUserMenu, type DashboardUser } from './dashboard-user-menu';
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
import { SidebarTrigger } from '@game-guild/ui/components/sidebar';
import { Bell, Command, Search } from 'lucide-react';
import * as React from 'react';
import type { DashboardNotificationItem, DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { openDashboardCommandPalette } from './dashboard-command-palette';

const COURSE_ROUTE_PREFIX = ['dashboard', 'learning', 'courses'];

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

interface DashboardHeaderProps {
  notifications?: DashboardNotificationSummary;
  user: DashboardUser;
}

export function DashboardHeader({ notifications, user }: DashboardHeaderProps) {
  const pathname = usePathname();
  const notificationSummary = notifications ?? { items: [], unreadCount: 0 };
  const unreadLabel = notificationSummary.unreadCount > 99 ? '99+' : String(notificationSummary.unreadCount);

  // Generate breadcrumbs from pathname
  const generateBreadcrumbs = () => {
    if (!pathname) return [];

    const paths = pathname
      .split('/')
      .filter(Boolean)
      .filter((segment) => !/^[a-z]{2}(?:-[A-Z]{2})?$/.test(segment));

    // If we're at the root, don't show anything
    if (paths.length === 0) {
      return [];
    }

    const breadcrumbs: Array<{ label: string; href?: string }> = [];

    let currentPath = '';
    paths.forEach((path, index) => {
      currentPath += `/${path}`;
      const label = path
        .replace(/-/g, ' ')
        .replace(/\b\w/g, (char) => char.toUpperCase());

      // Last item shouldn't have href (it's the current page)
      if (index === paths.length - 1) {
        breadcrumbs.push({ label, href: undefined });
      } else {
        breadcrumbs.push({ label, href: currentPath });
      }
    });

    if (paths.length >= 5 && COURSE_ROUTE_PREFIX.every((segment, index) => paths[index] === segment)) {
      return breadcrumbs.slice(0, 3).concat(breadcrumbs.slice(-2));
    }

    return breadcrumbs.slice(-5);
  };

  const breadcrumbs = generateBreadcrumbs();

  return (
    <header className="sticky top-0 z-40 grid h-16 min-w-0 shrink-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-2 border-b px-3 sm:px-4 xl:grid-cols-[minmax(0,1fr)_minmax(16rem,32rem)_minmax(0,1fr)]">
      <div className="flex min-w-0 items-center gap-2">
        <SidebarTrigger />
        {breadcrumbs.length > 0 && (
          <>
            <Separator orientation="vertical" className="mr-2 hidden data-[orientation=vertical]:h-4 sm:block" />
            <Breadcrumb aria-label="Dashboard breadcrumb" className="hidden min-w-0 flex-1 overflow-hidden sm:block">
              <BreadcrumbList className="flex-nowrap overflow-hidden">
                <BreadcrumbItem>
                  {breadcrumbs[0]?.href ? (
                    <BreadcrumbLink asChild>
                      <Link href={breadcrumbs[0].href}>{breadcrumbs[0].label}</Link>
                    </BreadcrumbLink>
                  ) : (
                    <BreadcrumbPage>{breadcrumbs[0]?.label}</BreadcrumbPage>
                  )}
                </BreadcrumbItem>
                {breadcrumbs.length > 1 && <BreadcrumbSeparator />}
                {breadcrumbs.slice(1).map((item) => (
                  <React.Fragment key={`${item.href ?? 'current'}:${item.label}`}>
                    <BreadcrumbItem>
                      {item.href ? (
                        <BreadcrumbLink asChild className="max-w-24 truncate md:max-w-40 xl:max-w-64">
                          <Link href={item.href}>{item.label}</Link>
                        </BreadcrumbLink>
                      ) : (
                        <BreadcrumbPage className="max-w-24 truncate md:max-w-40 xl:max-w-64">{item.label}</BreadcrumbPage>
                      )}
                    </BreadcrumbItem>
                    {item.href && <BreadcrumbSeparator />}
                  </React.Fragment>
                ))}
              </BreadcrumbList>
            </Breadcrumb>
          </>
        )}
      </div>
      <div className="hidden min-w-0 items-center justify-center xl:flex">
        {/* Search */}
        <button
          type="button"
          onClick={openDashboardCommandPalette}
          className="relative flex h-10 w-full max-w-sm items-center rounded-md border bg-background px-3 text-left text-sm text-muted-foreground transition-colors hover:bg-muted/50 lg:max-w-md"
          aria-label="Search dashboard"
        >
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <span className="min-w-0 flex-1 truncate pl-7 pr-16">Search dashboard...</span>
          <kbd className="pointer-events-none absolute right-3 top-1/2 hidden h-5 -translate-y-1/2 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium opacity-100 sm:flex">
            <Command className="size-3" />
          </kbd>
        </button>
      </div>
      <div className="flex shrink-0 items-center justify-end gap-1 sm:gap-2">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="xl:hidden"
          onClick={openDashboardCommandPalette}
          aria-label="Search dashboard"
        >
          <Search className="size-5" />
        </Button>

        {/* Theme Toggle */}
        <ThemeToggle />

        {/* Notifications */}
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button variant="ghost" size="icon" className="relative">
              <Bell className="size-5" />
              {notificationSummary.unreadCount > 0 && (
                <Badge variant="destructive" className="absolute -right-1 -top-1 h-5 min-w-5 rounded-full px-1 text-xs">
                  {unreadLabel}
                </Badge>
              )}
              <span className="sr-only">Notifications</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-80">
            <DropdownMenuLabel>Notifications</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <div className="max-h-[300px] overflow-y-auto">
              {notificationSummary.items.length > 0 ? (
                notificationSummary.items.map((item) => <NotificationMenuItem key={item.id} item={item} />)
              ) : (
                <div className="px-3 py-6 text-center">
                  <p className="text-sm font-medium">No notifications</p>
                  <p className="mt-1 text-xs text-muted-foreground">New account updates will appear here.</p>
                </div>
              )}
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuLabel className="text-xs font-normal text-muted-foreground">
              Showing latest account notifications
            </DropdownMenuLabel>
          </DropdownMenuContent>
        </DropdownMenu>

        <DashboardUserMenu user={user} />
      </div>
    </header>
  );
}
