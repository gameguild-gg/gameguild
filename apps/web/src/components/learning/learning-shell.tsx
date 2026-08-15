"use client";

import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { ThemeToggle } from "@/components/ui/theme-toggle";
import {
  searchLearnerWorkspace,
  type LearnerSearchItem,
} from "@/lib/learner/search-actions";
import type { DashboardNotificationSummary } from "@/lib/dashboard-notifications";
import { normalizeLearnerPathname } from "@/lib/learner/routes";
import { useAuth } from "@game-guild/client/react";
import {
  Avatar,
  AvatarFallback,
  AvatarImage,
} from "@game-guild/ui/components/avatar";
import { Button } from "@game-guild/ui/components/button";
import {
  CommandDialog,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from "@/components/ui/command";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@game-guild/ui/components/dropdown-menu";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from "@game-guild/ui/components/sheet";
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
} from "lucide-react";
import { type ReactNode, useEffect, useState } from "react";

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

const navigation = [
  { href: "/learn", label: "Home", icon: Home },
  { href: "/learn/courses", label: "My courses", icon: Library },
  { href: "/learn/calendar", label: "Calendar", icon: CalendarDays },
  { href: "/learn/grades", label: "Grades", icon: BookOpen },
  { href: "/learn/certificates", label: "Certificates", icon: Award },
];

function initials(name: string): string {
  return (
    name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join("")
      .toUpperCase() || "GG"
  );
}

function isRouteActive(pathname: string, href: string): boolean {
  if (href === "/") return pathname === "/";
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function LearningShell({
  children,
  notifications,
  user,
  webOrigin = "https://gameguild.gg",
}: LearningShellProps) {
  const pathname = normalizeLearnerPathname(usePathname());
  // Regex tests the locale-stripped pathname; coding assessments go edge-to-edge.
  const wide = /^\/learn\/courses\/[^/]+\/activities\/assessment-/.test(pathname);
  const router = useRouter();
  const { isLoading, signOut } = useAuth();
  const [ready, setReady] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [searchResults, setSearchResults] = useState<LearnerSearchItem[]>([]);
  const [searchStatus, setSearchStatus] = useState<
    "idle" | "loading" | "error" | "ready"
  >("idle");
  const catalogUrl = new URL("/courses", webOrigin)
    .toString()
    .replace(/\/$/, "");
  const signInUrl = new URL("/sign-in", webOrigin)
    .toString()
    .replace(/\/$/, "");
  const notificationItems = notifications?.items ?? [];

  useEffect(() => {
    setReady(true);
  }, []);

  useEffect(() => {
    const handleKeyboard = (event: KeyboardEvent) => {
      if (event.key.toLowerCase() === "k" && (event.ctrlKey || event.metaKey)) {
        event.preventDefault();
        setSearchOpen((current) => !current);
      }
    };

    document.addEventListener("keydown", handleKeyboard);
    return () => document.removeEventListener("keydown", handleKeyboard);
  }, []);

  useEffect(() => {
    const query = searchQuery.trim();
    if (query.length < 2) {
      setSearchResults([]);
      setSearchStatus("idle");
      return;
    }

    let cancelled = false;
    setSearchStatus("loading");
    const timer = window.setTimeout(async () => {
      const result = await searchLearnerWorkspace(query);
      if (cancelled) return;

      if (result.success) {
        setSearchResults(result.items);
        setSearchStatus("ready");
      } else {
        setSearchResults([]);
        setSearchStatus("error");
      }
    }, 250);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [searchQuery]);

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
    if (href.startsWith("http")) {
      window.location.assign(href);
      return;
    }
    router.push(href);
  }

  return (
    <div
      data-learning-ready={ready ? "true" : "false"}
      className="min-h-screen bg-background text-foreground"
    >
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
          aria-controls="learning-mobile-navigation"
          aria-expanded={mobileOpen}
        >
          <Menu className="size-5" />
        </Button>

        <Link
          href="/"
          className="mr-auto flex items-center gap-2 font-semibold lg:hidden"
        >
          <span className="flex size-8 items-center justify-center rounded-md bg-primary text-primary-foreground">
            <GraduationCap className="size-4" />
          </span>
          GameGuild Learning
        </Link>

        <Button
          variant="outline"
          className="mx-auto hidden w-full max-w-md justify-start text-muted-foreground lg:flex"
          onClick={() => setSearchOpen(true)}
        >
          <Search className="size-4" />
          Search learning
          <kbd className="ml-auto rounded border px-1.5 py-0.5 text-xs">
            Ctrl K
          </kbd>
        </Button>

        <div className="ml-auto flex items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="lg:hidden"
            aria-label="Search learning"
            onClick={() => setSearchOpen(true)}
          >
            <Search aria-hidden="true" className="size-4" />
          </Button>
          <ThemeToggle />
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                size="icon"
                aria-label="Open notifications"
              >
                <Bell aria-hidden="true" className="size-4" />
                {(notifications?.unreadCount ?? 0) > 0 ? (
                  <>
                    <span
                      aria-hidden="true"
                      className="absolute mt-[-1.25rem] ml-5 size-2 rounded-full bg-destructive"
                    />
                    <span className="sr-only">
                      {notifications?.unreadCount} unread notifications
                    </span>
                  </>
                ) : null}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-80">
              <DropdownMenuLabel>Notifications</DropdownMenuLabel>
              <DropdownMenuSeparator />
              {notificationItems.length > 0 ? (
                notificationItems.slice(0, 6).map((item) => (
                  <DropdownMenuItem key={item.id} asChild>
                    <Link
                      href={item.actionUrl || "/"}
                      className="flex-col items-start gap-1"
                    >
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
                <DropdownMenuItem disabled>
                  No new notifications
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                variant="ghost"
                className="gap-2 px-2"
                aria-label="Open account menu"
              >
                <Avatar size="sm">
                  {user.image ? (
                    <AvatarImage src={user.image} alt={user.name} />
                  ) : null}
                  <AvatarFallback>{initials(user.name)}</AvatarFallback>
                </Avatar>
                <span className="hidden max-w-36 truncate text-sm font-medium xl:inline">
                  {user.name}
                </span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-64">
              <DropdownMenuLabel className="font-normal">
                <p className="truncate text-sm font-medium">{user.name}</p>
                <p className="truncate text-xs text-muted-foreground">
                  {user.email}
                </p>
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
                {signingOut ? "Signing out..." : "Sign out"}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </header>

      <aside
        className={
          wide
            ? "hidden"
            : "fixed inset-y-0 left-0 z-50 hidden w-64 flex-col border-r bg-background p-4 lg:flex"
        }
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
        </div>

        <nav aria-label="Learner navigation" className="space-y-1">
          {navigation.map(({ href, icon: Icon, label }) => {
            const active = isRouteActive(pathname, href);
            return (
              <Link
                key={href}
                href={href}
                aria-current={active ? "page" : undefined}
                onClick={() => setMobileOpen(false)}
                className={`flex h-10 items-center gap-3 rounded-md px-3 text-sm transition-colors ${
                  active
                    ? "bg-accent text-accent-foreground"
                    : "text-muted-foreground hover:bg-accent/60 hover:text-foreground"
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

      <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
        <SheetContent
          id="learning-mobile-navigation"
          side="left"
          className="w-72 p-4 lg:hidden"
          aria-label="Learner navigation"
        >
          <SheetHeader className="mb-6 p-0 pr-8 text-left">
            <SheetTitle className="flex items-center gap-3">
              <span className="flex size-9 items-center justify-center rounded-md bg-primary text-primary-foreground">
                <GraduationCap aria-hidden="true" className="size-5" />
              </span>
              GameGuild Learning
            </SheetTitle>
            <SheetDescription>
              Navigate your courses and learning records.
            </SheetDescription>
          </SheetHeader>
          <nav aria-label="Learner navigation" className="space-y-1">
            {navigation.map(({ href, icon: Icon, label }) => {
              const active = isRouteActive(pathname, href);
              return (
                <Link
                  key={href}
                  href={href}
                  aria-current={active ? "page" : undefined}
                  onClick={() => setMobileOpen(false)}
                  className={`flex h-11 items-center gap-3 rounded-md px-3 text-sm transition-colors ${
                    active
                      ? "bg-accent text-accent-foreground"
                      : "text-muted-foreground hover:bg-accent/60 hover:text-foreground"
                  }`}
                >
                  <Icon aria-hidden="true" className="size-4" />
                  {label}
                </Link>
              );
            })}
          </nav>
          <div className="mt-auto border-t pt-4">
            <Button asChild variant="outline" className="w-full justify-start">
              <Link href={catalogUrl} onClick={() => setMobileOpen(false)}>
                <Library aria-hidden="true" className="size-4" />
                Browse courses
              </Link>
            </Button>
          </div>
        </SheetContent>
      </Sheet>

      <main
        id="learning-content"
        tabIndex={-1}
        className={
          wide ? "min-w-0 overflow-x-clip" : "min-w-0 overflow-x-clip lg:pl-64"
        }
      >
        <div
          className={
            wide
              ? "w-full px-4 pt-4 pb-6"
              : "mx-auto w-full max-w-[1600px] p-4 sm:p-6 lg:p-8"
          }
        >
          {children}
        </div>
      </main>

      <CommandDialog
        open={searchOpen}
        onOpenChange={setSearchOpen}
        title="Search learning"
        description="Search the courses and content available to your account."
      >
        <CommandInput
          placeholder="Search your courses and lessons..."
          value={searchQuery}
          onValueChange={setSearchQuery}
        />
        <CommandList aria-busy={searchStatus === "loading"}>
          <CommandEmpty>
            {searchStatus === "loading"
              ? "Searching your learning workspace..."
              : searchStatus === "error"
                ? "Search is temporarily unavailable. Try again."
                : searchQuery.trim().length < 2
                  ? "Type at least 2 characters to search."
                  : "No matching courses or lessons."}
          </CommandEmpty>
          {searchResults.length > 0 ? (
            <CommandGroup heading="Results">
              {searchResults.map((result) => (
                <CommandItem
                  key={`${result.kind}-${result.id}`}
                  onSelect={() => navigate(result.route)}
                  value={`${result.title} ${result.kind} ${result.description}`}
                >
                  <BookOpen aria-hidden="true" className="size-4" />
                  <span className="min-w-0">
                    <span className="block truncate font-medium">
                      {result.title}
                    </span>
                    <span className="block truncate text-xs text-muted-foreground">
                      {result.kind}
                      {result.description ? ` - ${result.description}` : ""}
                    </span>
                  </span>
                </CommandItem>
              ))}
            </CommandGroup>
          ) : null}
          <CommandGroup heading="Learning">
            {navigation.map(({ href, icon: Icon, label }) => (
              <CommandItem
                key={href}
                onSelect={() => navigate(href)}
                value={label}
              >
                <Icon className="size-4" />
                {label}
              </CommandItem>
            ))}
            <CommandItem
              onSelect={() => navigate(catalogUrl)}
              value="Browse courses"
            >
              <Library className="size-4" />
              Browse courses
            </CommandItem>
          </CommandGroup>
        </CommandList>
      </CommandDialog>
    </div>
  );
}
