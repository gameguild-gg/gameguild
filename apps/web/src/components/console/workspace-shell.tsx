'use client';

import { AppShellHeader } from '@/components/app/app-shell-header';
import { AppShellHeaderMenu } from '@/components/app/app-shell-header-menu';
import { AppShellSidebar } from '@/components/app/app-shell-sidebar';
import { AppShellContent, AppShellInset, AppShell } from '@/components/app/app-shell-layout';
import { Link, usePathname } from '@/i18n/navigation';
import type { DashboardContextSummary } from '@/lib/dashboard-contexts';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@game-guild/ui/components/breadcrumb';
import { Separator } from '@game-guild/ui/components/separator';
import { SidebarTrigger } from '@game-guild/ui/components/sidebar';
import { Toaster } from '@game-guild/ui/components/sonner';
import { House } from 'lucide-react';
import * as React from 'react';
import {
  filterWorkspaceNavigation,
  workspaceNavigationData,
} from './workspace-sidebar';
import { WorkspaceCommandPalette } from './workspace-command-palette';
import type { WorkspaceUser } from './workspace-user-menu';

const COURSE_ROUTE_PREFIX = ['dashboard', 'learning', 'courses'];

function buildBreadcrumbs(pathname: string): Array<{ label: string; href?: string }> {
  const paths = pathname
    .split('/')
    .filter(Boolean)
    .filter((segment) => !/^[a-z]{2}(?:-[A-Z]{2})?$/.test(segment));

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
}

/** The leading region of the workspace `AppShellHeader`: sidebar toggle + breadcrumbs. */
function WorkspaceHeaderLeading() {
  const pathname = usePathname();
  const isWorkspace = pathname?.startsWith('/workspace') ?? false;
  const breadcrumbs = pathname ? buildBreadcrumbs(pathname) : [];

  return (
    <div className="flex min-w-0 items-center gap-2">
      {isWorkspace ? (
        <SidebarTrigger className="md:hidden [&_svg]:size-5" />
      ) : (
        <SidebarTrigger className="[&_svg]:size-5" />
      )}
      {breadcrumbs.length > 0 && (
        <>
          {!isWorkspace && (
            <Separator orientation="vertical" className="mr-2 hidden data-[orientation=vertical]:h-4 sm:block" />
          )}
          <Breadcrumb aria-label="Dashboard breadcrumb" className="hidden min-w-0 flex-1 overflow-hidden sm:block">
            <BreadcrumbList className="flex-nowrap overflow-hidden">
              <BreadcrumbItem>
                {breadcrumbs[0]?.href ? (
                  <BreadcrumbLink render={<Link href={breadcrumbs[0].href} />}>
                    {breadcrumbs[0].label === 'Workspace' ? (
                      <House className="size-5" aria-hidden="true" />
                    ) : (
                      breadcrumbs[0].label
                    )}
                  </BreadcrumbLink>
                ) : (
                  <BreadcrumbPage>
                    {breadcrumbs[0].label === 'Workspace' ? (
                      <House className="size-5" aria-hidden="true" />
                    ) : (
                      breadcrumbs[0]?.label
                    )}
                  </BreadcrumbPage>
                )}
              </BreadcrumbItem>
              {breadcrumbs.length > 1 && <BreadcrumbSeparator />}
              {breadcrumbs.slice(1).map((item) => (
                <React.Fragment key={`${item.href ?? 'current'}:${item.label}`}>
                  <BreadcrumbItem>
                    {item.href ? (
                      <BreadcrumbLink className="max-w-24 truncate md:max-w-40 xl:max-w-64" render={<Link href={item.href} />}>
                        {item.label}
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
  );
}

interface WorkspaceShellProps {
  children: React.ReactNode;
  notifications?: DashboardNotificationSummary;
  user: WorkspaceUser;
  capabilities?: readonly string[];
  contexts?: readonly DashboardContextSummary[];
}

export function WorkspaceShell({
  children,
  notifications,
  user,
  capabilities = [],
}: WorkspaceShellProps) {
  const navigation = filterWorkspaceNavigation(
    workspaceNavigationData,
    capabilities,
  );

  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <a
        href="#dashboard-main"
        className="sr-only fixed left-4 top-4 z-50 rounded-md bg-background px-4 py-2 text-sm font-medium shadow-lg focus:not-sr-only"
      >
        Skip to main content
      </a>
      <AppShell>
        <AppShellSidebar navigation={navigation} notifications={notifications} />
        <AppShellInset>
          <WorkspaceCommandPalette
            navigation={navigation}
            capabilities={capabilities}
          />
          {/* Navbar */}
          <AppShellHeader>
            <WorkspaceHeaderLeading />
            <AppShellHeaderMenu user={user} notifications={notifications} />
          </AppShellHeader>

          {/* Page Content */}
          <AppShellContent>{children}</AppShellContent>
        </AppShellInset>
      </AppShell>
      <Toaster closeButton richColors position="top-right" />
    </div>
  );
}
