'use client';

import * as React from 'react';
import {
  SidebarInset,
  SidebarProvider,
} from '@game-guild/ui/components/sidebar';

/**
 * The single logged-in app shell. A `SidebarProvider` wrapper; compose the
 * sidebar and inset regions as children so each layout controls its content.
 */
export function AppShell({ children }: { readonly children: React.ReactNode }) {
  return (
    <SidebarProvider
      style={{ '--sidebar-width-icon': '4rem' } as React.CSSProperties}
    >
      <div className="flex h-svh min-w-0 flex-1 overflow-hidden">{children}</div>
    </SidebarProvider>
  );
}

/** The content column next to the sidebar; hosts the header and page content. */
export function AppShellInset({ children }: { readonly children: React.ReactNode }) {
  return (
    <SidebarInset className="min-w-0 overflow-hidden">
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        {children}
      </div>
    </SidebarInset>
  );
}

/** The scrollable page content region inside the inset. */
export function AppShellContent({
  children,
}: {
  readonly children: React.ReactNode;
}) {
  return (
    <div
      id="dashboard-main"
      tabIndex={-1}
      className="min-w-0 flex-1 overflow-y-auto overflow-x-hidden"
    >
      {children}
    </div>
  );
}
