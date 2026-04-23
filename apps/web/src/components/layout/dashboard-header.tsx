'use client';

import { ThemeToggle } from '@/components/ui/theme-toggle';
import { Link, usePathname } from '@/i18n/navigation';
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

const ITEMS_TO_DISPLAY = 3;

export function DashboardHeader() {
  const pathname = usePathname();
  const [open, setOpen] = React.useState(false);

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
              <Badge variant="destructive" className="absolute -right-1 -top-1 size-5 rounded-full p-0 text-xs">
                3
              </Badge>
              <span className="sr-only">Notifications</span>
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end" className="w-80">
            <DropdownMenuLabel>Notifications</DropdownMenuLabel>
            <DropdownMenuSeparator />
            <div className="max-h-[300px] overflow-y-auto">
              <DropdownMenuItem>
                <div className="flex flex-col gap-1">
                  <p className="text-sm font-medium">New project submitted</p>
                  <p className="text-xs text-muted-foreground">A new game project was submitted for review</p>
                  <p className="text-xs text-muted-foreground">2 minutes ago</p>
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem>
                <div className="flex flex-col gap-1">
                  <p className="text-sm font-medium">Achievement unlocked</p>
                  <p className="text-xs text-muted-foreground">You earned the &quot;First Commit&quot; badge</p>
                  <p className="text-xs text-muted-foreground">1 hour ago</p>
                </div>
              </DropdownMenuItem>
              <DropdownMenuItem>
                <div className="flex flex-col gap-1">
                  <p className="text-sm font-medium">Team invitation</p>
                  <p className="text-xs text-muted-foreground">You were invited to join &quot;Indie Devs&quot;</p>
                  <p className="text-xs text-muted-foreground">3 hours ago</p>
                </div>
              </DropdownMenuItem>
            </div>
            <DropdownMenuSeparator />
            <DropdownMenuItem asChild>
              <Link href="/notifications" className="w-full text-center">
                View all notifications
              </Link>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </header>
  );
}
