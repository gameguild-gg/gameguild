import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMemberAccessDirectory: vi.fn(),
  createCommunityGroup: vi.fn(),
  invitePlatformUser: vi.fn(),
  resendPlatformInvite: vi.fn(),
  cancelPlatformInvite: vi.fn(),
  acceptPlatformInvite: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  COMMUNITY_ACCESS_ROLES: [
    { value: 'Member', label: 'Member', description: 'Can access the community and learning surfaces.' },
    { value: 'Moderator', label: 'Moderator', description: 'Can moderate member activity and support queues.' },
    { value: 'TenantAdmin', label: 'Platform admin', description: 'Can manage users, content, and tenant operations.' },
    { value: 'SystemAdmin', label: 'Super admin', description: 'Full platform-management authority.' },
  ],
  getMemberAccessDirectory: mocks.getMemberAccessDirectory,
}));

vi.mock('@/lib/community/actions/groups', () => ({
  createCommunityGroup: mocks.createCommunityGroup,
}));

vi.mock('@/lib/community/actions/member-access', () => ({
  invitePlatformUser: mocks.invitePlatformUser,
  resendPlatformInvite: mocks.resendPlatformInvite,
  cancelPlatformInvite: mocks.cancelPlatformInvite,
  acceptPlatformInvite: mocks.acceptPlatformInvite,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import UsersPage from './page';

function buildMemberAccessRow(overrides: Partial<Record<string, unknown>> = {}) {
  const id = String(overrides.id ?? 'user-member');
  const username = String(overrides.username ?? id);
  const role = String(overrides.role ?? 'Member');
  const tenantId = overrides.tenantId === null ? null : String(overrides.tenantId ?? 'tenant-1');
  const tenantName = overrides.tenantName === null ? null : String(overrides.tenantName ?? 'GameGuild');
  const tenantSlug = overrides.tenantSlug === undefined ? tenantName?.toLowerCase() : String(overrides.tenantSlug);
  const primaryMembership = tenantId
    ? {
        tenantId,
        tenantName,
        tenantSlug,
        role,
        isActive: overrides.membershipIsActive ?? true,
        joinedAt: String(overrides.membershipJoinedAt ?? '2026-01-01T00:00:00.000Z'),
        leftAt: overrides.membershipLeftAt ?? null,
        inviteStatus: overrides.inviteStatus,
        invitedByEmail: overrides.invitedByEmail,
        invitedAt: overrides.invitedAt,
        lastInviteSentAt: overrides.lastInviteSentAt,
      }
    : null;

  return {
    member: {
      id,
      username,
      displayName: String(overrides.displayName ?? `${username} User`),
      email: String(overrides.email ?? `${username}@game-guild.com`),
      role: String(overrides.memberRole ?? role.toLowerCase()),
      status: String(overrides.status ?? 'active'),
      joinedAt: String(overrides.joinedAt ?? '2026-01-01T00:00:00.000Z'),
      lastActiveAt: String(overrides.lastActiveAt ?? '2026-06-01T00:00:00.000Z'),
    },
    memberships: primaryMembership ? [primaryMembership] : [],
    primaryMembership,
    role,
    isSuperAdmin: Boolean(overrides.isSuperAdmin ?? role === 'SystemAdmin'),
    isCurrentUser: Boolean(overrides.isCurrentUser ?? false),
    membershipLoadError: overrides.membershipLoadError ?? null,
  };
}

describe('community users page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders member counts and read-only access from the directory', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 2,
      currentUserId: 'user-admin',
      error: null,
      members: [
        buildMemberAccessRow({
          id: 'user-admin',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@game-guild.com',
          role: 'SystemAdmin',
          memberRole: 'admin',
          isCurrentUser: true,
        }),
        buildMemberAccessRow({
          id: 'user-member',
          username: 'member',
          displayName: 'Member User',
          email: 'member@game-guild.com',
          role: 'Member',
          joinedAt: '2026-02-01T00:00:00.000Z',
          lastActiveAt: '2026-06-02T00:00:00.000Z',
        }),
      ],
    });

    render(await UsersPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByRole('heading', { name: 'Users' })).toBeInTheDocument();
    expect(screen.getByText('Loaded from the identity API')).toBeInTheDocument();
    expect(screen.getByText('Members linked to an access workspace')).toBeInTheDocument();
    expect(screen.getByText('Access status')).toBeInTheDocument();
    expect(screen.getByText('Admin User')).toBeInTheDocument();
    expect(screen.getByText('Member User')).toBeInTheDocument();
    expect(screen.getAllByText('GameGuild')).toHaveLength(2);
    expect(screen.getByText('You')).toBeInTheDocument();
    expect(screen.queryByText('Promote / demote')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();

    const row = screen.getByText('Member User').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getAllByText('Member').length).toBeGreaterThan(0);
    expect(within(row!).getByText('Accepted')).toBeInTheDocument();
  });

  it('shows pending invite controls when workspace access is waiting for acceptance', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 2,
      currentUserId: 'user-admin',
      error: null,
      members: [
        buildMemberAccessRow({
          id: 'user-admin',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@game-guild.com',
          role: 'SystemAdmin',
          isCurrentUser: true,
          tenantId: 'tenant-1',
          tenantName: 'GameGuild',
        }),
        buildMemberAccessRow({
          id: 'user-pending',
          username: 'pending',
          displayName: 'Pending User',
          email: 'pending@game-guild.com',
          role: 'Moderator',
          membershipIsActive: false,
          inviteStatus: 'Pending',
          invitedByEmail: 'admin@game-guild.com',
          lastInviteSentAt: '2026-07-01T12:00:00.000Z',
        }),
      ],
    });

    render(await UsersPage({ searchParams: Promise.resolve({}) }));

    const row = screen.getByText('Pending User').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getByText('Pending invite')).toBeInTheDocument();
    expect(within(row!).getByText('Invited by admin@game-guild.com')).toBeInTheDocument();
    expect(within(row!).getByRole('button', { name: 'Resend invite' })).toBeInTheDocument();
    expect(within(row!).getByRole('button', { name: 'Accept invite' })).toBeInTheDocument();
    expect(within(row!).getByRole('button', { name: 'Cancel invite' })).toBeInTheDocument();
  });

  it('opens an invite dialog with user and workspace fields', async () => {
    const user = userEvent.setup();
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 1,
      currentUserId: 'user-admin',
      error: null,
      members: [
        buildMemberAccessRow({
          id: 'user-admin',
          username: 'admin',
          displayName: 'Admin User',
          email: 'admin@game-guild.com',
          role: 'SystemAdmin',
          isCurrentUser: true,
          tenantId: 'tenant-1',
          tenantName: 'GameGuild',
        }),
      ],
    });

    render(await UsersPage({ searchParams: Promise.resolve({}) }));
    await user.click(screen.getByRole('button', { name: 'Invite User' }));

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Invite user' })).toBeInTheDocument();
    expect(screen.getByLabelText('Email')).toBeInTheDocument();
    expect(screen.getByLabelText('Name')).toBeInTheDocument();
    expect(screen.getByText('Workspace')).toBeInTheDocument();
    expect(screen.getByText('Access role')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Send invite' })).toBeInTheDocument();
  });

  it('renders empty and warning states when the user directory cannot be loaded', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 0,
      currentUserId: null,
      error: 'Forbidden',
      members: [],
    });

    render(await UsersPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByRole('alert')).toHaveTextContent('Access warning');
    expect(screen.getByRole('alert')).toHaveTextContent('Forbidden');
    expect(screen.getByText('No users yet')).toBeInTheDocument();
    expect(screen.getByText('No users registered yet')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });

  it('shows query feedback, admin rollups, membership load errors, and no-membership rows', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 4,
      currentUserId: 'user-super',
      error: null,
      members: [
        buildMemberAccessRow({
          id: 'user-super',
          username: 'super-admin',
          displayName: 'Super Admin',
          role: 'SystemAdmin',
          isCurrentUser: true,
        }),
        buildMemberAccessRow({
          id: 'user-tenant-admin',
          username: 'tenant-admin',
          displayName: 'Tenant Admin',
          role: 'TenantAdmin',
        }),
        buildMemberAccessRow({
          id: 'user-owner',
          username: 'owner',
          displayName: 'Owner User',
          role: 'Owner',
          status: 'banned',
          membershipLoadError: 'Memberships could not be loaded.',
        }),
        buildMemberAccessRow({
          id: 'user-slug-only',
          username: 'slug-only',
          displayName: 'Slug Only User',
          role: 'Moderator',
          tenantName: null,
          tenantSlug: 'partner-studio',
        }),
        buildMemberAccessRow({
          id: 'user-unassigned',
          username: 'unassigned',
          displayName: 'Unassigned User',
          role: 'Member',
          tenantId: null,
          tenantName: null,
          status: 'pending',
        }),
      ],
    });

    render(await UsersPage({ searchParams: Promise.resolve({ message: 'Role changed.' }) }));

    expect(screen.getByRole('alert')).toHaveTextContent('Role changed.');

    const activeMembersCard = screen.getByText('Active members').closest('[data-slot="card"]');
    expect(activeMembersCard).not.toBeNull();
    expect(within(activeMembersCard!).getByText('3')).toBeInTheDocument();

    const workspaceAccessCard = screen.getByText('Workspace access').closest('[data-slot="card"]');
    expect(workspaceAccessCard).not.toBeNull();
    expect(within(workspaceAccessCard!).getByText('4')).toBeInTheDocument();

    const ownerRow = screen.getByText('Owner User').closest('tr');
    expect(ownerRow).not.toBeNull();
    expect(within(ownerRow!).getByText('Memberships could not be loaded.')).toBeInTheDocument();
    expect(within(ownerRow!).getByText('banned')).toBeInTheDocument();

    const slugOnlyRow = screen.getByText('Slug Only User').closest('tr');
    expect(slugOnlyRow).not.toBeNull();
    expect(within(slugOnlyRow!).getByText('partner-studio')).toBeInTheDocument();
    expect(within(slugOnlyRow!).getAllByText('Moderator').length).toBeGreaterThan(0);

    const unassignedRow = screen.getByText('Unassigned User').closest('tr');
    expect(unassignedRow).not.toBeNull();
    expect(within(unassignedRow!).getByText('No active workspace')).toBeInTheDocument();
    expect(within(unassignedRow!).getByText('No workspace')).toBeInTheDocument();
    expect(within(unassignedRow!).getByText('pending')).toBeInTheDocument();

    expect(screen.queryByRole('button', { name: 'Save' })).not.toBeInTheDocument();
  });
});
