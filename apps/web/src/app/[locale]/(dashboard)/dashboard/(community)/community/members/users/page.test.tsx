import { render, screen, within } from '@testing-library/react';
import type React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const mocks = vi.hoisted(() => ({
  getMemberAccessDirectory: vi.fn(),
  updateMemberAccessRole: vi.fn(),
}));

vi.mock('@/lib/community', () => ({
  COMMUNITY_ACCESS_ROLES: [
    { value: 'Member', label: 'Member', description: 'Community access.' },
    { value: 'Moderator', label: 'Moderator', description: 'Moderation access.' },
    { value: 'TenantAdmin', label: 'Platform admin', description: 'Tenant operations access.' },
    { value: 'SystemAdmin', label: 'Super admin', description: 'Full platform access.' },
  ],
  getMemberAccessDirectory: mocks.getMemberAccessDirectory,
}));

vi.mock('@/lib/community/actions/member-access', () => ({
  updateMemberAccessRole: mocks.updateMemberAccessRole,
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ href, children, ...props }: { href: string; children: React.ReactNode }) => (
    <a href={href} {...props}>
      {children}
    </a>
  ),
}));

import UsersAndRolesPage from './page';

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
        isActive: true,
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

describe('community users and roles page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders member counts and promotion controls from the access directory', async () => {
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

    render(await UsersAndRolesPage({ searchParams: Promise.resolve({}) }));

    expect(screen.getByRole('heading', { name: 'Users and roles' })).toBeInTheDocument();
    expect(screen.getByText('Loaded from the identity API')).toBeInTheDocument();
    expect(screen.getByText('Full platform authority')).toBeInTheDocument();
    expect(screen.getByText('Admin User')).toBeInTheDocument();
    expect(screen.getByText('Member User')).toBeInTheDocument();
    expect(screen.getAllByText('GameGuild')).toHaveLength(2);
    expect(screen.getByText('You')).toBeInTheDocument();
    expect(screen.getAllByRole('button', { name: 'Save' })).toHaveLength(2);

    const row = screen.getByText('Member User').closest('tr');
    expect(row).not.toBeNull();
    expect(within(row!).getAllByText('Member').length).toBeGreaterThan(0);
  });

  it('renders empty and warning states when the user directory cannot be loaded', async () => {
    mocks.getMemberAccessDirectory.mockResolvedValue({
      total: 0,
      currentUserId: null,
      error: 'Forbidden',
      members: [],
    });

    render(await UsersAndRolesPage({ searchParams: Promise.resolve({}) }));

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

    render(await UsersAndRolesPage({ searchParams: Promise.resolve({ message: 'Role changed.' }) }));

    expect(screen.getByRole('alert')).toHaveTextContent('Role changed.');

    const platformAdminsCard = screen.getByText('Platform admins').closest('[data-slot="card"]');
    expect(platformAdminsCard).not.toBeNull();
    expect(within(platformAdminsCard!).getByText('2')).toBeInTheDocument();

    const superAdminsCard = screen.getByText('Super admins').closest('[data-slot="card"]');
    expect(superAdminsCard).not.toBeNull();
    expect(within(superAdminsCard!).getByText('1')).toBeInTheDocument();

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
    expect(within(unassignedRow!).getByText('pending')).toBeInTheDocument();
    expect(within(unassignedRow!).getByText('No membership')).toBeInTheDocument();

    expect(screen.getAllByRole('button', { name: 'Save' })).toHaveLength(4);
  });
});
