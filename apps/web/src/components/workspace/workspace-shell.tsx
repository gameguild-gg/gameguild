import { AppShell } from '@/components/app/app-shell';
import { Link } from '@/i18n/navigation';
import { FolderKanban, MailCheck, Settings, SquareCheck, Users } from 'lucide-react';
import type React from 'react';
import type { ReactNode } from 'react';

const workspaceNav = [
  { label: 'Projects', href: '/workspace/projects', icon: FolderKanban },
  { label: 'Teams', href: '/workspace/teams', icon: Users },
  { label: 'Work', href: '/workspace/work', icon: SquareCheck },
  { label: 'Invitations', href: '/workspace/invitations', icon: MailCheck },
  { label: 'Settings', href: '/workspace/settings/account', icon: Settings },
] as const;

/** Authenticated member shell for /workspace — public chrome plus the workspace rail. */
export async function WorkspaceShell({ children }: { readonly children: ReactNode }): Promise<React.JSX.Element> {
  const shell = await AppShell({
    children: (
      <div className="mx-auto w-full max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        <nav aria-label="Workspace" className="mb-6 flex flex-wrap items-center gap-2">
          {workspaceNav.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className="inline-flex items-center gap-2 rounded-full border border-white/15 bg-white/[0.04] px-4 py-1.5 text-sm font-semibold text-slate-200 transition hover:border-white/30 hover:bg-white/10"
            >
              <item.icon className="size-4" aria-hidden="true" />
              {item.label}
            </Link>
          ))}
        </nav>
        {children}
      </div>
    ),
  });
  return shell;
}
