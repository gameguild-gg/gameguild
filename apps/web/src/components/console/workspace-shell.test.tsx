import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const workspaceSidebarSpy = vi.hoisted(() => vi.fn());

vi.mock('./workspace-sidebar', () => ({
  WorkspaceSidebar: (props: unknown) => {
    workspaceSidebarSpy(props);
    return <nav aria-label="Workspace navigation" />;
  },
  workspaceNavigationData: [],
  filterWorkspaceNavigation: () => [],
}));
vi.mock('./workspace-header', () => ({ WorkspaceHeader: () => <header /> }));
vi.mock('./workspace-command-palette', () => ({ WorkspaceCommandPalette: () => null }));
vi.mock('@game-guild/ui/components/sonner', () => ({ Toaster: () => null }));
vi.mock('@game-guild/ui/components/sidebar', () => ({
  SidebarProvider: ({ children }: { readonly children: React.ReactNode }) => <>{children}</>,
  SidebarInset: ({ children }: { readonly children: React.ReactNode }) => <>{children}</>,
}));

import { WorkspaceShell } from './workspace-shell';

describe('dashboard keyboard navigation', () => {
  beforeEach(() => {
    workspaceSidebarSpy.mockClear();
  });

  it('offers a direct skip link to the focusable main content', () => {
    render(
      <WorkspaceShell user={{ id: 'user-1', name: 'Member', initials: 'M' }}>
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
        user={{ id: 'user-1', name: 'Member', initials: 'M' }}
        contexts={[
          { type: 'Workspace', id: null, name: 'Workspace', route: '/workspace' },
          { type: 'Operations', id: null, name: 'Operations', route: '/dashboard' },
        ]}
      >
        <p>Workspace content</p>
      </WorkspaceShell>,
    );

    expect(workspaceSidebarSpy).toHaveBeenCalledOnce();
    expect(workspaceSidebarSpy.mock.calls[0]?.[0]).not.toHaveProperty('contexts');
  });
});
