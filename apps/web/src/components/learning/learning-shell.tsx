'use client';

import { ThemeToggle } from '@/components/ui/theme-toggle';
import type { DashboardNotificationSummary } from '@/lib/dashboard-notifications';
import { createLearnerRoutes } from '@/lib/learner/routes';
import { useAuth } from '@game-guild/client/react';
import { Avatar, AvatarFallback, AvatarImage } from '@game-guild/ui/components/avatar';
import { Button } from '@game-guild/ui/components/button';
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@game-guild/ui/components/dropdown-menu';
import {
  Award,
  Bell,
  BookOpen,
  CalendarDays,
  GraduationCap,
  Home,
  Library,
  LogOut,
  Menu,
  Search,
  X,
} from 'lucide-react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { type ReactNode, useEffect, useState } from 'react';

export interface LearningShellUser {
  id: string;
  name: string;
  email: string;
  image?: string | null;
}

interface LearningShellProps {
  children: ReactNode;
  notifications?: DashboardNotificationSummary;
  user: LearningShellUser;
  webOrigin?: string;
}

const routes = createLearnerRoutes();
const navigation = [
  { href: routes.home, label: 'Home', icon: Home },
  { href: routes.courses, label: 'My courses', icon: Library },
  { href: routes.calendar, label: 'Calendar', icon: CalendarDays },
  { href: routes.grades, label: 'Grades', icon: BookOpen },
  { href: routes.certificates, label: 'Certificates', icon: Award },
];

function initials(name: string): string {
  return (
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase() || 'GG'
  );
}

function isRouteActive(pathname: string, href: string): boolean {
  if (href === '/') return pathname === '/';
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function LearningShell({
  children,
  notifications,
  user,
  webOrigin = 'https://gameguild.gg',
}: LearningShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const { isLoading, signOut } = useAuth();
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const catalogUrl = new URL('/courses', webOrigin).toString().replace(/\/$/, '');
  const signInUrl = new URL('/sign-in', webOrigin).toString().replace(/\/$/, '');
  const notificationItems = notifications?.items ?? [];

  useEffect(() => {
    const handleKeyboard = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() === 'k' && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        setSearchOpen((current) => !current);
      }
    };

    document.addEventListener('keydown', handleKeyboard);
    return () => document.removeEventListener('keydown', handleKeyboard);
  }, []);

  async function handleSignOut() {
    setSigningOut(true);
    try {
      await signOut({ redirectTo: signInUrl });
    } finally {
      setSigningOut(false);
    }
  }

  function navigate(href: string) {
    setSearchOpen(false);
    if (href.startsWith('http')) {
      window.location.assign(href);
      return;
    }
    router.push(href);
  }

  return (
    <div className="min-h-screen bg-background text-foreground">
      <a
        href="#learning-content"
        className="sr-only z-[100] rounded-md bg-background px-4 py-2 focus:not-sr-only focus:fixed focus:left-4 focus:top-4"
      >
        Skip to learning content
      </a>

      <header className="sticky top-0 z-40 flex h-16 items-center border-b bg-background/95 px-4 backdrop-blur lg:pl-72">
        <Button
          variant="ghost"
          size="icon"
          className="mr-2 lg:hidden"
          onClick={() => setMobileOpen((open) => !open)}
          aria-label="Toggle navigation"
        >
          <Menu className="size-5" />
        </Button>

        <Link href="/" className="mr-auto flex items-center gap-2 font-semibold lg:hidden">
          <span className="flex size-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <GraduationCap className="size-4" />
          </span>
          GameGuild Learning
        </Link>

        <Button
          variant="outline"
          className="mx-auto hidden w-full max-w-md justify-start text-muted-foreground md:flex"
          onClick={() => setSearchOpen(true)}
        >
          <Search className="size-4" />
          Search learning
          <kbd className="ml-auto rounded border px-1.5 py-0.5 text-xs">Ctrl K</kbd>
        </Button>

        <div className="ml-auto flex items-center gap-1">
          <ThemeToggle />
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" aria-label="Open notifications">
                <Bell className="size-4" />
                {(notifications?.unreadCount ?? 0) > 0 ? (
                  <span className="absolute mt-[-1.25rem] ml-5 size-2 rounded-full bg-destructive" />
                ) : null}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-80">
              <DropdownMenuLabel>Notifications</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {notificationItems.length > 0 ? (
                notificationItems.slice(0, 6).map((item) => (
                  <DropdownMenuItem key={item.id} asChild>
                    <Link href={item.actionUrl || '/'} className="flex-col items-start gap-1">
                      <span className="font-medium">{item.title}</span>
                      {item.message ? (
                        <span className="line-clamp-2 text-xs text-muted-foreground">
                          {item.message}
                        </span>
                      ) : null}
                    </Link>
                  </DropdownMenuItem>
                ))
              ) : (
                <DropdownMenuItem disabled>No new notifications</DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="gap-2 px-2" aria-label="Open account menu">
                <Avatar size="sm">
                  {user.image ? <AvatarImage src={user.image} alt={user.name} /> : null}
                  <AvatarFallback>{initials(user.name)}</AvatarFallback>
                </Avatar>
                <span className="hidden max-w-36 truncate text-sm font-medium sm:inline">
                  {user.name}
                </span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-64">
              <DropdownMenuLabel className="font-normal">
                <p className="truncate text-sm font-medium">{user.name}</p>
                <p className="truncate text-xs text-muted-foreground">{user.email}</p>
              </DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                disabled={isLoading || signingOut}
                onSelect={(event) => {
                  event.preventDefault();
                  void handleSignOut();
                }}
              >
                <LogOut className="size-4" />
                {signingOut ? 'Signing out...' : 'Sign out'}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <aside
        className={`${mobileOpen ? 'flex' : 'hidden'} fixed inset-y-0 left-0 z-50 w-64 flex-col border-r bg-background p-4 lg:flex`}
      >
        <div className="mb-8 flex h-10 items-center gap-2">
          <Link
            href="/"
            onClick={() => setMobileOpen(false)}
            className="flex min-w-0 flex-1 items-center gap-3 px-2 text-sm font-semibold"
          >
            <span className="flex size-9 shrink-0 items-center justify-center rounded-md bg-primary text-primary-foreground">
              <GraduationCap className="size-5" />
            </span>
            <span className="truncate">GameGuild Learning</span>
          </Link>
          <Button
            variant="ghost"
            size="icon"
            className="shrink-0 lg:hidden"
            onClick={() => setMobileOpen(false)}
            aria-label="Close navigation"
          >
            <X className="size-5" />
          </Button>
        </div>

        <nav aria-label="Learner navigation" className="space-y-1">
          {navigation.map(({ href, icon: Icon, label }) => {
            const active = isRouteActive(pathname, href);
            return (
              <Link
                key={href}
                href={href}
                aria-current={active ? 'page' : undefined}
                onClick={() => setMobileOpen(false)}
                className={`flex h-10 items-center gap-3 rounded-md px-3 text-sm transition-colors ${
                  active
                    ? 'bg-accent text-accent-foreground'
                    : 'text-muted-foreground hover:bg-accent/60 hover:text-foreground'
                }`}
              >
                <Icon className="size-4" />
                {label}
              </Link>
            );
          })}
        </nav>

        <div className="mt-auto border-t pt-4">
          <Button asChild variant="outline" className="w-full justify-start">
            <Link href={catalogUrl}>
              <Library className="size-4" />
              Browse courses
            </Link>
          </Button>
        </div>
      </aside>

      {mobileOpen ? (
        <button
          className="fixed inset-0 z-40 bg-black/60 lg:hidden"
          onClick={() => setMobileOpen(false)}
          aria-label="Dismiss navigation"
        />
      ) : null}

      <main id="learning-content" className="min-w-0 lg:pl-64">
        <div className="mx-auto w-full max-w-[1600px] p-4 sm:p-6 lg:p-8">{children}</div>
      </main>

      <CommandDialog
        open={searchOpen}
        onOpenChange={setSearchOpen}
        title="Search learning"
        description="Navigate your learning workspace."
      >
        <CommandInput placeholder="Search learning..." />
        <CommandList>
          <CommandEmpty>No matching learning destination.</CommandEmpty>
          <CommandGroup heading="Learning">
            {navigation.map(({ href, icon: Icon, label }) => (
              <CommandItem key={href} onSelect={() => navigate(href)} value={label}>
                <Icon className="size-4" />
                {label}
              </CommandItem>
            ))}
            <CommandItem onSelect={() => navigate(catalogUrl)} value="Browse courses">
              <Library className="size-4" />
              Browse courses
            </CommandItem>
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </div>
  );
}
