'use client';

import { Link, usePathname } from '@/i18n/navigation';
import { Button } from '@game-guild/ui/components/button';
import {
  Sheet,
  SheetClose,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from '@game-guild/ui/components/sheet';
import { cn } from '@game-guild/ui/lib/utils';
import { Github, Menu } from 'lucide-react';

export type PublicNavItem = {
  readonly label: string;
  readonly href: string;
};

export type PublicWebsiteUser = {
  readonly name: string;
  readonly email: string | null;
  readonly image: string | null;
  readonly initials: string;
};

function isActivePath(pathname: string, href: string) {
  if (href === '/') return pathname === '/';
  return pathname === href || pathname.startsWith(`${href}/`);
}

export function PublicDesktopNav({ items }: { readonly items: readonly PublicNavItem[] }) {
  const pathname = usePathname() ?? '/';

  return (
    <nav
      aria-label="Main navigation"
      className="hidden items-center rounded-full border border-white/10 bg-white/[0.03] p-1 lg:flex"
    >
      {items.map((item) => {
        const active = isActivePath(pathname, item.href);

        return (
          <a
            key={item.href}
            href={item.href}
            aria-current={active ? 'page' : undefined}
            className={cn(
              'rounded-full px-3 py-1.5 text-sm font-medium transition',
              active ? 'bg-sky-300 text-slate-950' : 'text-slate-300 hover:bg-white/10 hover:text-white',
            )}
          >
            {item.label}
          </a>
        );
      })}
    </nav>
  );
}

export function PublicMobileNav({
  items,
  user = null,
}: {
  readonly items: readonly PublicNavItem[];
  readonly user?: PublicWebsiteUser | null;
}) {
  const pathname = usePathname() ?? '/';

  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="border-white/10 bg-white/[0.03] text-white hover:bg-white/10 hover:text-white lg:hidden"
          aria-label="Open public navigation"
        >
          <Menu className="size-4" aria-hidden="true" />
        </Button>
      </SheetTrigger>
      <SheetContent side="right" className="border-white/10 bg-slate-950 text-white">
        <SheetHeader>
          <SheetTitle className="text-white">GameGuild</SheetTitle>
          <SheetDescription className="text-slate-400">Move from learning to testing, projects, and community.</SheetDescription>
        </SheetHeader>

        <nav aria-label="Mobile navigation" className="mt-8 grid gap-2">
          {items.map((item) => {
            const active = isActivePath(pathname, item.href);

            return (
              <SheetClose asChild key={item.href}>
                <a
                  href={item.href}
                  aria-current={active ? 'page' : undefined}
                  className={cn(
                    'rounded-2xl border px-4 py-3 text-sm font-semibold transition',
                    active
                      ? 'border-sky-300 bg-sky-300 text-slate-950'
                      : 'border-white/10 bg-white/[0.03] text-slate-200 hover:bg-white/10 hover:text-white',
                  )}
                >
                  {item.label}
                </a>
              </SheetClose>
            );
          })}
        </nav>

        <div className="mt-8 grid gap-3">
          {user ? (
            <SheetClose asChild>
              <Link
                href="/dashboard"
                aria-label={`${user.name} profile`}
                className="inline-flex min-w-0 items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.04] px-4 py-3 text-left text-sm font-semibold text-white transition hover:bg-white/10"
              >
                <span className="flex size-10 shrink-0 items-center justify-center overflow-hidden rounded-full bg-sky-300 text-sm font-bold text-slate-950">
                  {user.image ? (
                    // eslint-disable-next-line @next/next/no-img-element
                    <img src={user.image} alt="" className="size-full object-cover" />
                  ) : (
                    user.initials
                  )}
                </span>
                <span className="min-w-0">
                  <span className="block truncate">{user.name}</span>
                  {user.email && <span className="block truncate text-xs font-medium text-slate-400">{user.email}</span>}
                </span>
              </Link>
            </SheetClose>
          ) : (
            <>
              <SheetClose asChild>
                <Link
                  href="/sign-up"
                  className="inline-flex items-center justify-center rounded-full bg-sky-300 px-4 py-2 text-sm font-semibold text-slate-950 transition hover:bg-sky-200"
                >
                  Join community
                </Link>
              </SheetClose>
              <SheetClose asChild>
                <Link
                  href="/sign-in"
                  className="inline-flex items-center justify-center rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:bg-white/10 hover:text-white"
                >
                  Sign in
                </Link>
              </SheetClose>
            </>
          )}
          <a
            href="https://github.com/gameguild-gg/gameguild"
            className="inline-flex items-center justify-center gap-2 rounded-full border border-white/10 px-4 py-2 text-sm font-semibold text-slate-200 transition hover:bg-white/10 hover:text-white"
          >
            <Github className="size-4" aria-hidden="true" />
            GitHub
          </a>
        </div>
      </SheetContent>
    </Sheet>
  );
}
