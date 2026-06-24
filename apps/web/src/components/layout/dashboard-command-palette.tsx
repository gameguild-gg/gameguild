'use client';

import * as React from 'react';
import { ArrowRight, Clock, FlaskConical, Plus, Rocket, Search } from 'lucide-react';
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandSeparator,
  CommandShortcut,
} from '@/components/ui/command';
import { usePathname, useRouter } from '@/i18n/navigation';
import { flattenDashboardNavigationItems, type DashboardNavSubItem } from './dashboard-sidebar';

export const DASHBOARD_COMMAND_PALETTE_OPEN_EVENT = 'gameguild:open-dashboard-command-palette';

const RECENT_ROUTES_KEY = 'gameguild:dashboard:recent-routes';
const MAX_RECENT_ROUTES = 6;

type RecentRoute = {
  href: string;
  label: string;
  visitedAt: number;
};

const quickActions: DashboardNavSubItem[] = [
  { title: 'Create course', url: '/dashboard/learning/courses/new', icon: Plus },
  { title: 'Review testing lab', url: '/dashboard/testing-lab', icon: FlaskConical },
  { title: 'Open launch pad', url: '/dashboard/launch-pad', icon: Rocket },
  { title: 'Manage members', url: '/dashboard/community/members/users', icon: ArrowRight },
];

function getRouteLabel(href: string, items: DashboardNavSubItem[]): string {
  const exact = items.find((item) => item.url === href);
  if (exact) return exact.title;

  const lastSegment = href.split('/').filter(Boolean).at(-1);
  if (!lastSegment) return 'Dashboard';

  return lastSegment
    .split('-')
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(' ');
}

function readRecentRoutes(): RecentRoute[] {
  if (typeof window === 'undefined') return [];

  try {
    const raw = window.localStorage.getItem(RECENT_ROUTES_KEY);
    if (!raw) return [];

    const parsed = JSON.parse(raw) as RecentRoute[];
    return Array.isArray(parsed)
      ? parsed.filter((item) => typeof item.href === 'string' && typeof item.label === 'string')
      : [];
  } catch {
    return [];
  }
}

function writeRecentRoutes(routes: RecentRoute[]) {
  if (typeof window === 'undefined') return;
  window.localStorage.setItem(RECENT_ROUTES_KEY, JSON.stringify(routes.slice(0, MAX_RECENT_ROUTES)));
}

function addRecentRoute(route: RecentRoute) {
  const next = [route, ...readRecentRoutes().filter((item) => item.href !== route.href)].slice(0, MAX_RECENT_ROUTES);
  writeRecentRoutes(next);
  return next;
}

export function openDashboardCommandPalette() {
  if (typeof window === 'undefined') return;
  window.dispatchEvent(new Event(DASHBOARD_COMMAND_PALETTE_OPEN_EVENT));
}

export function DashboardCommandPalette() {
  const router = useRouter();
  const pathname = usePathname();
  const navigationItems = React.useMemo(() => flattenDashboardNavigationItems(), []);
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState('');
  const [recentRoutes, setRecentRoutes] = React.useState<RecentRoute[]>([]);

  React.useEffect(() => {
    if (!pathname || pathname === '/sign-in') return;

    setRecentRoutes(
      addRecentRoute({
        href: pathname,
        label: getRouteLabel(pathname, navigationItems),
        visitedAt: Date.now(),
      }),
    );
  }, [navigationItems, pathname]);

  React.useEffect(() => {
    const openFromShell = () => {
      setRecentRoutes(readRecentRoutes());
      setOpen(true);
    };

    const down = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() === 'k' && (event.metaKey || event.ctrlKey)) {
        event.preventDefault();
        setRecentRoutes(readRecentRoutes());
        setOpen((current) => !current);
      }
    };

    document.addEventListener('keydown', down);
    window.addEventListener(DASHBOARD_COMMAND_PALETTE_OPEN_EVENT, openFromShell);

    return () => {
      document.removeEventListener('keydown', down);
      window.removeEventListener(DASHBOARD_COMMAND_PALETTE_OPEN_EVENT, openFromShell);
    };
  }, []);

  const runCommand = React.useCallback((href: string) => {
    setOpen(false);
    setQuery('');
    router.push(href);
  }, [router]);

  const trimmedQuery = query.trim();
  const searchHref = `/dashboard/search?q=${encodeURIComponent(trimmedQuery)}`;
  const showSearchAll = trimmedQuery.length > 1;

  return (
    <CommandDialog
      open={open}
      onOpenChange={setOpen}
      title="Search dashboard"
      description="Search GameGuild dashboard pages, recent resources, and quick actions."
      className="max-w-2xl"
    >
      <CommandInput
        value={query}
        onValueChange={setQuery}
        placeholder="Search courses, members, testing lab, launch pad..."
        aria-label="Search dashboard"
      />
      <CommandList className="max-h-[420px]">
        <CommandEmpty>No matching pages or actions found.</CommandEmpty>

        {showSearchAll && (
          <CommandGroup heading="Search">
            <CommandItem value={`search all ${trimmedQuery}`} onSelect={() => runCommand(searchHref)}>
              <Search className="size-4" />
              <span>Search all results for &quot;{trimmedQuery}&quot;</span>
              <CommandShortcut>Enter</CommandShortcut>
            </CommandItem>
          </CommandGroup>
        )}

        {!trimmedQuery && recentRoutes.length > 0 && (
          <>
            <CommandGroup heading="Recent">
              {recentRoutes.map((route) => (
                <CommandItem key={route.href} value={`${route.label} ${route.href}`} onSelect={() => runCommand(route.href)}>
                  <Clock className="size-4" />
                  <span>{route.label}</span>
                  <CommandShortcut>{route.href}</CommandShortcut>
                </CommandItem>
              ))}
            </CommandGroup>
            <CommandSeparator />
          </>
        )}

        <CommandGroup heading="Quick actions">
          {quickActions.map((action) => {
            const Icon = action.icon;
            return (
              <CommandItem key={action.url} value={`${action.title} ${action.url}`} onSelect={() => runCommand(action.url)}>
                <Icon className="size-4" />
                <span>{action.title}</span>
                <CommandShortcut>{action.url}</CommandShortcut>
              </CommandItem>
            );
          })}
        </CommandGroup>

        <CommandSeparator />

        <CommandGroup heading="Pages">
          {navigationItems.map((item) => {
            const Icon = item.icon;
            return (
              <CommandItem key={item.url} value={`${item.title} ${item.url}`} onSelect={() => runCommand(item.url)}>
                <Icon className="size-4" />
                <span>{item.title}</span>
                <CommandShortcut>{item.url}</CommandShortcut>
              </CommandItem>
            );
          })}
        </CommandGroup>
      </CommandList>
    </CommandDialog>
  );
}
