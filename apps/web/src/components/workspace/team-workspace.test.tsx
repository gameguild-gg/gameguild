import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceTeam: vi.fn(),
  getWorkspaceTeamProjects: vi.fn(),
  getWorkspaceTeamInvitations: vi.fn(),
  notFound: vi.fn(() => {
    throw new Error('not-found');
  }),
}));

vi.mock('next/navigation', () => ({
  usePathname: () => '/workspace/learning', notFound: mocks.notFound }));
vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children }: { href: string; children: React.ReactNode }) => (
    <a href={href}>{children}</a>
  ),
}));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceTeam: mocks.getWorkspaceTeam,
  getWorkspaceTeamProjects: mocks.getWorkspaceTeamProjects,
  getWorkspaceTeamInvitations: mocks.getWorkspaceTeamInvitations,
  getWorkspaceProjectOwnership: vi.fn().mockResolvedValue(null),
  getWorkspaceLibrary: vi.fn().mockResolvedValue(null),
}));
vi.mock('@/lib/workspace-actions', () => ({
  addTeamMemberForm: vi.fn(),
  archiveTeamForm: vi.fn(),
  changeTeamMemberForm: vi.fn(),
  createTeamInvitationForm: vi.fn(),
  removeTeamMemberForm: vi.fn(),
  restoreTeamForm: vi.fn(),
  revokeTeamInvitationForm: vi.fn(),
  updateTeamForm: vi.fn(),
}));
vi.mock('@/components/workspaces/context-workspace-nav', () => ({
  ContextWorkspaceNav: ({
    base,
    active,
    items,
  }: {
    base: string;
    active: string;
    items: string[];
  }) => (
    <nav data-testid="workspace-nav" data-base={base} data-active={active}>
      {items.join(',')}
    </nav>
  ),
}));
vi.mock('@/components/workspaces/workspace-library-panel', () => ({
  WorkspaceLibraryPanel: () => <div data-testid="library-panel" />,
}));

import TeamWorkspaceView from './team-workspace';

const team = {
  id: 'team-1',
  slug: 'alpha-team',
  name: 'Alpha Team',
  description: 'First team',
  isPersonal: false,
  visibility: 'Private',
  status: 'Active',
  members: [{ userId: 'u1', isActive: true, authority: 'Owner', professionalTitle: 'Lead' }],
};

function props(slug: string, section?: string) {
  return { slug, section };
}

describe('team workspace page (member surface)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceTeam.mockResolvedValue(team);
    mocks.getWorkspaceTeamProjects.mockResolvedValue([
      { id: 'project-1', slug: 'neon-racer', title: 'Neon Racer', teamRole: 'Contributor', status: 'Draft', participationMode: 'SelectedMembers' },
    ]);
    mocks.getWorkspaceTeamInvitations.mockResolvedValue([]);
  });
  afterEach(cleanup);

  it('uses /teams/:slug as the member base route', async () => {
    render(await TeamWorkspaceView(props('alpha-team') as never));

    expect(screen.getByTestId('workspace-nav')).toHaveAttribute('data-base', '/workspace/teams/alpha-team');
    expect(screen.getByTestId('workspace-nav')).toHaveAttribute('data-active', 'overview');
  });

  it('links team projects to /projects/:slug', async () => {
    render(await TeamWorkspaceView(props('alpha-team', 'projects') as never));

    expect(screen.getByRole('link', { name: /Neon Racer/ })).toHaveAttribute(
      'href',
      '/workspace/projects/neon-racer',
    );
  });

  it('links project creation to the member /projects/new route', async () => {
    render(await TeamWorkspaceView(props('alpha-team') as never));

    expect(screen.getByRole('link', { name: 'Create project' })).toHaveAttribute(
      'href',
      '/workspace/projects/new',
    );
  });

  it('falls back to the overview section when none is given', async () => {
    render(await TeamWorkspaceView(props('alpha-team') as never));

    expect(screen.getByTestId('workspace-nav')).toHaveAttribute('data-active', 'overview');
    expect(screen.getByText('Active members')).toBeInTheDocument();
  });

  it('returns not-found when the team does not exist', async () => {
    mocks.getWorkspaceTeam.mockResolvedValue(null);

    await expect(TeamWorkspaceView(props('ghost') as never)).rejects.toThrow('not-found');
  });
});
