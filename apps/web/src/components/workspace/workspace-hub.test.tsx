import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import type { ReactNode } from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getWorkspaceTeams: vi.fn(),
  getWorkspaceProjects: vi.fn(),
  getWorkspaceMyTeamInvitations: vi.fn(),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children, href, ...props }: { children: ReactNode; href: string }) => (
    <a href={href} {...props}>{children}</a>
  ),
}));

vi.mock('@/lib/workspaces', () => ({
  getWorkspaceTeams: mocks.getWorkspaceTeams,
  getWorkspaceProjects: mocks.getWorkspaceProjects,
  getWorkspaceMyTeamInvitations: mocks.getWorkspaceMyTeamInvitations,
}));

import { WorkspaceHub } from './workspace-hub';

describe('WorkspaceHub', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.getWorkspaceTeams.mockResolvedValue([]);
    mocks.getWorkspaceProjects.mockResolvedValue([]);
    mocks.getWorkspaceMyTeamInvitations.mockResolvedValue([]);
  });

  afterEach(cleanup);

  it('renders its navigation actions as styled links without a Radix Slot boundary', async () => {
    render(await WorkspaceHub());

    expect(screen.getByRole('link', { name: 'Team' })).toHaveAttribute('href', '/workspace/teams/new');
    expect(screen.getByRole('link', { name: 'Project' })).toHaveAttribute('href', '/workspace/projects/new');
    expect(screen.getByRole('link', { name: 'All teams' })).toHaveAttribute('href', '/workspace/teams');
    expect(screen.getByRole('link', { name: 'All projects' })).toHaveAttribute('href', '/workspace/projects');
    expect(screen.getByRole('link', { name: 'Open assigned work' })).toHaveAttribute('href', '/workspace/work');
    expect(screen.getByText('Create a team to collaborate on projects.')).toBeInTheDocument();
    expect(screen.getByText('Create a project or join a team project.')).toBeInTheDocument();
  });

  it('renders recent teams, projects, and invitation totals', async () => {
    mocks.getWorkspaceTeams.mockResolvedValue([{ id: 'team-1', slug: 'alpha', name: 'Alpha' }]);
    mocks.getWorkspaceProjects.mockResolvedValue([{ id: 'project-1', slug: 'arcade', title: 'Arcade', status: 'Active' }]);
    mocks.getWorkspaceMyTeamInvitations.mockResolvedValue([{ id: 'invitation-1' }]);

    render(await WorkspaceHub());

    expect(screen.getByRole('link', { name: /Alpha/ })).toHaveAttribute('href', '/workspace/teams/alpha');
    expect(screen.getByRole('link', { name: /Arcade/ })).toHaveAttribute('href', '/workspace/projects/arcade');
    expect(screen.getByText('Invitations').closest('[data-slot="card"]')).toHaveTextContent('1');
  });
});
