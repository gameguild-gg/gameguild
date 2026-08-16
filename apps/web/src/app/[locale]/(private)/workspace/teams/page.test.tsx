import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceTeams: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceTeams: mocks.getWorkspaceTeams,
}));

import TeamsPage from './page';

const teams = [
  {
    id: 'team-1',
    slug: 'alpha-team',
    name: 'Alpha Team',
    description: 'First team',
    isPersonal: false,
    visibility: 'Private',
    members: [
      { userId: 'u1', isActive: true },
      { userId: 'u2', isActive: false },
    ],
  },
  {
    id: 'team-2',
    slug: 'personal',
    name: 'Personal',
    description: null,
    isPersonal: true,
    visibility: 'Private',
    members: [],
  },
];

describe('member teams list page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceTeams.mockResolvedValue(teams);
  });
  afterEach(cleanup);

  it('lists teams with links to the new /teams/:slug routes', async () => {
    render(await TeamsPage());

    expect(screen.getByRole('heading', { name: 'Teams' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /Alpha Team/ })).toHaveAttribute(
      'href',
      '/workspace/teams/alpha-team',
    );
    expect(screen.getByText('1 active members')).toBeInTheDocument();
  });

  it('links team creation to /teams/new', async () => {
    render(await TeamsPage());

    expect(screen.getByRole('link', { name: /Create Team/ })).toHaveAttribute('href', '/workspace/teams/new');
  });

  it('shows the empty state when no teams exist', async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([]);

    render(await TeamsPage());

    expect(screen.getByText('No Teams yet')).toBeInTheDocument();
  });
});
