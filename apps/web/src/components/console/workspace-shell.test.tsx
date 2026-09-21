import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const appSidebarSpy = vi.hoisted(() => vi.fn());
const mocks = vi.hoisted(() => ({ pathname: '/workspace' }));

vi.mock('@/components/app/app-shell-sidebar', () => ({
  AppShellSidebar: (props: unknown) => {
    appSidebarSpy(props);
    return <nav aria-label="Workspace navigation" />;
  },
}));
vi.mock('@/components/app/app-shell-header-menu', () => ({ AppShellHeaderMenu: () => <div /> }));
vi.mock('./workspace-command-palette', () => ({ WorkspaceCommandPalette: () => null }));
vi.mock('@game-guild/ui/components/sonner', () => ({ Toaster: () => null }));
vi.mock('@game-guild/ui/components/sidebar', () => ({
  SidebarProvider: ({ children }: { readonly children: React.ReactNode }) => <>{children}</>,
  SidebarInset: ({ children }: { readonly children: React.ReactNode }) => <>{children}</>,
  SidebarTrigger: () => <button type="button">Toggle sidebar</button>,
}));
vi.mock('@/i18n/navigation', () => ({
  Link: 'a',
  usePathname: () => mocks.pathname,
}));

import { WorkspaceShell } from './workspace-shell';

describe('dashboard keyboard navigation', () => {
  beforeEach(() => {
    appSidebarSpy.mockClear();
    mocks.pathname = '/workspace';
  });

  it('offers a direct skip link to the focusable main content', () => {
    render(
      <WorkspaceShell user={{ id: 'user-1', name: 'Member', email: 'member@gameguild.gg' }}>
        <p>Workspace content</p>
      </WorkspaceShell>,
    );

    expect(screen.getByRole('link', { name: 'Skip to main content' })).toHaveAttribute(
      'href',
      '#dashboard-main',
    );
    const skipTarget = document.getElementById('dashboard-main');
    expect(skipTarget).toHaveAttribute('tabindex', '-1');
    expect(skipTarget?.tagName).toBe('DIV');
  });

  it('does not expose workspace and operations as switchable tenant contexts', () => {
    render(
      <WorkspaceShell
        user={{ id: 'user-1', name: 'Member', email: 'member@gameguild.gg' }}
        contexts={[
          { type: 'Workspace', id: null, name: 'Workspace', route: '/workspace' },
          { type: 'Operations', id: null, name: 'Operations', route: '/dashboard' },
        ]}
      >
        <p>Workspace content</p>
      </WorkspaceShell>,
    );

    expect(appSidebarSpy).toHaveBeenCalledOnce();
    expect(appSidebarSpy.mock.calls[0]?.[0]).not.toHaveProperty('contexts');
  });

  it('keeps the folded breadcrumbs in the header leading region', () => {
    mocks.pathname = '/workspace/testing-lab/reports';
    render(
      <WorkspaceShell user={{ id: 'user-1', name: 'Member', email: 'member@gameguild.gg' }}>
        <p>Workspace content</p>
      </WorkspaceShell>,
    );

    expect(screen.getByRole('navigation', { name: 'Dashboard breadcrumb' })).toHaveTextContent('Reports');
  });
});
