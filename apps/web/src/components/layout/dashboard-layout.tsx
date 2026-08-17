'use client';

import { DashboardShell } from '@/components/console/console-shell';

interface DashboardLayoutProps {
  children: React.ReactNode;
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <DashboardShell
      user={{
        id: 'local-preview-user',
        name: 'GameGuild user',
        email: 'user@gameguild.gg',
        image: null,
      }}
    >
      {children}
    </DashboardShell>
  );
}
