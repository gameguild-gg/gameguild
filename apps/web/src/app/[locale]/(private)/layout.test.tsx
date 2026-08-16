import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import type { ReactNode } from 'react';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  redirect: vi.fn((args: unknown) => {
    throw new Error(`redirect:${JSON.stringify(args)}`);
  }),
  getDashboardContexts: vi.fn(),
  getWorkspaceTeams: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/i18n/navigation', () => ({
  redirect: mocks.redirect,
  Link: ({ href, children }: { href: string; children: ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/dashboard-contexts', () => ({
  getDashboardContexts: mocks.getDashboardContexts,
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceTeams: mocks.getWorkspaceTeams,
}));
vi.mock('@/components/workspace/workspace-shell', () => ({
  WorkspaceShell: ({ children, user, teams }: { children: ReactNode; user: { name: string }; teams: unknown[] }) => (
    <div data-testid="workspace-shell" data-user={user.name} data-teams={teams.length}>
      {children}
    </div>
  ),
}));

import PrivateLayout from './layout';

const layoutProps = {
  children: <div data-testid="private-content">member content</div>,
  params: Promise.resolve({ locale: 'en-US' }),
};

describe('private layout auth gate', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getDashboardContexts.mockResolvedValue({ capabilities: [], contexts: [] });
    mocks.getWorkspaceTeams.mockResolvedValue([
      { id: 'team-1', slug: 'personal', name: 'Personal', isPersonal: true },
    ]);
  });
  afterEach(cleanup);

  it('redirects anonymous users to sign-in', async () => {
    mocks.auth.mockResolvedValue(null);

    await expect(PrivateLayout(layoutProps as never)).rejects.toThrow(
      'redirect:{"href":"/sign-in","locale":"en-US"}',
    );
  });

  it('redirects when auth returns an invalid session shape', async () => {
    mocks.auth.mockResolvedValue(() => undefined);

    await expect(PrivateLayout(layoutProps as never)).rejects.toThrow('redirect:');
  });

  it('renders children inside the workspace shell with teams for the switcher', async () => {
    mocks.auth.mockResolvedValue({ user: { id: 'user-1', name: 'Ada Lovelace' } });

    const ui = await PrivateLayout(layoutProps as never);
    render(ui);

    const shell = screen.getByTestId('workspace-shell');
    expect(shell).toHaveAttribute('data-user', 'Ada Lovelace');
    expect(shell).toHaveAttribute('data-teams', '1');
    expect(screen.getByTestId('private-content')).toBeInTheDocument();
  });
});
