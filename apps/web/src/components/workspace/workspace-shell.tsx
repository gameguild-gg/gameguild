'use client';

import { GraduationCap, LayoutDashboard, LogOut, Settings } from 'lucide-react';
import { usePathname, useRouter } from 'next/navigation';

import { Toaster } from '@/components/ui/sonner';
import { WorkspaceSidebar, type WorkspaceTeamSummary } from '@/components/workspace/workspace-sidebar';
import { Link } from '@/i18n/navigation';
import { useAuth } from '@game-guild/client/react';
import { Avatar, AvatarFallback } from '@game-guild/ui/components/avatar';
import { Breadcrumb, BreadcrumbItem, BreadcrumbList, BreadcrumbPage } from '@game-guild/ui/components/breadcrumb';
import { Button } from '@game-guild/ui/components/button';
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger } from '@game-guild/ui/components/dropdown-menu';
import { Separator } from '@game-guild/ui/components/separator';
import { SidebarInset, SidebarProvider, SidebarTrigger } from '@game-guild/ui/components/sidebar';

export interface WorkspaceUser {
  name: string;
  email: string;
  initials: string;
  canManage: boolean;
}

function WorkspaceBreadcrumbs() {
  const pathname = usePathname() ?? '';
  const paths = pathname.split('/').filter((p) => p && !/^[a-z]{2}(-[A-Z]{2})?$/.test(p));
  const label = paths.length > 0 ? paths[paths.length - 1].replace(/-/g, ' ') : 'Hub';

  return (
    <>
      <Separator orientation="vertical" className="mr-2 hidden h-4 sm:block" />
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbPage className="capitalize">{label}</BreadcrumbPage>
          </BreadcrumbItem>
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
        <Button variant="ghost" className="h-auto min-w-0 gap-2 rounded-full p-1 pr-3" aria-label={`Open ${user.name} account menu`}>
          <Avatar size="sm">
            <AvatarFallback className="bg-primary text-xs font-bold text-primary-foreground">{user.initials}</AvatarFallback>
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
            <Link href="/dashboard"><LayoutDashboard className="size-4" />Console</Link>
          </DropdownMenuItem>
        )}
        <DropdownMenuItem asChild>
          <Link href="/workspace/settings/account"><Settings className="size-4" />Account settings</Link>
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onSelect={(event) => {
            event.preventDefault();
            void signOut({ redirect: false }).then(() => router.push('/sign-in'));
          }}
        >
          <LogOut className="size-4" />Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * Member application shell for /workspace — visual twin of the console:
 * collapsible sidebar (team switcher + workspace nav), sticky header with
 * breadcrumbs and the user menu, uniform page container.
 */
export function WorkspaceShell({
  children,
  user,
  teams,
}: {
  children: React.ReactNode;
  user: WorkspaceUser;
  teams: readonly WorkspaceTeamSummary[];
}): React.JSX.Element {
  return (
    <div className="flex h-svh min-w-0 flex-1 overflow-hidden">
      <SidebarProvider>
        <WorkspaceSidebar teams={teams} />
        <SidebarInset className="min-w-0 overflow-hidden">
          <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
            <header className="sticky top-0 z-40 grid h-16 min-w-0 shrink-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-2 border-b px-3 sm:px-4">
              <div className="flex min-w-0 items-center gap-2">
                <SidebarTrigger />
                <Link href="/workspace" className="hidden items-center gap-2 font-semibold sm:flex">
                  <span className="flex size-6 items-center justify-center rounded-md bg-primary text-primary-foreground">
                    <GraduationCap className="size-3.5" aria-hidden="true" />
                  </span>
                  Workspace
                </Link>
                <WorkspaceBreadcrumbs />
              </div>
              <div className="flex items-center gap-2">
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
