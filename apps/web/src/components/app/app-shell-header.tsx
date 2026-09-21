import type { ReactNode } from 'react';

/**
 * Shared app header bar. A thin layout wrapper: consumers compose the leading
 * region (breadcrumbs or navigation) and the `<AppShellHeaderMenu>` actions slot.
 */
export function AppShellHeader({ children }: { children: ReactNode }) {
  return (
    <header className="sticky top-0 z-40 grid h-16 min-w-0 shrink-0 grid-cols-[minmax(0,1fr)_auto] items-center gap-2 border-b px-3 sm:px-4">
      {children}
    </header>
  );
}
