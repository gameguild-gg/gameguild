'use client';

import { ThemeToggle } from '@/components/ui/theme-toggle';
import { Link, usePathname } from '@/i18n/navigation';
import { DashboardUserMenu, type DashboardUser } from './dashboard-user-menu';
import { Badge } from '@game-guild/ui/components/badge';
import {
  Breadcrumb,
  BreadcrumbEllipsis,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@game-guild/ui/components/breadcrumb';
import { Button } from '@game-guild/ui/components/button';
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger,
} from '@game-guild/ui/components/drawer';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import { Input } from '@game-guild/ui/components/input';
import { Separator } from '@game-guild/ui/components/separator';
import { SidebarTrigger } from '@game-guild/ui/components/sidebar';
import { Bell, Command, Search } from 'lucide-react';
import * as React from 'react';
import type { DashboardNotificationItem, DashboardNotificationSummary } from '@/lib/dashboard-notifications';

const ITEMS_TO_DISPLAY = 3;

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
  const [open, setOpen] = React.useState(false);
  const notificationSummary = notifications ?? { items: [], unreadCount: 0 };
  const unreadLabel = notificationSummary.unreadCount > 99 ? '99+' : String(notificationSummary.unreadCount);

  // Generate breadcrumbs from pathname
  const generateBreadcrumbs = () => {
    if (!pathname) return [];

    const paths = pathname.split('/').filter(Boolean);

    // If we're at the root, don't show anything
    if (paths.length === 0) {
      return [];
    }

    const breadcrumbs: Array<{ label: string; href?: string }> = [];

    let currentPath = '';
    paths.forEach((path, index) => {
      currentPath += `/${path}`;
      const label = path.charAt(0).toUpperCase() + path.slice(1).replace(/-/g, ' ');

      // Last item shouldn't have href (it's the current page)
      if (index === paths.length - 1) {
        breadcrumbs.push({ label, href: undefined });
      } else {
        breadcrumbs.push({ label, href: currentPath });
      }
    });

    return breadcrumbs;
  };

  const breadcrumbs = generateBreadcrumbs();

  return (
    <header className="sticky top-0 z-40 flex h-16 shrink-0 items-center justify-between gap-2 border-b px-4">
      <div className="flex items-center gap-2">
        <SidebarTrigger />
        {breadcrumbs.length > 0 && (
          <>
            <Separator orientation="vertical" className="mr-2 data-[orientation=vertical]:h-4" />
            <Breadcrumb>
              <BreadcrumbList>
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
                {breadcrumbs.length > ITEMS_TO_DISPLAY ? (
                  <>
                    <BreadcrumbItem>
                      <DropdownMenu open={open} onOpenChange={setOpen}>
                        <DropdownMenuTrigger className="flex items-center gap-1" aria-label="Toggle menu">
                          <BreadcrumbEllipsis className="size-4" />
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="start">
                          {breadcrumbs.slice(1, -2).map((item, index) => (
                            <DropdownMenuItem key={index}>
                              <Link href={item.href ?? '#'}>{item.label}</Link>
                            </DropdownMenuItem>
                          ))}
                        </DropdownMenuContent>
                      </DropdownMenu>
                      <Drawer open={open} onOpenChange={setOpen}>
                        <DrawerTrigger aria-label="Toggle Menu">
                          <BreadcrumbEllipsis className="size-4" />
                        </DrawerTrigger>
                        <DrawerContent>
                          <DrawerHeader className="text-left">
                            <DrawerTitle>Navigate to</DrawerTitle>
                            <DrawerDescription>Select a page to navigate to.</DrawerDescription>
                          </DrawerHeader>
                          <div className="grid gap-1 px-4">
                            {breadcrumbs.slice(1, -2).map((item, index) => (
                              <Link key={index} href={item.href ?? '#'} className="py-1 text-sm" onClick={() => setOpen(false)}>
                                {item.label}
                              </Link>
                            ))}
                          </div>
                          <DrawerFooter className="pt-4">
                            <DrawerClose asChild>
                              <Button variant="outline">Close</Button>
                            </DrawerClose>
                          </DrawerFooter>
                        </DrawerContent>
                      </Drawer>
                    </BreadcrumbItem>
                    <BreadcrumbSeparator />
                  </>
                ) : null}
                {(breadcrumbs.length > ITEMS_TO_DISPLAY
                  ? breadcrumbs.slice(-ITEMS_TO_DISPLAY + 1)
                  : breadcrumbs.slice(1)
                ).map((item, index) => (
                  <React.Fragment key={index}>
                    <BreadcrumbItem>
                      {item.href ? (
                        <BreadcrumbLink asChild className="max-w-20 truncate md:max-w-none">
                          <Link href={item.href}>{item.label}</Link>
                        </BreadcrumbLink>
                      ) : (
                        <BreadcrumbPage className="max-w-20 truncate md:max-w-none">{item.label}</BreadcrumbPage>
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
      <div className="flex items-center gap-2">
        {/* Search */}
        <div className="relative ml-auto w-full max-w-sm">
          <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input type="search" placeholder="Search... (Ctrl+K)" className="pl-10 pr-4" />
          <kbd className="pointer-events-none absolute right-3 top-1/2 hidden h-5 -translate-y-1/2 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium opacity-100 sm:flex">
            <Command className="size-3" />
          </kbd>
        </div>
      </div>
      <div className="flex items-center gap-4">
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
