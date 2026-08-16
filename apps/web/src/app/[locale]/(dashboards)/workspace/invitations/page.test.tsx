import '@testing-library/jest-dom/vitest';
import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  auth: vi.fn(),
  getWorkspaceMyTeamInvitations: vi.fn(),
  getPendingMemberInvitations: vi.fn(),
}));

vi.mock('@/auth', () => ({ auth: mocks.auth }));
vi.mock('@/lib/workspaces', () => ({
  getWorkspaceMyTeamInvitations: mocks.getWorkspaceMyTeamInvitations,
}));
vi.mock('@/lib/community', () => ({
  getPendingMemberInvitations: mocks.getPendingMemberInvitations,
}));
vi.mock('@/lib/community/actions/member-access', () => ({
  acceptCurrentUserInvite: vi.fn(),
}));
vi.mock('@/lib/workspace-actions', () => ({
  acceptTeamInvitationForm: vi.fn(),
}));

import InvitationsPage from './page';

describe('member invitations page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mocks.auth.mockResolvedValue({ user: { id: 'user-1' } });
    mocks.getPendingMemberInvitations.mockResolvedValue({
      invitations: [
        {
          tenantId: 'tenant-1',
          tenantName: 'GameGuild',
          tenantSlug: 'gameguild',
          role: 'Member',
          invitedByEmail: 'admin@gameguild.gg',
        },
      ],
      error: undefined,
    });
    mocks.getWorkspaceMyTeamInvitations.mockResolvedValue([
      {
        id: 'invite-1',
        teamName: 'Alpha Team',
        authority: 'Member',
        expiresAt: '2026-01-01T00:00:00.000Z',
      },
    ]);
  });
  afterEach(cleanup);

  it('renders workspace and team invitations', async () => {
    render(await InvitationsPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByText('Pending workspace invitations')).toBeInTheDocument();
    expect(screen.getByText('GameGuild')).toBeInTheDocument();
    expect(screen.getByText('Pending Team invitations')).toBeInTheDocument();
    expect(screen.getByText('Alpha Team')).toBeInTheDocument();
  });

  it('shows the error alert when loading fails', async () => {
    mocks.getPendingMemberInvitations.mockResolvedValue({
      invitations: [],
      error: 'boom',
    });

    render(await InvitationsPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByText('Invitations could not be loaded')).toBeInTheDocument();
    expect(screen.getByText('boom')).toBeInTheDocument();
  });

  it('shows success message banner from query', async () => {
    render(
      await InvitationsPage({ searchParams: Promise.resolve({ message: 'Accepted' }) }),
    );

    expect(screen.getByText('Invitation updated')).toBeInTheDocument();
    expect(screen.getByText('Accepted')).toBeInTheDocument();
  });

  it('renders empty states when nothing is pending', async () => {
    mocks.getPendingMemberInvitations.mockResolvedValue({ invitations: [], error: undefined });
    mocks.getWorkspaceMyTeamInvitations.mockResolvedValue([]);

    render(await InvitationsPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByText('No pending invitations')).toBeInTheDocument();
    expect(screen.getByText('No pending Team invitations.')).toBeInTheDocument();
  });
});
